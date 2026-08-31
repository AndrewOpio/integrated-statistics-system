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
/// Ticket 07: <c>FundsNotIssued</c> must suppress the Print/"Print Slip" action on the CB Details
/// view model — see <c>withdrawal-shared-concepts.md</c>'s FundsNotIssued section. The view's
/// print condition (<c>Details.cshtml</c>) reads <c>Model.FundsNotIssued</c> directly, so the
/// behavior this covers is that the flag flows unchanged from the entity into the view model.
/// </summary>
public class CbWithdrawRequestsControllerDetailsTests
{
    private static CbWithdrawRequestsController BuildController(WithdrawalRequest request)
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

        // Ownership check always succeeds here — this file covers FundsNotIssued mapping onto
        // the view model, not cross-bank ownership (that's CbOwnershipAuthorizationHandlerTests).
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
                    // A cbId claim is enough to pass Details()'s IsCbUser() gate (ticket 34); the
                    // CbMaker appRole claim keeps canEdit's role check truthy without pulling in
                    // any other role-driven branch (available transitions, BOU visibility
                    // filtering) for this ViewModel-mapping check.
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("appRole", $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
                        new Claim("cbId", "1"),
                    ])),
                },
            },
        };

        return controller;
    }

    private static WithdrawalRequest NewRequest(
        bool fundsNotIssued = false,
        bool requiresSpecialApproval = false,
        DateOnly? collectionDate = null) => new()
    {
        Id = Guid.NewGuid(),
        StatusTypeId = 5,
        StatusTypeName = WithdrawalStatusNames.Approved,
        CollectionDate = collectionDate ?? DateOnly.FromDateTime(DateTime.Today),
        TransactionRefNumber = null, // skips the applied-penalties branch, irrelevant here
        TotalAmount = 10_000_000m,
        FundsNotIssued = fundsNotIssued,
        RequiresSpecialApproval = requiresSpecialApproval,
    };

    [Fact]
    public async Task Details_FundsNotIssued_Request_Has_FundsNotIssued_True_On_ViewModel()
    {
        var request = NewRequest(fundsNotIssued: true);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.FundsNotIssued);
    }

    [Fact]
    public async Task Details_Normal_Request_Has_FundsNotIssued_False_On_ViewModel()
    {
        var request = NewRequest(fundsNotIssued: false);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(vm.FundsNotIssued);
    }

    // The CB Details view's static "Exceptional Approval Required" badge (Details.cshtml) reads
    // Model.RequiresSpecialApproval directly and unconditionally — these lock down that the flag
    // flows unchanged from the entity into the view model, and that it's independent of the live
    // grace-period indicator (GracePeriodReason), which is computed separately from
    // CollectionDate/day-before-holiday and only ever applies on same-day collection. A collection
    // date a week out guarantees no grace period is active, isolating the static flag from the
    // live one.
    private static readonly DateOnly CollectionDateOutsideGracePeriod = DateOnly.FromDateTime(DateTime.Today).AddDays(7);

    [Fact]
    public async Task Details_Exceptional_Request_Has_RequiresSpecialApproval_True_On_ViewModel()
    {
        var request = NewRequest(requiresSpecialApproval: true, collectionDate: CollectionDateOutsideGracePeriod);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.RequiresSpecialApproval);
    }

    [Fact]
    public async Task Details_Standard_Request_Has_RequiresSpecialApproval_False_On_ViewModel()
    {
        var request = NewRequest(requiresSpecialApproval: false, collectionDate: CollectionDateOutsideGracePeriod);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(vm.RequiresSpecialApproval);
    }

    [Fact]
    public async Task Details_Exceptional_Request_Does_Not_Set_GracePeriodReason()
    {
        var request = NewRequest(requiresSpecialApproval: true, collectionDate: CollectionDateOutsideGracePeriod);
        var controller = BuildController(request);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.Null(vm.GracePeriodReason);
    }
}
