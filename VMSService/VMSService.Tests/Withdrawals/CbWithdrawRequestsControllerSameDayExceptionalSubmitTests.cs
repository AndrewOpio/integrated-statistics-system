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
/// before it'll persist — but that only runs when an actual save happens. A request created with a
/// future date and left unsubmitted until that date arrives (or edited past the client, e.g.
/// directly in the DB) can reach the actual submit action with RequiresSpecialApproval/
/// SpecialApprovalReason still reflecting whatever was true at last save, not now. This covers the
/// fix: ExecuteAction re-checks at the one point submission itself happens, for both
/// SubmitForInternalApproval and ResubmitForApproval.
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

    private static (CbWithdrawRequestsController Controller, Mock<IWorkflowService> WorkflowService) BuildController(WithdrawalRequest request)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
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
        // Same unflagged same-day setup that could block a submit — Approve isn't a CB Maker
        // submit action, so it must never even look at same-day-exceptional status here.
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
}
