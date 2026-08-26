using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Workflows;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using Xunit;
using static VMSService.Core.Static.SystemPolicy;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Covers the corrected ticket-03 routing: <c>ResubmitForApproval</c> (CB Maker) always lands on
/// Pending Internal Approval with no shortcut — the ABM-same-code skip-ahead only fires later, at
/// the CB Approver's own approval action in <see cref="CbWithdrawRequestsController.ExecuteAction"/>,
/// extending the pre-existing "RTGS code already provided" auto-advance. See ADR-0001.
/// </summary>
public class CbWithdrawRequestsControllerTests
{
    private const int PendingWithdrawalConfirmationStatusId = 50;
    private const int PendingVerificationStatusId = 42;

    private static WithdrawalRequest NewApprovedRequest(string? rtgsCode, string? rtgsCodeAtLastReturn, string rtgsVerificationStatus)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 1,
            StatusTypeName = WithdrawalStatusNames.PendingInternalApproval,
            RtgsCode = rtgsCode,
            RtgsCodeAtLastReturn = rtgsCodeAtLastReturn,
            RtgsVerificationStatus = rtgsVerificationStatus,
            IsLumpSum = true, // sidesteps the unrelated denomination-copy branch entirely
            TotalAmount = 10_000_000m,
        };

    private static (CbWithdrawRequestsController Controller, WithdrawalRequest Request, List<WithdrawalRequestStatusHistory> History) BuildController(
        WithdrawalRequest request, WithdrawalRequestStatusHistory? lastReturn)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var historyRepo = new Mock<IWithdrawalRequestStatusHistoryRepository>();
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        historyRepo
            .Setup(r => r.Create(It.IsAny<WithdrawalRequestStatusHistory>()))
            .Callback<WithdrawalRequestStatusHistory>(createdHistory.Add)
            .ReturnsAsync(true);
        historyRepo
            .Setup(r => r.FindMostRecentByNewStatusNameAsync(
                request.Id, WithdrawalStatusNames.ReturnedForRevision, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastReturn);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.StatusHistory).Returns(historyRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var statusResolver = new Mock<IWithdrawalStatusResolver>();
        statusResolver
            .Setup(r => r.GetStatusIdAsync(WithdrawalStatusNames.PendingWithdrawalConfirmation))
            .ReturnsAsync(PendingWithdrawalConfirmationStatusId);
        statusResolver
            .Setup(r => r.GetStatusIdAsync(WithdrawalStatusNames.PendingVerification))
            .ReturnsAsync(PendingVerificationStatusId);

        // Simulates the real WorkflowService already having committed the configured transition
        // (Pending Internal Approval -> Pending RTGS Code) before ExecuteAction reloads the request.
        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()))
            .Callback(() =>
            {
                request.StatusTypeId = 2;
                request.StatusTypeName = WithdrawalStatusNames.PendingRTGSVerification;
            })
            .ReturnsAsync(true);

        // Ownership check always succeeds here — these tests cover ExecuteAction's ABM
        // skip-ahead routing, not cross-bank ownership (that's CbOwnershipAuthorizationHandlerTests).
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
            statusResolver.Object,
            Mock.Of<IAttachmentRepository>(),
            Mock.Of<IIdentityService>(),
            Mock.Of<IPublicHolidayRepository>(),
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
                        new Claim(ClaimTypes.Name, "cb-approver"),
                        new Claim(ClaimTypes.NameIdentifier, "cb-approver-id"),
                        new Claim("appRoleId", $"{SystemSettings.AppAcronym}:{Roles.CbApprover}"),
                    ])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, request, createdHistory);
    }

    [Fact]
    public async Task Approving_ABM_Returned_Request_With_Unchanged_Code_Skips_Straight_To_PendingVerification()
    {
        var request = NewApprovedRequest(rtgsCode: "SAME-CODE", rtgsCodeAtLastReturn: "SAME-CODE", rtgsVerificationStatus: RtgsVerification.Confirmed);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingVerification,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "abm-user",
            ChangedAt = DateTime.UtcNow,
        };
        var (controller, updatedRequest, history) = BuildController(request, lastReturn);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approving", CancellationToken.None);

        Assert.Equal(PendingVerificationStatusId, updatedRequest.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingVerification, updatedRequest.StatusTypeName);
        Assert.Equal(RtgsVerification.Confirmed, updatedRequest.RtgsVerificationStatus); // untouched — no re-verification needed
        Assert.Contains(history, h => h.NewStatusName == WithdrawalStatusNames.PendingVerification);
    }

    [Fact]
    public async Task Approving_ABM_Returned_Request_With_Changed_Code_Follows_Normal_Path_And_Resets_Verification()
    {
        var request = NewApprovedRequest(rtgsCode: "NEW-CODE", rtgsCodeAtLastReturn: "OLD-CODE", rtgsVerificationStatus: RtgsVerification.Confirmed);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingVerification,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "abm-user",
            ChangedAt = DateTime.UtcNow,
        };
        var (controller, updatedRequest, history) = BuildController(request, lastReturn);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approving", CancellationToken.None);

        Assert.Equal(PendingWithdrawalConfirmationStatusId, updatedRequest.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingWithdrawalConfirmation, updatedRequest.StatusTypeName);
        Assert.Equal(RtgsVerification.Pending, updatedRequest.RtgsVerificationStatus); // stale confirmation reset
        Assert.Contains(history, h => h.NewStatusName == WithdrawalStatusNames.PendingWithdrawalConfirmation);
    }

    [Fact]
    public async Task Approving_BackOffice_Returned_Request_Never_Skips_Ahead_Even_With_Identical_Code()
    {
        var request = NewApprovedRequest(rtgsCode: "BAD-CODE", rtgsCodeAtLastReturn: "BAD-CODE", rtgsVerificationStatus: RtgsVerification.AmountMismatch);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.VerificationFailed,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "back-office-user",
            ChangedAt = DateTime.UtcNow,
        };
        var (controller, updatedRequest, _) = BuildController(request, lastReturn);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approving", CancellationToken.None);

        Assert.Equal(PendingWithdrawalConfirmationStatusId, updatedRequest.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingWithdrawalConfirmation, updatedRequest.StatusTypeName);
    }

    [Fact]
    public async Task Approving_First_Time_Submission_With_PreFilled_Code_Behaves_As_Today()
    {
        // No prior return at all (lastReturn is null) — matches today's pre-ticket-03 behavior.
        var request = NewApprovedRequest(rtgsCode: "FIRST-CODE", rtgsCodeAtLastReturn: null, rtgsVerificationStatus: RtgsVerification.Pending);
        var (controller, updatedRequest, _) = BuildController(request, lastReturn: null);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approving", CancellationToken.None);

        Assert.Equal(PendingWithdrawalConfirmationStatusId, updatedRequest.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingWithdrawalConfirmation, updatedRequest.StatusTypeName);
    }

    [Fact]
    public async Task Approving_CbApprovers_Own_Return_Resubmission_Behaves_As_Today()
    {
        var request = NewApprovedRequest(rtgsCode: "SOME-CODE", rtgsCodeAtLastReturn: null, rtgsVerificationStatus: RtgsVerification.Pending);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingInternalApproval,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "cb-approver",
            ChangedAt = DateTime.UtcNow,
        };
        var (controller, updatedRequest, _) = BuildController(request, lastReturn);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ApproveToRTGSVerification, "approving", CancellationToken.None);

        Assert.Equal(PendingWithdrawalConfirmationStatusId, updatedRequest.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingWithdrawalConfirmation, updatedRequest.StatusTypeName);
    }
}
