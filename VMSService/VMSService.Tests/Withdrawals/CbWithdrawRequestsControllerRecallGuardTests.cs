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
/// Recall is intended to be available at every non-terminal status. WorkflowService's config
/// already only wires RequestRecall from the five legitimate non-terminal statuses, but that's
/// config, not a guarantee — this locks the same rule down explicitly in code, so a misconfigured
/// WorkflowService can't let a terminal-status request be recalled.
/// </summary>
public class CbWithdrawRequestsControllerRecallGuardTests
{
    private const int RoleId = 1;

    private static WithdrawalRequest NewRequest(string statusName) => new()
    {
        Id = Guid.NewGuid(),
        CbId = 1,
        StatusTypeId = 3,
        StatusTypeName = statusName,
        CollectionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        TransactionRefNumber = null, // skips the applied-penalties branch, irrelevant here
        TotalAmount = 10_000_000m,
    };

    private static (CbWithdrawRequestsController Controller, Mock<IWorkflowService> WorkflowService) BuildController(
        WithdrawalRequest request, List<WorkflowTransitionDto>? transitions = null)
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
        denomRepo.Setup(r => r.FindAll()).ReturnsAsync(new List<VMSService.Core.Services.Stocks.StockEntities.Denomination>());

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

        // Stubbed to allow the transition regardless — proves the guard added for this ticket is a
        // real code-level check, not reliant on this (or WorkflowService's real config) saying no.
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
                        new Claim("appRole", $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
                        new Claim("appRoleId", $"{SystemPolicy.SystemSettings.AppAcronym}:{RoleId}"),
                        new Claim("cbId", "1"),
                    ])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, workflowService);
    }

    public static IEnumerable<object[]> TerminalStatuses =>
    [
        [WithdrawalStatusNames.Approved],
        [WithdrawalStatusNames.Rejected],
        [WithdrawalStatusNames.RejectComplete],
        [WithdrawalStatusNames.RejectExceptionalApproval],
        [WithdrawalStatusNames.RejectExceptionalApprovalComplete],
        [WithdrawalStatusNames.RecallComplete],
    ];

    [Theory]
    [MemberData(nameof(TerminalStatuses))]
    public async Task RequestRecall_On_Terminal_Status_Is_Rejected_Even_When_WorkflowService_Would_Allow_It(string terminalStatus)
    {
        var request = NewRequest(terminalStatus);
        var (controller, workflowService) = BuildController(request);

        var result = await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.RequestRecall, "remarks", CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Never);
    }

    public static IEnumerable<object[]> LegitimateNonTerminalStatuses =>
    [
        [WithdrawalStatusNames.PendingRTGSVerification],
        [WithdrawalStatusNames.PendingWithdrawalConfirmation],
        [WithdrawalStatusNames.PendingExceptionalApproval],
        [WithdrawalStatusNames.PendingVerification],
        [WithdrawalStatusNames.PendingApproval],
    ];

    [Theory]
    [MemberData(nameof(LegitimateNonTerminalStatuses))]
    public async Task RequestRecall_On_Legitimate_NonTerminal_Status_Is_Unaffected(string nonTerminalStatus)
    {
        var request = NewRequest(nonTerminalStatus);
        var (controller, workflowService) = BuildController(request);

        var result = await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.RequestRecall, "remarks", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }
}
