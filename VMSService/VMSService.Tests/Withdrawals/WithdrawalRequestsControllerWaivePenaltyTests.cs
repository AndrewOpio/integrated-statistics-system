using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Authorization;
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
/// <c>AppliedPenalty.ToBeWaived</c>, with the <c>WaiverReason</c>/<c>WaivedBy</c>/<c>WaivedDate</c>
/// fields it needs to actually work — a per-instance waiver ("this specific applied penalty,
/// waived, with this reason"), deliberately separate from the branch-and-date-ranged
/// <c>PenaltyWaiver</c> table. Actor is the ABM (Panel B): exercised the same way
/// <see cref="WithdrawalRequestsController.ApplyPenalties"/> already is gated — Pending
/// Verification plus an available workflow transition for the caller's role.
/// </summary>
public class WithdrawalRequestsControllerWaivePenaltyTests
{
    private static WithdrawalRequest NewRequest(string statusName) => new()
    {
        Id = Guid.NewGuid(),
        StatusTypeId = 42,
        StatusTypeName = statusName,
        TransactionRefNumber = "TXN-001",
        TotalAmount = 10_000_000m,
        CreatedDate = DateTime.Now,
    };

    private static (WithdrawalRequestsController Controller, FakeAppliedPenaltyRepository AppliedPenalties)
        BuildController(WithdrawalRequest request, bool hasAvailableTransition = true)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);

        var appliedPenaltyRepository = new FakeAppliedPenaltyRepository();

        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(w => w.GetAvailableTransitionsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(hasAvailableTransition
                ? [new WorkflowTransitionDto { ActionName = "ReturnForRevision", RequiredRole = 5 }]
                : []);

        var controller = new WithdrawalRequestsController(
            requestRepo.Object,
            Mock.Of<IWorkflowServiceClient>(),
            workflowService.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            Mock.Of<IAttachmentRepository>(),
            uow.Object,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStockLedgerRepository>(),
            Mock.Of<ICategoryRepository>(),
            Mock.Of<ICounterPartyTypeRepository>(),
            Mock.Of<IPenaltyRepository>(),
            appliedPenaltyRepository,
            Mock.Of<IPenaltyWaiverRepository>(),
            Mock.Of<ILogger<WithdrawalRequestsController>>(),
            Mock.Of<IMapper>(),
            Mock.Of<IPublicHolidayRepository>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.PanelB}"),
                        new Claim(SystemPolicy.SystemSettings.AppRoleClaimType, $"{SystemPolicy.SystemSettings.AppAcronym}:5"),
                        new Claim(ClaimTypes.Name, "abm.jane"),
                    ])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, appliedPenaltyRepository);
    }

    private static AppliedPenalty SeedApplied(FakeAppliedPenaltyRepository repo, WithdrawalRequest request, int id = 1)
    {
        var applied = new AppliedPenalty
        {
            Id = id,
            Source = PenaltyConstants.Source.Withdrawal,
            SourceId = request.TransactionRefNumber!,
            PenaltyId = 7,
            ChargeValue = 50_000m,
            Description = string.Empty,
        };
        repo.Rows.Add(applied);
        return applied;
    }

    [Fact]
    public async Task Sets_ToBeWaived_Reason_WaivedBy_And_WaivedDate_Together()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var (controller, appliedPenalties) = BuildController(request);
        var applied = SeedApplied(appliedPenalties, request);

        await controller.WaivePenalty(request.Id, applied.Id, "Denomination mixup was a data-entry error, corrected before dispatch.");

        Assert.True(applied.ToBeWaived);
        Assert.Equal("Denomination mixup was a data-entry error, corrected before dispatch.", applied.WaiverReason);
        Assert.Equal("abm.jane", applied.WaivedBy);
        Assert.NotNull(applied.WaivedDate);
    }

    [Fact]
    public async Task Rejects_When_Reason_Is_Missing()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var (controller, appliedPenalties) = BuildController(request);
        var applied = SeedApplied(appliedPenalties, request);

        await controller.WaivePenalty(request.Id, applied.Id, "   ");

        Assert.False(applied.ToBeWaived);
        Assert.Null(applied.WaiverReason);
    }

    [Fact]
    public async Task Rejects_When_Request_Is_Not_Pending_Verification()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingApproval);
        var (controller, appliedPenalties) = BuildController(request);
        var applied = SeedApplied(appliedPenalties, request);

        await controller.WaivePenalty(request.Id, applied.Id, "Reason");

        Assert.False(applied.ToBeWaived);
    }

    [Fact]
    public async Task Rejects_When_Caller_Has_No_Available_Transition()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var (controller, appliedPenalties) = BuildController(request, hasAvailableTransition: false);
        var applied = SeedApplied(appliedPenalties, request);

        await controller.WaivePenalty(request.Id, applied.Id, "Reason");

        Assert.False(applied.ToBeWaived);
    }

    [Fact]
    public async Task Rejects_When_Applied_Penalty_Does_Not_Belong_To_This_Request()
    {
        // appliedPenaltyId is user-supplied — must be scoped to the request's own
        // TransactionRefNumber rather than trusted outright.
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var otherRequest = NewRequest(WithdrawalStatusNames.PendingVerification);
        otherRequest.TransactionRefNumber = "TXN-OTHER";
        var (controller, appliedPenalties) = BuildController(request);
        var appliedToOtherRequest = SeedApplied(appliedPenalties, otherRequest);

        await controller.WaivePenalty(request.Id, appliedToOtherRequest.Id, "Reason");

        Assert.False(appliedToOtherRequest.ToBeWaived);
    }
}
