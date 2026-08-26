using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
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
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
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
/// Ticket 08: every action on a withdrawal request must deny a CB user who isn't from the
/// request's own bank, while leaving same-bank CB users unaffected. This wires the controller up
/// with the *real* authorization pipeline (<see cref="CbOwnershipAuthorizationHandler"/> behind a
/// real <see cref="IAuthorizationService"/>), not a mock that always says yes — a mocked
/// authorization service is exactly the kind of test that would have hidden the original
/// Guid/int claim-parsing bug described in the ticket.
///
/// Ticket 09 correction: every BOU role is also denied on every action here — the controller is
/// CB-only now that ticket 23 moved the one deliberate exception, PrintRequest, out entirely.
/// </summary>
public class CbWithdrawRequestsControllerOwnershipTests
{
    private const int OwnBankId = 1;
    private const int OtherBankId = 2;

    private static ClaimsPrincipal CbUser(int cbId) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "cb-user"),
        new Claim(ClaimTypes.NameIdentifier, "cb-user-id"),
        new Claim(RolePolicy.CbIdClaim, cbId.ToString()),
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
    ]));

    internal static ClaimsPrincipal BouUser(string role) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "bou-user"),
        new Claim(ClaimTypes.NameIdentifier, "bou-user-id"),
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
    ]));

    // internal, not private — reused by CbWithdrawRequestsControllerMakerCheckerTests (ticket 09),
    // which needs the same fully-mocked controller to exercise a different guard on the same
    // action set.
    internal static (CbWithdrawRequestsController Controller, WithdrawalRequest Request) BuildController(
        int requestCbId, string statusName, ClaimsPrincipal user)
    {
        var request = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            CbId = requestCbId,
            CbCode = "TB",
            StatusTypeId = 1,
            StatusTypeName = statusName,
            CollectionDate = DateOnly.FromDateTime(DateTime.Today),
            BouBranchId = 1,
            BouBranchCode = "HO",
            IsLumpSum = true,
            TotalAmount = 10_000_000m,
            TransactionRefNumber = null, // skips the applied-penalties branch on Details
        };

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        uow.Setup(u => u.Requests.FindById(request.Id)).ReturnsAsync(request);
        uow.Setup(u => u.StatusHistory.GetByRequestAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WithdrawalRequestStatusHistory>());
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        // SaveDraft's helpers reach these via _uow directly (not the standalone repo params
        // below) — Moq only auto-mocks nested properties touched inside a Setup chain, so they
        // need an explicit stand-in or SaveDenominationsAsync/SaveAgentsAsync NRE on a null getter.
        uow.Setup(u => u.Denominations).Returns(Mock.Of<IWithdrawalRequestDenominationRepository>());
        uow.Setup(u => u.WithdrawalAgents).Returns(Mock.Of<IWithdrawalRequestAgentRepository>());

        var withdrawalRequestRepo = new Mock<IWithdrawalRequestRepository>();
        withdrawalRequestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var workflowServiceClient = new Mock<IWorkflowServiceClient>();
        workflowServiceClient.Setup(c => c.FindStatusById(It.IsAny<int>()))
            .ReturnsAsync((int id) => new StatusTypeDto { Id = id, Name = statusName });

        var configServiceClient = new Mock<IConfigurationsServiceClient>();
        configServiceClient.Setup(c => c.GetCbIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new CommercialBankDto { Id = requestCbId, Name = "Test Bank", Code = "TB" });
        configServiceClient.Setup(c => c.GetBouBranchByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new BranchDto { Id = 1, Name = "Head Office", BranchCode = "HO" });
        configServiceClient.Setup(c => c.GetAllBouBranchesAsync())
            .ReturnsAsync(new List<BranchDto>());

        // Create() also needs these — the other 7 actions never reach them.
        var agentRepo = new Mock<IAgentDetailRepository>();
        agentRepo.Setup(r => r.FindAll()).ReturnsAsync(new List<AgentDetail>());

        var denomRepo = new Mock<IDenominationRepository>();
        denomRepo.Setup(r => r.FindAll()).ReturnsAsync(new List<Denomination>());

        var attachmentRepository = new Mock<IAttachmentRepository>();
        attachmentRepository.Setup(r => r.FindByEntityId(It.IsAny<string>())).ReturnsAsync(new List<Attachment>());

        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository.Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>())).ReturnsAsync((PublicHoliday?)null);
        publicHolidayRepository.Setup(r => r.FindByActive(It.IsAny<bool>())).ReturnsAsync(new List<PublicHoliday>());

        var txnRefGenerator = new Mock<IWithdrawalTransactionRefNumberGenerator>();
        txnRefGenerator
            .Setup(g => g.GenerateAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("REF-TEST");

        // Real DI-backed authorization pipeline — this is the point of the test: it exercises the
        // handler's actual claim parsing rather than a stubbed always-succeed/always-fail mock.
        var authorizationService = new ServiceCollection()
            .AddLogging()
            .AddAuthorizationCore()
            .AddSingleton<IAuthorizationHandler, CbOwnershipAuthorizationHandler>()
            .BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();

        var controller = new CbWithdrawRequestsController(
            authorizationService,
            agentRepo.Object,
            configServiceClient.Object,
            denomRepo.Object,
            txnRefGenerator.Object,
            uow.Object,
            withdrawalRequestRepo.Object,
            Mock.Of<IWithdrawalRequestAgentRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            workflowServiceClient.Object,
            Mock.Of<IWorkflowService>(),
            Mock.Of<IWithdrawalStatusResolver>(),
            attachmentRepository.Object,
            publicHolidayRepository.Object,
            Mock.Of<IAppliedPenaltyRepository>(),
            Mock.Of<ILogger<CbWithdrawRequestsController>>(),
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            },
            Url = Mock.Of<IUrlHelper>(),
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, request);
    }

    internal static SaveWithdrawalRequestViewModel BuildValidSaveDraftVm(WithdrawalRequest request) => new()
    {
        Id = request.Id,
        CbCode = request.CbCode,
        BouBranchId = request.BouBranchId,
        BouBranchCode = request.BouBranchCode,
        CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
        IsLumpSum = true,
        LumpSumAmount = 10_000_000m,
    };

    // Each case pairs a harmless request status (chosen so the action exits quickly once past the
    // ownership gate, without needing to mock the rest of that action's business logic — Details
    // is the one exception, kept as a full render since CbWithdrawRequestsControllerDetailsTests
    // already established that setup) with the actual invocation.
    public static IEnumerable<object[]> ActionCases()
    {
        yield return new object[] { "Details", WithdrawalStatusNames.Approved,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.Details(r.Id, CancellationToken.None)) };

        yield return new object[] { "Edit", WithdrawalStatusNames.Approved,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.Edit(r.Id, CancellationToken.None)) };

        yield return new object[] { "SaveDraft", WithdrawalStatusNames.Draft,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.SaveDraft(BuildValidSaveDraftVm(r), CancellationToken.None)) };

        yield return new object[] { "Delete", WithdrawalStatusNames.Approved,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.Delete(r.Id, CancellationToken.None)) };

        yield return new object[] { "UpdateRtgs", WithdrawalStatusNames.Approved,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.UpdateRtgs(r.Id, null, null, CancellationToken.None)) };

        yield return new object[] { "ExecuteAction", WithdrawalStatusNames.PendingInternalApproval,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.ExecuteAction(r.Id, "SomeAction", "remarks", CancellationToken.None)) };

        yield return new object[] { "SetSameDayReason", WithdrawalStatusNames.PendingInternalApproval,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.SetSameDayReason(r.Id, new string('a', ReasonRules.MinLength), CancellationToken.None)) };
    }

    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task Bank_A_User_Against_Bank_B_Request_Is_Denied_On_Every_Action(
        string actionName, string status, Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>> invoke)
    {
        var (controller, request) = BuildController(OtherBankId, status, CbUser(OwnBankId));

        var result = await invoke(controller, request);

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task Bank_A_User_Against_Bank_A_Request_Is_Unaffected(
        string actionName, string status, Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>> invoke)
    {
        var (controller, request) = BuildController(OwnBankId, status, CbUser(OwnBankId));

        var result = await invoke(controller, request);

        Assert.IsNotType<ForbidResult>(result);
    }

    // Ticket 09 (IsCbUser() fix): CbOwnershipAuthorizationHandler's "any BOU role bypasses
    // ownership" rule is blanket across every action that calls it, so without IsCbUser() a BOU
    // user who knew a request's GUID could edit its RTGS code, execute CB-side workflow
    // transitions, view its internal review history, or set the CB Approver's
    // exceptional-approval reason. PrintRequest was the one deliberate exception to this — ticket
    // 23 moved it out into WithdrawalPrintController entirely, so every action left on this
    // controller is CB-only; see WithdrawalPrintControllerTests for PrintRequest's own coverage.
    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task Any_Bou_Role_Is_Denied_On_Every_Action(
        string actionName, string status, Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>> invoke)
    {
        var (controller, request) = BuildController(OtherBankId, status, BouUser(RolePolicy.PanelA));

        var result = await invoke(controller, request);

        Assert.IsType<ForbidResult>(result);
    }
}
