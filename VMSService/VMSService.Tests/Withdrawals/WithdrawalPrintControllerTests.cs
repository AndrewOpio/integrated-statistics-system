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
        new Claim("isStaff", "false"),
    ]));

    private static ClaimsPrincipal BouUser(string role) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "bou-user"),
        new Claim(ClaimTypes.NameIdentifier, "bou-user-id"),
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
        new Claim("isStaff", "true"),
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

    // ── DispatchVariance (ticket 18): the receipt showed the requested total unconditionally,
    // even when the BOU-origin dispatch rows above it summed to something else. Same formula as
    // BouWithdrawalRequestDetailsViewModel.DispatchVariance (WithdrawalRequestsControllerDetailsTests),
    // kept in sync deliberately so the two pages can't disagree about what "matched" means.

    private static WithdrawalRequestDenomination NewDenomRow(string origin, decimal amount) => new()
    {
        Origin = origin,
        BundleType = BundleTypes.Reissue,
        Amount = amount,
        DenominationValue = amount,
    };

    [Fact]
    public async Task No_Dispatch_Rows_Variance_Is_Full_Shortfall()
    {
        var (controller, request) = BuildController(OwnBankId, CbUser(OwnBankId));

        var view = Assert.IsType<ViewResult>(await controller.PrintRequest(request.Id, CancellationToken.None));
        var vm = Assert.IsType<VMSService.Core.Models.CbWithdrawRequest.WithdrawalPrintViewModel>(view.Model);

        Assert.Equal(-request.TotalAmount, vm.DispatchVariance);
    }

    [Fact]
    public async Task Dispatch_Matching_Requested_Amount_Has_Zero_Variance()
    {
        var (controller, request) = BuildController(OwnBankId, CbUser(OwnBankId));
        request.WithdrawalRequestDenominations.Add(NewDenomRow(WithdrawalOrigin.Bou, request.TotalAmount));

        var view = Assert.IsType<ViewResult>(await controller.PrintRequest(request.Id, CancellationToken.None));
        var vm = Assert.IsType<VMSService.Core.Models.CbWithdrawRequest.WithdrawalPrintViewModel>(view.Model);

        Assert.Equal(0m, vm.DispatchVariance);
    }

    [Fact]
    public async Task Dispatch_Short_Of_Requested_Amount_Has_Nonzero_Variance()
    {
        var (controller, request) = BuildController(OwnBankId, CbUser(OwnBankId));
        request.WithdrawalRequestDenominations.Add(NewDenomRow(WithdrawalOrigin.Bou, request.TotalAmount - 4_000_000m));

        var view = Assert.IsType<ViewResult>(await controller.PrintRequest(request.Id, CancellationToken.None));
        var vm = Assert.IsType<VMSService.Core.Models.CbWithdrawRequest.WithdrawalPrintViewModel>(view.Model);

        Assert.Equal(-4_000_000m, vm.DispatchVariance);
    }

    [Fact]
    public async Task Cb_Origin_Rows_Excluded_From_DispatchVariance()
    {
        // Only what the bank actually dispatched counts — the CB's own wishlist rows must not be
        // mistaken for dispatch, or a request with no dispatch at all would look "matched".
        var (controller, request) = BuildController(OwnBankId, CbUser(OwnBankId));
        request.WithdrawalRequestDenominations.Add(NewDenomRow(WithdrawalOrigin.Cb, request.TotalAmount));

        var view = Assert.IsType<ViewResult>(await controller.PrintRequest(request.Id, CancellationToken.None));
        var vm = Assert.IsType<VMSService.Core.Models.CbWithdrawRequest.WithdrawalPrintViewModel>(view.Model);

        Assert.Equal(-request.TotalAmount, vm.DispatchVariance);
    }
}
