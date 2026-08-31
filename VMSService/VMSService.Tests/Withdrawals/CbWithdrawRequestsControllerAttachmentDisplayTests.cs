using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Authorization;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Identity;
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
/// The BOU Details action already read a status-history entry's <c>AttachmentIds</c> and loaded
/// the matching attachments onto it, but <c>CbWithdrawRequestsController.Details</c> never did the
/// same mapping — so a receipt attached on Approve was invisible to the CB even after the entity
/// gained an <c>AttachmentIds</c> column and the view started rendering it. This covers the fix:
/// the CB Details action now loads per-entry attachments the same way the BOU one does.
/// </summary>
public class CbWithdrawRequestsControllerAttachmentDisplayTests
{
    private static (CbWithdrawRequestsController Controller, Mock<IAttachmentRepository> AttachmentRepo) BuildController(WithdrawalRequest request)
    {
        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var workflowServiceClient = new Mock<IWorkflowServiceClient>();
        workflowServiceClient
            .Setup(c => c.FindStatusById(It.IsAny<int>()))
            .ReturnsAsync((int id) => new StatusTypeDto { Id = id, Name = request.StatusTypeName });

        var configServiceClient = new Mock<IConfigurationsServiceClient>();
        configServiceClient.Setup(c => c.GetCbIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new CommercialBankDto { Id = 1, Name = "Test Bank", Code = "TB" });
        configServiceClient.Setup(c => c.GetBouBranchByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new BranchDto { Id = 1, Name = "Head Office", BranchCode = "HO" });

        var attachmentRepository = new Mock<IAttachmentRepository>();
        attachmentRepository.Setup(r => r.FindByEntityId(It.IsAny<string>()))
            .ReturnsAsync(new List<Attachment>());

        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository.Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>()))
            .ReturnsAsync((PublicHoliday?)null);

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var controller = new CbWithdrawRequestsController(
            authorizationService.Object,
            Mock.Of<IAgentDetailRepository>(),
            configServiceClient.Object,
            Mock.Of<IDenominationRepository>(),
            Mock.Of<IWithdrawalTransactionRefNumberGenerator>(),
            uow.Object,
            Mock.Of<IWithdrawalRequestRepository>(),
            Mock.Of<IWithdrawalRequestAgentRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            workflowServiceClient.Object,
            Mock.Of<IWorkflowService>(),
            Mock.Of<IWithdrawalStatusResolver>(),
            attachmentRepository.Object,
            publicHolidayRepository.Object,
            Mock.Of<IPenaltyRepository>(),
            Mock.Of<IAppliedPenaltyRepository>(),
            Mock.Of<ILogger<CbWithdrawRequestsController>>(),
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("appRole", $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
                        new Claim("cbId", "1"),
                    ])),
                },
            },
        };

        return (controller, attachmentRepository);
    }

    private static WithdrawalRequest NewApprovedRequestWithAttachedHistory(long attachmentId)
    {
        var request = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 5,
            StatusTypeName = WithdrawalStatusNames.Approved,
            CollectionDate = DateOnly.FromDateTime(DateTime.Today),
            TransactionRefNumber = null,
            TotalAmount = 10_000_000m,
        };
        request.WithdrawalRequestStatusHistory.Add(new WithdrawalRequestStatusHistory
        {
            WithdrawalRequestId = request.Id,
            OldStatusId = 4,
            OldStatusName = WithdrawalStatusNames.PendingApproval,
            NewStatusId = 5,
            NewStatusName = WithdrawalStatusNames.Approved,
            ChangedBy = "bm-user",
            ChangedAt = DateTime.UtcNow,
            AttachmentIds = attachmentId.ToString(),
        });
        return request;
    }

    [Fact]
    public async Task Details_StatusHistory_Entry_With_AttachmentIds_Has_Matching_Attachments_Loaded()
    {
        const long attachmentId = 5;
        var request = NewApprovedRequestWithAttachedHistory(attachmentId);
        var (controller, attachmentRepo) = BuildController(request);
        attachmentRepo.Setup(r => r.FindById((int)attachmentId)).ReturnsAsync(new Attachment
        {
            Id = attachmentId,
            FileName = "receipt.pdf",
            FilePath = "uploads/withdrawals/receipt.pdf",
            FileType = "application/pdf",
            FileSize = 1024,
            IsDeleted = false,
        });

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        var entry = Assert.Single(vm.StatusHistory);
        var attachment = Assert.Single(entry.Attachments);
        Assert.Equal("receipt.pdf", attachment.FileName);
    }

    [Fact]
    public async Task Details_StatusHistory_Entry_With_No_AttachmentIds_Has_No_Attachments()
    {
        var request = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 5,
            StatusTypeName = WithdrawalStatusNames.Approved,
            CollectionDate = DateOnly.FromDateTime(DateTime.Today),
            TotalAmount = 10_000_000m,
        };
        request.WithdrawalRequestStatusHistory.Add(new WithdrawalRequestStatusHistory
        {
            WithdrawalRequestId = request.Id,
            OldStatusId = 4,
            OldStatusName = WithdrawalStatusNames.PendingApproval,
            NewStatusId = 5,
            NewStatusName = WithdrawalStatusNames.Approved,
            ChangedBy = "bm-user",
            ChangedAt = DateTime.UtcNow,
        });
        var (controller, _) = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        var entry = Assert.Single(vm.StatusHistory);
        Assert.Empty(entry.Attachments);
    }
}
