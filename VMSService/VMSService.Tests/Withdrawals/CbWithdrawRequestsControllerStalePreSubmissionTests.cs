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
using VMSService.Core.Models.CbWithdrawRequest;
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
using VMSService.Core.Services.Stocks.StockEntities;
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
/// Neither the CB Maker nor the CB Approver should be able to act on a pre-submission request
/// (Draft/Pending Internal Approval/Returned for Internal Revision) once its collection date has
/// passed — a hard block, not an auto-expiry: only Edit/Delete stay reachable for the Maker (so
/// they can fix the date), and every other action — workflow transitions, RTGS entry, same-day
/// reason capture — is rejected server-side for both roles, not just hidden in the view.
/// </summary>
public class CbWithdrawRequestsControllerStalePreSubmissionTests
{
    private const int RoleId = 1;

    private static WithdrawalRequest NewRequest(DateOnly collectionDate, string statusName = "Pending Internal Approval")
        => new()
        {
            Id = Guid.NewGuid(),
            CbId = 1,
            StatusTypeId = 3,
            StatusTypeName = statusName,
            CollectionDate = collectionDate,
            TransactionRefNumber = null, // skips the applied-penalties branch, irrelevant here
            TotalAmount = 10_000_000m,
        };

    private static DateOnly Yesterday => DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
    private static DateOnly Tomorrow => DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    private static (CbWithdrawRequestsController Controller, Mock<IWorkflowService> WorkflowService) BuildController(
        WithdrawalRequest request, string roleName, List<WorkflowTransitionDto>? transitions = null)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);
        requestRepo.Setup(r => r.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var workflowServiceClient = new Mock<IWorkflowServiceClient>();
        workflowServiceClient
            .Setup(c => c.FindStatusById(It.IsAny<int>()))
            .ReturnsAsync((int id) => new StatusTypeDto { Id = id, Name = request.StatusTypeName });
        workflowServiceClient
            .Setup(c => c.GetAvailableTransitions(It.IsAny<GetAvailableTransitionsRequestDto>()))
            .ReturnsAsync(transitions ?? []);

        var configServiceClient = new Mock<IConfigurationsServiceClient>();
        configServiceClient.Setup(c => c.GetCbIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new CommercialBankDto { Id = 1, Name = "Test Bank", Code = "TB" });
        configServiceClient.Setup(c => c.GetBouBranchByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new BranchDto { Id = 1, Name = "Head Office", BranchCode = "HO" });
        configServiceClient.Setup(c => c.GetAllBouBranchesAsync())
            .ReturnsAsync(new List<BranchDto>());

        var agentRepo = new Mock<IAgentDetailRepository>();
        agentRepo.Setup(r => r.FindAll()).ReturnsAsync(new List<AgentDetail>());

        var denomRepo = new Mock<IDenominationRepository>();
        denomRepo.Setup(r => r.FindAll()).ReturnsAsync(new List<Denomination>());

        var withdrawalRequestAgentRepo = new Mock<IWithdrawalRequestAgentRepository>();
        withdrawalRequestAgentRepo
            .Setup(r => r.GetByRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WithdrawalRequestAgent>());

        var withdrawalRequestDenomRepo = new Mock<IWithdrawalRequestDenominationRepository>();
        withdrawalRequestDenomRepo
            .Setup(r => r.GetByRequestAndOriginAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WithdrawalRequestDenomination>());

        var attachmentRepository = new Mock<IAttachmentRepository>();
        attachmentRepository.Setup(r => r.FindByEntityId(It.IsAny<string>())).ReturnsAsync(new List<Attachment>());

        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository.Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>())).ReturnsAsync((PublicHoliday?)null);
        publicHolidayRepository.Setup(r => r.FindByActive(It.IsAny<bool>())).ReturnsAsync(new List<PublicHoliday>());

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
            agentRepo.Object,
            configServiceClient.Object,
            denomRepo.Object,
            Mock.Of<IWithdrawalTransactionRefNumberGenerator>(),
            uow.Object,
            requestRepo.Object,
            withdrawalRequestAgentRepo.Object,
            withdrawalRequestDenomRepo.Object,
            workflowServiceClient.Object,
            workflowService.Object,
            Mock.Of<IWithdrawalStatusResolver>(),
            attachmentRepository.Object,
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
                        new Claim("appRole", $"{SystemPolicy.SystemSettings.AppAcronym}:{roleName}"),
                        new Claim("appRoleId", $"{SystemPolicy.SystemSettings.AppAcronym}:{RoleId}"),
                        new Claim("cbId", "1"),
                    ])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, workflowService);
    }

    // ── Details view-model gating ──────────────────────────────────────────

    [Fact]
    public async Task Details_Stale_PreSubmission_CbMaker_Has_CanEdit_True_And_No_Other_Actions()
    {
        var request = NewRequest(Yesterday);
        var (controller, _) = BuildController(request, RolePolicy.CbMaker, [new WorkflowTransitionDto { ActionName = "SomeAction" }]);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.IsStalePreSubmission);
        Assert.True(vm.CanEdit);
        Assert.False(vm.CanEditRtgs);
        Assert.Empty(vm.AvailableTransitions);
    }

    [Fact]
    public async Task Details_Stale_PreSubmission_CbApprover_Has_No_Actions_At_All()
    {
        var request = NewRequest(Yesterday);
        var (controller, _) = BuildController(request, RolePolicy.CbApprover, [new WorkflowTransitionDto { ActionName = "SomeAction" }]);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.True(vm.IsStalePreSubmission);
        Assert.False(vm.CanEdit); // CbApprover never satisfies canEdit's CbMaker check
        Assert.False(vm.CanEditRtgs);
        Assert.Empty(vm.AvailableTransitions);
    }

    [Fact]
    public async Task Details_NonStale_PreSubmission_Is_Unaffected()
    {
        var request = NewRequest(Tomorrow);
        var transitions = new List<WorkflowTransitionDto> { new() { ActionName = "SubmitForInternalApproval" } };
        var (controller, _) = BuildController(request, RolePolicy.CbApprover, transitions);

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.False(vm.IsStalePreSubmission);
        Assert.True(vm.CanEditRtgs);
        Assert.NotEmpty(vm.AvailableTransitions);
    }

    // ── Edit / Delete reachability ─────────────────────────────────────────

    [Fact]
    public async Task Edit_Stale_PendingInternalApproval_Reaches_Edit_Page()
    {
        var request = NewRequest(Yesterday);
        var (controller, _) = BuildController(request, RolePolicy.CbMaker);

        var result = await controller.Edit(request.Id, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_NonStale_PendingInternalApproval_Still_Redirects_To_Details()
    {
        var request = NewRequest(Tomorrow);
        var (controller, _) = BuildController(request, RolePolicy.CbMaker);

        var result = await controller.Edit(request.Id, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
    }

    [Fact]
    public async Task Delete_Stale_PendingInternalApproval_Succeeds()
    {
        var request = NewRequest(Yesterday);
        var (controller, _) = BuildController(request, RolePolicy.CbMaker);

        var result = await controller.Delete(request.Id, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("CbRequests", redirect.ControllerName);
        Assert.True(request.IsDeleted);
    }

    [Fact]
    public async Task Delete_NonStale_PendingInternalApproval_Still_Blocked()
    {
        var request = NewRequest(Tomorrow);
        var (controller, _) = BuildController(request, RolePolicy.CbMaker);

        var result = await controller.Delete(request.Id, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(request.IsDeleted);
        Assert.NotNull(controller.TempData["Error"]);
    }

    // ── Server-side rejection on the other actions ───────────────────────

    [Fact]
    public async Task ExecuteAction_On_Stale_Request_Is_Blocked_Without_Executing_Transition()
    {
        var request = NewRequest(Yesterday);
        var (controller, workflowService) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.ExecuteAction(request.Id, "SomeAction", "remarks", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAction_On_NonStale_Request_Executes_Normally()
    {
        var request = NewRequest(Tomorrow);
        var (controller, workflowService) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.ExecuteAction(request.Id, "SomeAction", "remarks", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRtgs_On_Stale_Request_Is_Blocked()
    {
        var request = NewRequest(Yesterday);
        var (controller, _) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.UpdateRtgs(request.Id, null, "RTGS-CODE", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(request.RtgsCode);
    }

    [Fact]
    public async Task SetSameDayReason_On_Stale_Request_Is_Blocked()
    {
        var request = NewRequest(Yesterday);
        var (controller, _) = BuildController(request, RolePolicy.CbApprover);

        var result = await controller.SetSameDayReason(request.Id, "A valid reason long enough to pass validation", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.False(request.RequiresSpecialApproval);
    }

    // ── SaveDraft: fixing the date is the only way through ─────────────────
    // SaveDraft itself has no IsStalePreSubmission check — it doesn't need one, because its
    // existing, unconditional "collection date can't be in the past" guard already rejects a
    // stale request's edit unless a real, non-past date is submitted. Covering that explicitly
    // here, per this ticket's own Tests section, rather than just trusting the pre-existing guard.

    private static SaveWithdrawalRequestViewModel SaveDraftVmWithDate(WithdrawalRequest request, DateOnly collectionDate) => new()
    {
        Id = request.Id,
        CbCode = request.CbCode,
        BouBranchId = request.BouBranchId,
        BouBranchCode = request.BouBranchCode,
        CollectionDate = collectionDate,
        IsLumpSum = true,
        LumpSumAmount = 10_000_000m,
    };

    private static CbWithdrawRequestsController BuildSaveDraftController(WithdrawalRequest request)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests.FindById(request.Id)).ReturnsAsync(request);
        uow.Setup(u => u.StatusHistory.GetByRequestAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(u => u.Denominations).Returns(Mock.Of<IWithdrawalRequestDenominationRepository>());
        uow.Setup(u => u.WithdrawalAgents).Returns(Mock.Of<IWithdrawalRequestAgentRepository>());

        var txnRefGenerator = new Mock<IWithdrawalTransactionRefNumberGenerator>();
        txnRefGenerator
            .Setup(g => g.GenerateAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("REF-TEST");

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var controller = new CbWithdrawRequestsController(
            authorizationService.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            txnRefGenerator.Object,
            uow.Object,
            requestRepo.Object,
            Mock.Of<IWithdrawalRequestAgentRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            Mock.Of<IWorkflowServiceClient>(),
            Mock.Of<IWorkflowService>(),
            Mock.Of<IWithdrawalStatusResolver>(),
            Mock.Of<IAttachmentRepository>(),
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
                        new Claim(ClaimTypes.Name, "cb-maker"),
                        new Claim(ClaimTypes.NameIdentifier, "cb-maker-id"),
                        new Claim(RolePolicy.CbIdClaim, request.CbId.ToString()),
                        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
                    ])),
                },
            },
            Url = Mock.Of<IUrlHelper>(),
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    [Fact]
    public async Task SaveDraft_Edit_Of_Stale_Request_With_Still_Past_Date_Is_Rejected()
    {
        var request = NewRequest(Yesterday, WithdrawalStatusNames.PendingInternalApproval);
        request.CbCode = "TB";
        request.BouBranchCode = "HO";
        var controller = BuildSaveDraftController(request);
        var vm = SaveDraftVmWithDate(request, Yesterday);

        var result = await controller.SaveDraft(vm, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(
            badRequest.Value!.GetType().GetProperty("errors")!.GetValue(badRequest.Value));
        Assert.Contains("Collection date cannot be in the past.", errors);
    }

    [Fact]
    public async Task SaveDraft_Edit_Of_Stale_Request_With_New_Future_Date_Succeeds()
    {
        var request = NewRequest(Yesterday, WithdrawalStatusNames.PendingInternalApproval);
        request.CbCode = "TB";
        request.BouBranchCode = "HO";
        var controller = BuildSaveDraftController(request);
        var vm = SaveDraftVmWithDate(request, Tomorrow);

        var result = await controller.SaveDraft(vm, CancellationToken.None);

        Assert.IsNotType<BadRequestObjectResult>(result);
        Assert.Equal(Tomorrow, request.CollectionDate);
    }
}
