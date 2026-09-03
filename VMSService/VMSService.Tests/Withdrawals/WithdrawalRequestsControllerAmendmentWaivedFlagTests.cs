using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Authorization;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.PenaltyWaivers;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Penalty #5's waived case: the ABM's most recent return for revision was at or after
/// 11:00 EAT — BOU's own lateness, so the penalty never applies, surfaced instead as a computed
/// <c>IsAmendmentPenaltyWaived</c> flag on the BOU Details view model. Mirrors
/// <see cref="WithdrawalRequestsControllerDetailsTests"/>'s controller-building setup, but wires the
/// mapper to actually project status history (that file's mapper always returns empty, which would
/// hide this flag entirely).
/// </summary>
public class WithdrawalRequestsControllerAmendmentWaivedFlagTests
{
    private static WithdrawalRequestsController BuildController(WithdrawalRequest request)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);

        var workflowServiceClient = new Mock<IWorkflowServiceClient>();
        workflowServiceClient
            .Setup(c => c.FindStatusById(It.IsAny<int>()))
            .ReturnsAsync((int id) => new StatusTypeDto { Id = id, Name = request.StatusTypeName });

        var configServiceClient = new Mock<IConfigurationsServiceClient>();
        configServiceClient.Setup(c => c.GetCbIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new CommercialBankDto { Id = 1, Name = "Test Bank", Code = "TB" });
        configServiceClient.Setup(c => c.GetBouBranchByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new BranchDto { Id = 1, Name = "Head Office", BranchCode = "HO" });

        var denomRepo = new Mock<IDenominationRepository>();
        denomRepo.Setup(r => r.FindAll()).ReturnsAsync(new List<Denomination>());

        var attachmentRepository = new Mock<IAttachmentRepository>();
        attachmentRepository.Setup(r => r.FindByEntityId(It.IsAny<string>()))
            .ReturnsAsync(new List<Attachment>());

        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<IEnumerable<WithdrawalRequestStatusHistoryViewModel>>(It.IsAny<object>()))
            .Returns((object src) => ((IEnumerable<WithdrawalRequestStatusHistory>)src).Select(h => new WithdrawalRequestStatusHistoryViewModel
            {
                Id = h.Id,
                OldStatusName = h.OldStatusName,
                NewStatusName = h.NewStatusName,
                ChangedAt = h.ChangedAt,
                ChangedBy = h.ChangedBy,
            }));

        var controller = new WithdrawalRequestsController(
            requestRepo.Object,
            workflowServiceClient.Object,
            Mock.Of<IWorkflowService>(),
            Mock.Of<IAgentDetailRepository>(),
            configServiceClient.Object,
            denomRepo.Object,
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            attachmentRepository.Object,
            uow.Object,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStockLedgerRepository>(),
            Mock.Of<ICategoryRepository>(),
            Mock.Of<ICounterPartyTypeRepository>(),
            Mock.Of<IPenaltyRepository>(),
            Mock.Of<IAppliedPenaltyRepository>(),
            Mock.Of<IPenaltyWaiverRepository>(),
            Mock.Of<ILogger<WithdrawalRequestsController>>(),
            mapper.Object,
            Mock.Of<IPublicHolidayRepository>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.RemittanceOfficer}"),
                    ])),
                },
            },
        };

        return controller;
    }

    private static WithdrawalRequest NewRequest(WithdrawalRequestStatusHistory lastAbmReturn) => new()
    {
        Id = Guid.NewGuid(),
        StatusTypeId = 20,
        StatusTypeName = WithdrawalStatusNames.ReturnedForRevision,
        CollectionDate = DateOnly.FromDateTime(DateTime.Today),
        TransactionRefNumber = null, // skips the applied-penalties branch, irrelevant here
        TotalAmount = 10_000_000m,
        WithdrawalRequestStatusHistory = [lastAbmReturn],
    };

    [Fact]
    public async Task Abm_Return_At_Or_After_11_Sets_Waived_Flag_True()
    {
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingVerification,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedAt = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc), // 12:00 EAT
        };
        var request = NewRequest(lastReturn);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var model = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(model.IsAmendmentPenaltyWaived);
    }

    [Fact]
    public async Task Abm_Return_Before_11_Sets_Waived_Flag_False()
    {
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingVerification,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedAt = new DateTime(2024, 1, 1, 6, 0, 0, DateTimeKind.Utc), // 09:00 EAT
        };
        var request = NewRequest(lastReturn);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var model = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(model.IsAmendmentPenaltyWaived);
    }

    [Fact]
    public async Task Return_From_A_Non_Abm_Origin_Never_Sets_Waived_Flag()
    {
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.VerificationFailed,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedAt = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc), // 12:00 EAT — would be waived if it were ABM-origin
        };
        var request = NewRequest(lastReturn);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var model = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(model.IsAmendmentPenaltyWaived);
    }
}
