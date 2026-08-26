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
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
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
/// Ticket 06: BM Reject and the ABM's own ReturnForRevision (origin: Pending Verification) must
/// release a request's held BOU-origin denomination rows — via
/// <see cref="IWithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync"/>, run only
/// after the pre-existing <c>ReverseBoxStateForRequestAsync</c> box-state reversal (which reads
/// those same rows to know what to reverse — clearing first would leave it nothing to work from).
/// The CB Approver's and Back Office's returns share the same ActionName/ToStatus (ADR-0001) but
/// must NOT trigger this — no dispatch data exists yet at their origins. CompleteRecall's approved
/// path is wired the same way — confirmed: Recall can be requested from Pending Approval (dispatch
/// guaranteed) or Pending Verification (dispatch may already exist), so the same dangling-boxes
/// problem applies there too.
/// </summary>
public class WithdrawalRequestsControllerReleaseOnRejectReturnTests
{
    private static WithdrawalRequest NewRequest(int statusTypeId, string statusName)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = statusTypeId,
            StatusTypeName = statusName,
            // ExecuteAction blocks several BOU review statuses without an RTGS code on file —
            // unrelated to what these tests cover, so set unconditionally to clear that gate.
            RtgsCode = "RTGS-TEST",
            TotalAmount = 10_000_000m,
            CreatedDate = DateTime.Now,
        };

    private static (WithdrawalRequestsController Controller, Mock<IWithdrawalRequestDenominationRepository> DenomRepo, List<string> CallOrder)
        BuildController(WithdrawalRequest request, WithdrawalRequest? updatedRequest, int? postTransitionStatusId, string? postTransitionStatusName)
    {
        var callOrder = new List<string>();

        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);
        if (updatedRequest is not null)
            requestRepo.Setup(r => r.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(updatedRequest);
        requestRepo.Setup(r => r.Update(It.IsAny<WithdrawalRequest>())).ReturnsAsync(true);

        // Shared across both the directly-injected _wrDenomRepo and _uow.Denominations, matching
        // the real registration (same repository type, two access paths).
        var denomRepo = new Mock<IWithdrawalRequestDenominationRepository>();
        denomRepo
            .Setup(r => r.GetHeldByBoxAsync(request.Id, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(nameof(IWithdrawalRequestDenominationRepository.GetHeldByBoxAsync)))
            .ReturnsAsync([]);
        denomRepo
            .Setup(r => r.DeleteBouRowsWithChildrenAsync(request.Id, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(nameof(IWithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync)))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.Denominations).Returns(denomRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()))
            .Callback(() =>
            {
                if (postTransitionStatusId.HasValue)
                {
                    request.StatusTypeId = postTransitionStatusId.Value;
                    request.StatusTypeName = postTransitionStatusName!;
                }
            })
            .ReturnsAsync(true);

        var controller = new WithdrawalRequestsController(
            requestRepo.Object,
            Mock.Of<IWorkflowServiceClient>(),
            workflowService.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            denomRepo.Object,
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
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimType.AppRole, $"{SystemSettings.AppAcronym}:{Roles.RemittancesOfficer}")])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, denomRepo, callOrder);
    }

    [Fact]
    public async Task Reject_Releases_BouOrigin_Rows_After_Reversing_Box_State()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingApproval);
        var updated = NewRequest(20, WithdrawalStatusNames.Rejected);
        updated.Id = request.Id;
        var (controller, denomRepo, callOrder) = BuildController(
            request, updated, postTransitionStatusId: 20, postTransitionStatusName: WithdrawalStatusNames.Rejected);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.Reject, "no receipt", CancellationToken.None);

        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(request.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(
            [nameof(IWithdrawalRequestDenominationRepository.GetHeldByBoxAsync), nameof(IWithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync)],
            callOrder);
    }

    [Fact]
    public async Task AbmReturnForRevision_From_PendingVerification_Releases_BouOrigin_Rows()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification);
        var updated = NewRequest(20, WithdrawalStatusNames.ReturnedForRevision);
        updated.Id = request.Id;
        var (controller, denomRepo, callOrder) = BuildController(
            request, updated, postTransitionStatusId: 20, postTransitionStatusName: WithdrawalStatusNames.ReturnedForRevision);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ReturnForRevision, "unrelated issue", CancellationToken.None);

        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(request.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(
            [nameof(IWithdrawalRequestDenominationRepository.GetHeldByBoxAsync), nameof(IWithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync)],
            callOrder);
    }

    [Fact]
    public async Task BackOfficeReturnForRevision_From_VerificationFailed_Does_Not_Release_Rows()
    {
        // Origin 3 (ADR-0001) — no dispatch data exists yet this early, so nothing to release.
        var request = NewRequest(15, WithdrawalStatusNames.VerificationFailed);
        var updated = NewRequest(20, WithdrawalStatusNames.ReturnedForRevision);
        updated.Id = request.Id;
        var (controller, denomRepo, _) = BuildController(
            request, updated, postTransitionStatusId: 20, postTransitionStatusName: WithdrawalStatusNames.ReturnedForRevision);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ReturnForRevision, "code failed", CancellationToken.None);

        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CbApproverReturnForRevision_From_PendingInternalApproval_Does_Not_Release_Rows()
    {
        // Origin 1 (ADR-0001) — same reasoning as the Back Office origin above.
        var request = NewRequest(5, WithdrawalStatusNames.PendingInternalApproval);
        var updated = NewRequest(20, WithdrawalStatusNames.ReturnedForRevision);
        updated.Id = request.Id;
        var (controller, denomRepo, _) = BuildController(
            request, updated, postTransitionStatusId: 20, postTransitionStatusName: WithdrawalStatusNames.ReturnedForRevision);

        await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.ReturnForRevision, "internal issue", CancellationToken.None);

        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteRecall_Releases_BouOrigin_Rows_On_Success()
    {
        var request = NewRequest(30, WithdrawalStatusNames.RecallApproved);
        var (controller, denomRepo, callOrder) = BuildController(
            request, updatedRequest: null, postTransitionStatusId: null, postTransitionStatusName: null);

        await controller.CompleteRecall(request.Id, CancellationToken.None);

        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(request.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(
            [nameof(IWithdrawalRequestDenominationRepository.GetHeldByBoxAsync), nameof(IWithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync)],
            callOrder);
    }
}
