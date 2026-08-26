using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Authorization;
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
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using Xunit;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 34: <c>WithdrawalRequestsController</c> had no authorization guard at all until ticket
/// 09's follow-up added a blanket <c>BouOnly</c> (all 6 general BOU roles). A `/code-review` run
/// then found that set includes <c>Bou Checker</c>/<c>Head of Back Office</c>, neither of which the
/// user confirmed does withdrawal work — so the controller now gates on
/// <c>RolePolicy.WithdrawalBouRoles</c>, a narrower, withdrawal-scoped set of exactly 5 roles.
/// Exercised via <c>AvailableBoxes</c> — the check is the very first line, so an unmocked
/// <c>IWithdrawalRequestRepository</c> (returning null → NotFound) is enough to prove the gate
/// passed without needing to mock the rest of any given action's business logic.
/// </summary>
public class WithdrawalRequestsControllerAuthorizationTests
{
    private static WithdrawalRequestsController BuildController(ClaimsPrincipal user)
    {
        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests.FindById(It.IsAny<Guid>())).ReturnsAsync((WithdrawalRequest?)null);

        var controller = new WithdrawalRequestsController(
            Mock.Of<IWithdrawalRequestRepository>(),
            Mock.Of<IWorkflowServiceClient>(),
            Mock.Of<IWorkflowService>(),
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
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
                HttpContext = new DefaultHttpContext { User = user },
            },
        };

        return controller;
    }

    private static ClaimsPrincipal UserWithRole(string role) => new(new ClaimsIdentity(
    [
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
    ]));

    [Theory]
    [InlineData(RolePolicy.BackOffice)]
    [InlineData(RolePolicy.RemittanceOfficer)]
    [InlineData(RolePolicy.CurrencyApprover)]
    [InlineData(RolePolicy.PanelB)]
    [InlineData(RolePolicy.PanelA)]
    public async Task Confirmed_Withdrawal_Role_Is_Unaffected(string role)
    {
        var controller = BuildController(UserWithRole(role));

        var result = await controller.AvailableBoxes(1, Guid.NewGuid(), CancellationToken.None);

        Assert.IsNotType<ForbidResult>(result);
    }

    [Theory]
    [InlineData(RolePolicy.BouChecker)]
    [InlineData("Head of Back Office")]
    public async Task Role_Not_Confirmed_For_Withdrawal_Work_Is_Denied(string role)
    {
        var controller = BuildController(UserWithRole(role));

        var result = await controller.AvailableBoxes(1, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task User_With_No_Role_Claim_Is_Denied()
    {
        var controller = BuildController(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await controller.AvailableBoxes(1, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }
}
