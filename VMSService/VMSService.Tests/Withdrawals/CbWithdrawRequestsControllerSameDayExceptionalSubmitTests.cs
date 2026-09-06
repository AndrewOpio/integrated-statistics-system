using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Authorization;
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
using VMSService.Core.Services.Submissions;
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
/// SaveDraft re-derives same-day-exceptional status from CollectionDate and requires a reason
/// before it'll persist — but that only runs when an actual save happens. A request left sitting
/// (by either the Maker before submitting, or the Approver before approving) until its collection
/// date arrives can reach the actual action with RequiresSpecialApproval/SpecialApprovalReason
/// still reflecting whatever was true earlier, not now. This covers the fix: ExecuteAction
/// re-checks at the actual action points for both roles — the Maker's SubmitForInternalApproval/
/// ResubmitForApproval, and the Approver's ApproveToRTGSVerification/ExceptionalWithdrawalRequest.
///
/// SubmissionClassifier hardcodes DateTime.UtcNow — there's no injectable clock anywhere in this
/// codebase (SaveDraft's identical logic has the same limitation and, confirmed, zero existing
/// test coverage for the same reason). Tests that must exercise the "collection date is today"
/// branch use the classifier itself as a self-consistent oracle: compute the expected
/// classification the same way the controller will, immediately before invoking it, rather than
/// hardcoding an assumption about the real wall-clock at test-run time. isDayBeforeHoliday is
/// forced true throughout, removing dependence on which real weekday the tests happen to run on —
/// the remaining variable (exact time-of-day relative to the 11:00 EAT grace cutoff) is shared
/// between the oracle computation and the real call, microseconds apart.
/// </summary>
public class CbWithdrawRequestsControllerSameDayExceptionalSubmitTests
{
    private static WithdrawalRequest NewRequest(DateOnly collectionDate, bool requiresSpecialApproval, string? reason)
        => new()
        {
            Id = Guid.NewGuid(),
            CbId = 1,
            StatusTypeId = 1,
            StatusTypeName = WithdrawalStatusNames.PendingInternalApproval,
            CollectionDate = collectionDate,
            RequiresSpecialApproval = requiresSpecialApproval,
            SpecialApprovalReason = reason,
            TotalAmount = 10_000_000m,
        };

    private static (CbWithdrawRequestsController Controller, Mock<IWorkflowService> WorkflowService) BuildController(
        WithdrawalRequest request, string role = RolePolicy.CbMaker)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Forced true unconditionally, regardless of the date asked about — removes dependence on
        // which real weekday "yesterday" happens to be when these tests run.
        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository
            .Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>()))
            .ReturnsAsync(new PublicHoliday { Date = DateOnly.FromDateTime(DateTime.Today), Name = "Forced" });

        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()))
            .ReturnsAsync(true);

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var controller = new CbWithdrawRequestsController(
            authorizationService.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            Mock.Of<IWithdrawalTransactionRefNumberGenerator>(),
            uow.Object,
            requestRepo.Object,
            Mock.Of<IWithdrawalRequestAgentRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            Mock.Of<IWorkflowServiceClient>(),
            workflowService.Object,
            Mock.Of<IWithdrawalStatusResolver>(),
            Mock.Of<IAttachmentRepository>(),
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
                        new Claim("appRole", $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
                        new Claim("cbId", "1"),
                    ])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, workflowService);
    }

    private static bool TodayIsCurrentlySameDayExceptional(DateOnly today)
        => SubmissionClassifier.Classify(DateTime.UtcNow, today, isDayBeforeHoliday: true)
            == SubmissionClassification.SameDayExceptional;

    [Theory]
    [InlineData(WithdrawalStatusActionNames.SubmitForInternalApproval)]
    [InlineData(WithdrawalStatusActionNames.ResubmitForApproval)]
    public async Task Submit_Of_Unflagged_Same_Day_Request_Matches_Live_Classification(string actionName)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null);
        var (controller, workflowService) = BuildController(request);

        var expectedBlocked = TodayIsCurrentlySameDayExceptional(today);

        var result = await controller.ExecuteAction(request.Id, actionName, "please approve", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        if (expectedBlocked)
        {
            Assert.NotNull(controller.TempData["Error"]);
            workflowService.Verify(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Never);
        }
        else
        {
            workflowService.Verify(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
        }
    }

    [Theory]
    [InlineData(WithdrawalStatusActionNames.SubmitForInternalApproval)]
    [InlineData(WithdrawalStatusActionNames.ResubmitForApproval)]
    public async Task Submit_Of_Properly_Flagged_Same_Day_Request_Always_Executes(string actionName)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: true, reason: "Urgent same-day withdrawal");
        var (controller, workflowService) = BuildController(request);

        var result = await controller.ExecuteAction(request.Id, actionName, "please approve", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    [Fact]
    public async Task Submit_Of_Future_Dated_Request_Is_Always_Unaffected()
    {
        // Fully deterministic regardless of real time — a future collection date is never
        // same-day, so the guard can never fire here.
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, requiresSpecialApproval: false, reason: null);
        var (controller, workflowService) = BuildController(request);

        var result = await controller.ExecuteAction(
            request.Id, WithdrawalStatusActionNames.SubmitForInternalApproval, "please approve", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    [Fact]
    public async Task Other_Actions_Are_Never_Subject_To_This_Guard()
    {
        // Same unflagged same-day setup that could block a submit/approve — an unrelated action
        // name must never even look at same-day-exceptional status here.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null);
        var (controller, workflowService) = BuildController(request);

        var result = await controller.ExecuteAction(request.Id, "SomeUnrelatedAction", "remarks", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    // ── Same guard, the Approver's own approve actions ──────────────────────
    // A request that was correctly unflagged when the Maker submitted (or even correctly flagged,
    // then somehow lost its reason) can still become same-day-exceptional by the time the Approver
    // actually reviews it — same-day-exceptional is a property of the request, not of who's acting
    // on it, so this is the identical guard applied at the Approver's own action point.

    [Theory]
    [InlineData(WithdrawalStatusActionNames.ApproveToRTGSVerification)]
    [InlineData(WithdrawalStatusActionNames.ExceptionalWithdrawalRequest)]
    public async Task Approve_Of_Unflagged_Same_Day_Request_Matches_Live_Classification(string actionName)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null);
        var (controller, workflowService) = BuildController(request, RolePolicy.CbApprover);

        var expectedBlocked = TodayIsCurrentlySameDayExceptional(today);

        var result = await controller.ExecuteAction(request.Id, actionName, "approved", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        if (expectedBlocked)
        {
            Assert.NotNull(controller.TempData["Error"]);
            workflowService.Verify(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Never);
        }
        else
        {
            workflowService.Verify(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
        }
    }

    [Fact]
    public async Task Approve_Of_Properly_Flagged_Same_Day_Request_Always_Executes()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: true, reason: "Urgent same-day withdrawal");
        var (controller, workflowService) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.ExecuteAction(
            request.Id, WithdrawalStatusActionNames.ExceptionalWithdrawalRequest, "approved", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    [Fact]
    public async Task ReturnForRevision_By_Approver_Is_Never_Subject_To_The_Guard()
    {
        // Returning isn't approving — no reason should ever be required to send it back.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null);
        var (controller, workflowService) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.ExecuteAction(
            request.Id, WithdrawalStatusActionNames.ReturnForRevision, "needs changes", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    // ── SetSameDayReason: first-time capture, not just correction ───────────
    // Previously this action could only correct an already-true RequiresSpecialApproval — there
    // was no way to set it the first time from the Approver's side, since the flag they'd need to
    // already be true is exactly the thing that's missing. It now also accepts live
    // same-day-exceptional as sufficient grounds to act, independent of the stored flag.

    [Fact]
    public async Task SetSameDayReason_First_Time_Capture_On_Live_Same_Day_Request_Matches_Live_Classification()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null);
        var (controller, _) = BuildController(request, RolePolicy.CbApprover);

        var expectedAllowed = TodayIsCurrentlySameDayExceptional(today);

        var result = await controller.SetSameDayReason(request.Id, "Captured by the Approver at review time", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        if (expectedAllowed)
        {
            Assert.Null(controller.TempData["Error"]);
            Assert.True(request.RequiresSpecialApproval);
            Assert.Equal("Captured by the Approver at review time", request.SpecialApprovalReason);
        }
        else
        {
            Assert.NotNull(controller.TempData["Error"]);
            Assert.False(request.RequiresSpecialApproval);
            Assert.Null(request.SpecialApprovalReason);
        }
    }

    [Fact]
    public async Task SetSameDayReason_On_Future_Dated_Request_Is_Still_Rejected()
    {
        // Fully deterministic — a future collection date is never same-day, so first-time capture
        // must never succeed here regardless of the live-classification relaxation.
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, requiresSpecialApproval: false, reason: null);
        var (controller, _) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.SetSameDayReason(request.Id, "Should not be accepted", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.False(request.RequiresSpecialApproval);
        Assert.Null(request.SpecialApprovalReason);
    }
}
