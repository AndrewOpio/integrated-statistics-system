using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore.Storage;
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
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Extends ticket 07 (hide Print when FundsNotIssued) per follow-up discussion: the ABM needs a
/// dedicated way to declare "no funds issued" for a request, separate from the dispatch entry modal
/// (<see cref="WithdrawalRequestsController.SaveBankDenominations"/>). The business rule: a request
/// is either dispatched without penalty, dispatched with penalty, or not-issued with penalty — never
/// not-issued with nothing at all — so at least one penalty must already be applied. Marking the
/// flag also immediately releases any boxes already held, reusing the same
/// <c>ReleaseBouHoldAsync</c> sequence ticket 06 built for Reject/Return. Reversible via
/// <see cref="WithdrawalRequestsController.UndoFundsNotIssued"/> — deliberately does not try to
/// restore the released boxes (they may have been reassigned by then), it just re-opens dispatch
/// editing so the ABM re-enters from scratch through the normal <c>SaveBankDenominations</c> flow.
/// </summary>
public class WithdrawalRequestsControllerMarkFundsNotIssuedTests
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

    private static (WithdrawalRequestsController Controller, Mock<IWithdrawalRequestDenominationRepository> DenomRepo, List<string> CallOrder)
        BuildController(WithdrawalRequest request, bool hasAppliedPenalty)
    {
        var callOrder = new List<string>();

        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

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
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());

        var appliedPenaltyRepository = new Mock<IAppliedPenaltyRepository>();
        appliedPenaltyRepository
            .Setup(r => r.FindBySourceId(request.TransactionRefNumber!))
            .ReturnsAsync(hasAppliedPenalty
                ? [new AppliedPenalty { PenaltyId = 1 }]
                : new List<AppliedPenalty>());

        var controller = new WithdrawalRequestsController(
            requestRepo.Object,
            Mock.Of<IWorkflowServiceClient>(),
            Mock.Of<IWorkflowService>(),
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
            appliedPenaltyRepository.Object,
            Mock.Of<IPenaltyWaiverRepository>(),
            Mock.Of<ILogger<WithdrawalRequestsController>>(),
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, denomRepo, callOrder);
    }

    [Fact]
    public async Task Marks_Flag_And_Releases_Held_Boxes_When_A_Penalty_Is_Already_Applied()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var (controller, denomRepo, callOrder) = BuildController(request, hasAppliedPenalty: true);

        await controller.MarkFundsNotIssued(request.Id, CancellationToken.None);

        Assert.True(request.FundsNotIssued);
        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(request.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(
            [nameof(IWithdrawalRequestDenominationRepository.GetHeldByBoxAsync), nameof(IWithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync)],
            callOrder);
    }

    [Fact]
    public async Task Rejects_When_No_Penalty_Is_Applied_Yet()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var (controller, denomRepo, _) = BuildController(request, hasAppliedPenalty: false);

        await controller.MarkFundsNotIssued(request.Id, CancellationToken.None);

        Assert.False(request.FundsNotIssued);
        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rejects_When_Request_Is_Not_Pending_Verification()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingApproval);
        var (controller, denomRepo, _) = BuildController(request, hasAppliedPenalty: true);

        await controller.MarkFundsNotIssued(request.Id, CancellationToken.None);

        Assert.False(request.FundsNotIssued);
        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveBankDenominations_Rejects_Edits_Once_FundsNotIssued_Is_Set()
    {
        // Hiding "Edit Dispatch" is only a view condition — this is the server-side half, closing
        // off the same edit via a direct POST to SaveBankDenominations once the flag is set.
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        request.FundsNotIssued = true;
        var (controller, denomRepo, _) = BuildController(request, hasAppliedPenalty: true);

        await controller.SaveBankDenominations(request.Id, "[]", CancellationToken.None);

        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        denomRepo.Verify(r => r.Create(It.IsAny<WithdrawalRequestDenomination>()), Times.Never);
    }

    [Fact]
    public async Task Undo_Clears_The_Flag_When_Marked_And_Still_Pending_Verification()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        request.FundsNotIssued = true;
        var (controller, _, _) = BuildController(request, hasAppliedPenalty: true);

        await controller.UndoFundsNotIssued(request.Id, CancellationToken.None);

        Assert.False(request.FundsNotIssued);
    }

    [Fact]
    public async Task Undo_Rejects_When_Not_Marked()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        var (controller, _, _) = BuildController(request, hasAppliedPenalty: true);

        await controller.UndoFundsNotIssued(request.Id, CancellationToken.None);

        Assert.False(request.FundsNotIssued);
    }

    [Fact]
    public async Task Undo_Rejects_When_Not_Pending_Verification()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingApproval);
        request.FundsNotIssued = true;
        var (controller, _, _) = BuildController(request, hasAppliedPenalty: true);

        await controller.UndoFundsNotIssued(request.Id, CancellationToken.None);

        Assert.True(request.FundsNotIssued);
    }

    [Fact]
    public async Task Undo_Then_SaveBankDenominations_Allows_Dispatch_To_Be_Re_Entered()
    {
        // The whole point of Undo: it doesn't try to restore the released boxes, it just re-opens
        // the door SaveBankDenominations closes once FundsNotIssued is set (see the rejection test
        // above) — the ABM re-enters dispatch from scratch through the normal flow.
        var request = NewRequest(WithdrawalStatusNames.PendingVerification);
        request.FundsNotIssued = true;
        var (controller, denomRepo, _) = BuildController(request, hasAppliedPenalty: true);

        await controller.UndoFundsNotIssued(request.Id, CancellationToken.None);
        await controller.SaveBankDenominations(request.Id, "[]", CancellationToken.None);

        Assert.False(request.FundsNotIssued);
        denomRepo.Verify(r => r.DeleteBouRowsWithChildrenAsync(request.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
