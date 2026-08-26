using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Models.WithdrawalRequests;
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
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 07: <c>FundsNotIssued</c> must suppress the Print/"Print Slip" action on the BOU Details
/// view model too (per <c>withdrawal-shared-concepts.md</c>'s cross-controller access note, both CB
/// and BOU Details pages link to the same <c>CbWithdrawRequestsController.PrintRequest</c>). The
/// view's print condition reads <c>Model.FundsNotIssued</c> directly, so this covers the flag
/// flowing unchanged from the entity into the BOU view model.
/// </summary>
public class WithdrawalRequestsControllerDetailsTests
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
            .Setup(m => m.Map<IEnumerable<VMSService.Core.Models.CbWithdrawRequest.WithdrawalRequestStatusHistoryViewModel>>(It.IsAny<object>()))
            .Returns(new List<VMSService.Core.Models.CbWithdrawRequest.WithdrawalRequestStatusHistoryViewModel>());

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
            mapper.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    // No appRole claims — keeps every role-driven branch (dispatch editability,
                    // available transitions) inert; irrelevant to the FundsNotIssued mapping.
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            },
        };

        return controller;
    }

    private static WithdrawalRequest NewRequest(bool fundsNotIssued) => new()
    {
        Id = Guid.NewGuid(),
        StatusTypeId = 5,
        StatusTypeName = WithdrawalStatusNames.Approved,
        CollectionDate = DateOnly.FromDateTime(DateTime.Today),
        TransactionRefNumber = null, // skips the applied-penalties branch, irrelevant here
        TotalAmount = 10_000_000m,
        FundsNotIssued = fundsNotIssued,
    };

    [Fact]
    public async Task Details_FundsNotIssued_Request_Has_FundsNotIssued_True_On_ViewModel()
    {
        var request = NewRequest(fundsNotIssued: true);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.FundsNotIssued);
    }

    [Fact]
    public async Task Details_Normal_Request_Has_FundsNotIssued_False_On_ViewModel()
    {
        var request = NewRequest(fundsNotIssued: false);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(vm.FundsNotIssued);
    }

    private static WithdrawalRequestDenomination NewDenomRow(string origin, decimal amount, string bundleType = BundleTypes.Reissue) => new()
    {
        Origin = origin,
        BundleType = bundleType,
        Amount = amount,
        DenominationValue = amount,
    };

    // ── DispatchVariance (follow-up to ticket 06): the Submit-for-Approval readiness gate in
    // Details.cshtml needs to know not just whether dispatch rows exist, but whether they actually
    // sum to the requested amount — HasDispatchData alone doesn't check that. Computed once here so
    // the view's gate and the "Matched/Over/Short" badge can't disagree about what "matched" means.

    [Fact]
    public async Task Details_No_Dispatch_Rows_HasDispatchData_False_And_Variance_Is_Full_Shortfall()
    {
        var request = NewRequest(fundsNotIssued: false);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(vm.HasDispatchData);
        Assert.Equal(-request.TotalAmount, vm.DispatchVariance);
    }

    [Fact]
    public async Task Details_Dispatch_Matching_Requested_Amount_Has_Zero_Variance()
    {
        var request = NewRequest(fundsNotIssued: false);
        request.WithdrawalRequestDenominations.Add(NewDenomRow(WithdrawalOrigin.Bou, request.TotalAmount));
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.HasDispatchData);
        Assert.Equal(0m, vm.DispatchVariance);
    }

    [Fact]
    public async Task Details_Dispatch_Short_Of_Requested_Amount_Has_Nonzero_Variance()
    {
        var request = NewRequest(fundsNotIssued: false);
        request.WithdrawalRequestDenominations.Add(NewDenomRow(WithdrawalOrigin.Bou, request.TotalAmount - 4_000_000m));
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.HasDispatchData);
        Assert.Equal(-4_000_000m, vm.DispatchVariance);
    }

    [Fact]
    public async Task Details_Cb_Origin_Rows_Excluded_From_DispatchVariance()
    {
        // Only what the bank actually dispatched counts — the CB's own wishlist rows must not be
        // mistaken for dispatch, or a request with no dispatch at all would look "matched".
        var request = NewRequest(fundsNotIssued: false);
        request.WithdrawalRequestDenominations.Add(NewDenomRow(WithdrawalOrigin.Cb, request.TotalAmount));
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<BouWithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(vm.HasDispatchData);
        Assert.Equal(-request.TotalAmount, vm.DispatchVariance);
    }
}
