using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
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
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using Xunit;
using static VMSService.Core.Static.SystemPolicy;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// SubmitVerification is supposed to route an exceptional request to Pending Exceptional Approval
/// and a standard one to Pending Verification, based on RequiresSpecialApproval — but nothing ever
/// checked that flag was actually correct by the time BOU sees it. This covers the defensive
/// backstop: re-run SubmissionClassifier against the recorded SubmitForInternalApproval timestamp
/// (not BOU's later clock — same-day-exceptional is only meaningful relative to when the CB Maker
/// submitted) and reject a mismatch instead of silently trusting the stored flag.
///
/// Unlike the CB-side submit guard, these tests don't depend on the real wall-clock at all — the
/// "submission timestamp" being tested is a fixed, explicit value in status history, not
/// DateTime.UtcNow at test-run time.
/// </summary>
public class WithdrawalRequestsControllerSubmitVerificationTests
{
    private static WithdrawalRequest NewRequest(bool requiresSpecialApproval)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 10,
            StatusTypeName = WithdrawalStatusNames.PendingWithdrawalConfirmation,
            RtgsCode = "RTGS-TEST",
            RequiresSpecialApproval = requiresSpecialApproval,
            CollectionDate = new DateOnly(2026, 8, 28), // a Friday; day-before (Thu) not a holiday
            TotalAmount = 10_000_000m,
        };

    private static WithdrawalRequestsController BuildController(
        WithdrawalRequest request, WithdrawalRequestStatusHistory? submittedAt)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var statusHistoryRepo = new Mock<IWithdrawalRequestStatusHistoryRepository>();
        statusHistoryRepo
            .Setup(r => r.FindMostRecentByNewStatusNameAsync(
                request.Id, WithdrawalStatusNames.PendingInternalApproval, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submittedAt);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.StatusHistory).Returns(statusHistoryRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository.Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>())).ReturnsAsync((PublicHoliday?)null);

        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()))
            .ReturnsAsync(true);

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
            Mock.Of<IAppliedPenaltyRepository>(),
            Mock.Of<IPenaltyWaiverRepository>(),
            Mock.Of<ILogger<WithdrawalRequestsController>>(),
            Mock.Of<IMapper>(),
            publicHolidayRepository.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimType.AppRole, $"{SystemSettings.AppAcronym}:{Roles.BackOffice}")])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    private static WithdrawalRequestStatusHistory SubmittedAt(DateTime changedAtUtc) => new()
    {
        NewStatusName = WithdrawalStatusNames.PendingInternalApproval,
        ChangedAt = changedAtUtc,
    };

    [Fact]
    public async Task Mismatched_Flag_Blocks_Verification()
    {
        // Submitted same-day (collection date == submission date, Fri after 11am EAT, day before
        // is a plain Thursday) — should have been exceptional, but the flag says it wasn't.
        var request = NewRequest(requiresSpecialApproval: false);
        var submittedAt = SubmittedAt(new DateTime(2026, 8, 28, 8, 30, 0, DateTimeKind.Utc));
        var controller = BuildController(request, submittedAt);

        var result = await controller.SubmitVerification(request.Id, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Pending, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task Correctly_Flagged_Same_Day_Request_Proceeds()
    {
        var request = NewRequest(requiresSpecialApproval: true);
        var submittedAt = SubmittedAt(new DateTime(2026, 8, 28, 8, 30, 0, DateTimeKind.Utc));
        var controller = BuildController(request, submittedAt);

        var result = await controller.SubmitVerification(request.Id, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task Correctly_Unflagged_Standard_Request_Proceeds()
    {
        // Submitted two days before its own collection date — never same-day, flag correctly false.
        var request = NewRequest(requiresSpecialApproval: false);
        var submittedAt = SubmittedAt(new DateTime(2026, 8, 26, 8, 30, 0, DateTimeKind.Utc));
        var controller = BuildController(request, submittedAt);

        var result = await controller.SubmitVerification(request.Id, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task No_Submission_History_Found_Skips_The_Check()
    {
        // Defensive fallback only — shouldn't happen in practice (every request that reaches
        // Pending RTGS Confirmation passed through Pending Internal Approval first), but must not
        // block verification if the history lookup comes back empty for some other reason.
        var request = NewRequest(requiresSpecialApproval: false);
        var controller = BuildController(request, submittedAt: null);

        var result = await controller.SubmitVerification(request.Id, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }
}
