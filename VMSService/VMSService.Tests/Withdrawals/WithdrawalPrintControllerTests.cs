using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Authorization;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 23: <c>PrintRequest</c> moved out of <c>CbWithdrawRequestsController</c> into this
/// small neutral controller — it's the one deliberate exception to the CB/BOU split (read-only,
/// renders identically for both sides), gated by ticket 08's ownership check (own-bank CB, or any
/// BOU role) rather than <c>CbOnly</c>/<c>BouOnly</c>. Uses the same real-authorization-pipeline
/// pattern as <see cref="CbWithdrawRequestsControllerOwnershipTests"/>: a real
/// <see cref="IAuthorizationService"/> backed by <see cref="CbOwnershipAuthorizationHandler"/>,
/// not a mock that always says yes.
/// </summary>
public class WithdrawalPrintControllerTests
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

    private static ClaimsPrincipal BouUser(string role) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "bou-user"),
        new Claim(ClaimTypes.NameIdentifier, "bou-user-id"),
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
    ]));

    private static (WithdrawalPrintController Controller, WithdrawalRequest Request) BuildController(
        int requestCbId, ClaimsPrincipal user)
    {
        var request = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            CbId = requestCbId,
            CbCode = "TB",
            StatusTypeName = WithdrawalStatusNames.Approved,
            CollectionDate = DateOnly.FromDateTime(DateTime.Today),
            BouBranchId = 1,
            BouBranchCode = "HO",
            TotalAmount = 10_000_000m,
            TransactionRefNumber = "REF-TEST",
        };

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var configServiceClient = new Mock<IConfigurationsServiceClient>();
        configServiceClient.Setup(c => c.GetCbIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new CommercialBankDto { Id = requestCbId, Name = "Test Bank", Code = "TB" });
        configServiceClient.Setup(c => c.GetBouBranchByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new BranchDto { Id = 1, Name = "Head Office", BranchCode = "HO" });

        var statusResolver = new Mock<IWithdrawalStatusResolver>();
        statusResolver.Setup(r => r.GetStatusIdAsync(It.IsAny<string>())).ReturnsAsync(1);

        var authorizationService = new ServiceCollection()
            .AddLogging()
            .AddAuthorizationCore()
            .AddSingleton<IAuthorizationHandler, CbOwnershipAuthorizationHandler>()
            .BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();

        var controller = new WithdrawalPrintController(
            authorizationService,
            Mock.Of<IAgentDetailRepository>(),
            configServiceClient.Object,
            uow.Object,
            statusResolver.Object,
            Mock.Of<IIdentityService>(),
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            },
        };

        return (controller, request);
    }

    [Fact]
    public async Task Bank_A_User_Against_Bank_B_Request_Is_Denied()
    {
        var (controller, request) = BuildController(OtherBankId, CbUser(OwnBankId));

        var result = await controller.PrintRequest(request.Id, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Bank_A_User_Against_Bank_A_Request_Is_Unaffected()
    {
        var (controller, request) = BuildController(OwnBankId, CbUser(OwnBankId));

        var result = await controller.PrintRequest(request.Id, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }

    [Theory]
    [InlineData(RolePolicy.BouChecker)]
    [InlineData(RolePolicy.PanelA)]
    [InlineData(RolePolicy.PanelB)]
    [InlineData(RolePolicy.RemittanceOfficer)]
    [InlineData(RolePolicy.BackOffice)]
    [InlineData(RolePolicy.CurrencyApprover)]
    public async Task Any_Bou_Role_Is_Unaffected_On_Any_Banks_Request(string bouRole)
    {
        var (controller, request) = BuildController(OtherBankId, BouUser(bouRole));

        var result = await controller.PrintRequest(request.Id, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }
}
