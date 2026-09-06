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
using VMSService.Core.Services.Submissions;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Automatic penalty wiring: penalties #1 (late letter of intent), #2a (RTGS code already present at a late
/// submission), #2b (RTGS code entered late after the fact) and #3b (exceptional request submitted
/// after 15:00) — all wired through <see cref="CbWithdrawRequestsController"/>.
///
/// Same limitation as <see cref="CbWithdrawRequestsControllerSameDayExceptionalSubmitTests"/>:
/// SubmissionClassifier hardcodes DateTime.UtcNow with no injectable clock, so "collection date is
/// today" tests use the classifier itself as a self-consistent oracle, computed immediately before
/// the real call. Tests that don't need to specifically exercise the same-day boundary use a future
/// collection date instead, which is fully deterministic (never same-day, regardless of wall clock).
/// </summary>
public class CbWithdrawRequestsControllerPenaltyTests
{
    private static WithdrawalRequest NewRequest(
        DateOnly collectionDate, bool requiresSpecialApproval, string? reason, string? rtgsCode = null,
        string statusName = WithdrawalStatusNames.PendingInternalApproval)
        => new()
        {
            Id = Guid.NewGuid(),
            CbId = 1,
            StatusTypeId = 1,
            StatusTypeName = statusName,
            CollectionDate = collectionDate,
            RequiresSpecialApproval = requiresSpecialApproval,
            SpecialApprovalReason = reason,
            RtgsCode = rtgsCode,
            TransactionRefNumber = "REF-PENALTY-TEST",
            TotalAmount = 10_000_000m,
        };

    private static bool TodayIsCurrentlySameDayExceptional(DateOnly today)
        => SubmissionClassifier.Classify(DateTime.UtcNow, today, isDayBeforeHoliday: true)
            == SubmissionClassification.SameDayExceptional;

    private static bool IsCurrentlyAfter3pmEat()
        => TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) > new TimeOnly(15, 0, 0);

    private static bool IsCurrentlyAfter11amEat()
        => TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) > new TimeOnly(11, 0, 0);

    private static (CbWithdrawRequestsController Controller, FakeAppliedPenaltyRepository AppliedPenalties)
        BuildController(WithdrawalRequest request, string role = RolePolicy.CbApprover)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.Requests.FindById(request.Id)).ReturnsAsync(request);
        uow.Setup(u => u.Requests.ExistsByRtgsCodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(u => u.StatusHistory.FindMostRecentByNewStatusNameAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WithdrawalRequestStatusHistory?)null);

        // Forced true unconditionally — same trick as CbWithdrawRequestsControllerSameDayExceptionalSubmitTests,
        // removes dependence on which real weekday "yesterday" happens to be.
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

        var penaltyRepository = new FakePenaltyRepository(
            PenaltyConstants.AutomaticPenalties.LateSubmissionOfLetterOfIntent,
            PenaltyConstants.AutomaticPenalties.LateSubmissionOfRtgsToTransact,
            PenaltyConstants.AutomaticPenalties.TransactingLate);
        var appliedPenaltyRepository = new FakeAppliedPenaltyRepository();

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
            penaltyRepository,
            appliedPenaltyRepository,
            Mock.Of<ILogger<CbWithdrawRequestsController>>(),
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
                        new Claim(RolePolicy.CbIdClaim, request.CbId.ToString()),
                    ])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, appliedPenaltyRepository);
    }

    [Fact]
    public async Task Approval_Of_SameDay_Request_With_Code_Present_Matches_Live_Classification_And_Clock_For_Penalties_1_And_2a()
    {
        // Penalty #1 (the letter of intent) and #2a (the RTGS code) are judged independently —
        // #1 against the grace-window classifier, #2a against a plain 11:00 EAT clock on the
        // collection date — so each gets its own live oracle rather than assuming they agree.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: true, reason: "Urgent same-day withdrawal", rtgsCode: "RTGS-1");
        var (controller, appliedPenalties) = BuildController(request);

        var expectedSameDayExceptional = TodayIsCurrentlySameDayExceptional(today);
        var expectedRtgsLate = IsCurrentlyAfter11amEat();

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approve", CancellationToken.None);

        Assert.Equal(expectedSameDayExceptional, appliedPenalties.Rows.Any(a => a.PenaltyId == 1));
        Assert.Equal(expectedRtgsLate, appliedPenalties.Rows.Any(a => a.PenaltyId == 2));
    }

    [Fact]
    public async Task Approval_Of_SameDay_Request_Without_Code_Never_Applies_Penalty_2a_Even_When_Penalty_1_Applies()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: true, reason: "Urgent same-day withdrawal", rtgsCode: null);
        var (controller, appliedPenalties) = BuildController(request);

        var expectedSameDayExceptional = TodayIsCurrentlySameDayExceptional(today);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approve", CancellationToken.None);

        Assert.Equal(expectedSameDayExceptional, appliedPenalties.Rows.Any(a => a.PenaltyId == 1));
        Assert.DoesNotContain(appliedPenalties.Rows, a => a.PenaltyId == 2);
    }

    [Fact]
    public async Task Approval_Of_Future_Dated_Request_Never_Applies_Penalty_1_Or_2a()
    {
        // Fully deterministic — a future collection date is never same-day, regardless of wall clock.
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, requiresSpecialApproval: false, reason: null, rtgsCode: "RTGS-1");
        var (controller, appliedPenalties) = BuildController(request);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approve", CancellationToken.None);

        Assert.Empty(appliedPenalties.Rows);
    }

    [Fact]
    public async Task Repeated_Approval_Of_The_Same_Request_Never_Stacks_Penalty_1()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: true, reason: "Urgent same-day withdrawal", rtgsCode: "RTGS-1");
        var (controller, appliedPenalties) = BuildController(request);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approve", CancellationToken.None);
        var countAfterFirstCall = appliedPenalties.Rows.Count(a => a.PenaltyId == 1);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approve again", CancellationToken.None);
        var countAfterSecondCall = appliedPenalties.Rows.Count(a => a.PenaltyId == 1);

        Assert.Equal(countAfterFirstCall, countAfterSecondCall);
    }

    [Fact]
    public async Task ExceptionalWithdrawalRequest_Matches_Live_15_00_Cutoff_For_Penalty_3b()
    {
        // Future collection date isolates penalty #3b from #1/#2a — deterministically Standard,
        // so only the 15:00 clock check under test can add a row.
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, requiresSpecialApproval: false, reason: null);
        var (controller, appliedPenalties) = BuildController(request);

        var expectedAfter3pm = IsCurrentlyAfter3pmEat();

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ExceptionalWithdrawalRequest, "exceptional", CancellationToken.None);

        Assert.Equal(expectedAfter3pm, appliedPenalties.Rows.Any(a => a.PenaltyId == 3));
    }

    [Fact]
    public async Task ApproveToRTGSVerification_Never_Applies_Penalty_3b_Even_After_15_00()
    {
        // Penalty #3b is specific to the exceptional-approval action — the ordinary approval path
        // must never trigger it, deterministically, regardless of wall clock.
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, requiresSpecialApproval: false, reason: null);
        var (controller, appliedPenalties) = BuildController(request);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approve", CancellationToken.None);

        Assert.DoesNotContain(appliedPenalties.Rows, a => a.PenaltyId == 3);
    }

    // ── Penalty #2b — UpdateRtgs ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateRtgs_Entering_Code_On_A_Future_Dated_Request_Never_Applies_Penalty_2b()
    {
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, requiresSpecialApproval: false, reason: null, rtgsCode: null);
        var (controller, appliedPenalties) = BuildController(request, role: RolePolicy.CbMaker);

        await controller.UpdateRtgs(request.Id, null, "RTGS-LATER", CancellationToken.None);

        Assert.Empty(appliedPenalties.Rows);
    }

    [Fact]
    public async Task UpdateRtgs_Entering_Code_SameDay_Matches_Live_11am_Clock_For_Penalty_2b()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null, rtgsCode: null);
        var (controller, appliedPenalties) = BuildController(request, role: RolePolicy.CbMaker);

        var expectedRtgsLate = IsCurrentlyAfter11amEat();

        await controller.UpdateRtgs(request.Id, null, "RTGS-TODAY", CancellationToken.None);

        Assert.Equal(expectedRtgsLate, appliedPenalties.Rows.Any(a => a.PenaltyId == 2));
    }

    [Fact]
    public async Task UpdateRtgs_Editing_An_Already_Present_Code_Never_Applies_Penalty_2b()
    {
        // Penalty #2b is specifically for a code entered for the first time — editing a code that
        // was already there (Penalty #2a's territory, judged at approval instead) must not re-fire.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null, rtgsCode: "RTGS-ORIGINAL");
        var (controller, appliedPenalties) = BuildController(request, role: RolePolicy.CbMaker);

        await controller.UpdateRtgs(request.Id, null, "RTGS-CHANGED", CancellationToken.None);

        Assert.Empty(appliedPenalties.Rows);
    }
}
