using System.Security.Claims;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
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
using VMSService.Core.Services.Workflows;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using Xunit;
using static VMSService.Core.Static.SystemPolicy;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Controller-level coverage of the BOU landing page (Index + History): the terminal-and-past-date
/// exclusion wired end-to-end, hero-card/status counts narrowed to the active set, the
/// Total Processed lifetime-sum fix, and branch scoping propagated into the repository query.
/// Complements the repository-level tests, which cover the raw query logic.
/// </summary>
public class WithdrawalRequestsControllerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly LastWeek = Today.AddDays(-7);
    private static readonly DateOnly ThirteenMonthsAgo = Today.AddMonths(-13);

    private static WithdrawalRequest NewRequest(string statusName, DateOnly? collectionDate = null, string bouBranchCode = "KLA", decimal totalAmount = 10_000_000m)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 1,
            StatusTypeName = statusName,
            CollectionDate = collectionDate ?? Today,
            BouBranchCode = bouBranchCode,
            TotalAmount = totalAmount,
            CreatedDate = DateTime.Now,
        };

    private static WithdrawalRequestsController BuildController(List<WithdrawalRequest> allRequests, params string[] roles)
    {
        if (roles.Length == 0) roles = [Roles.RemittancesOfficer];

        var repo = new Mock<IWithdrawalRequestRepository>();

        repo.Setup(r => r.GetActiveAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly today, string? branch, CancellationToken _) =>
                allRequests.Where(r => !(WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName) && r.CollectionDate < today)
                                     && (string.IsNullOrEmpty(branch) || r.BouBranchCode == branch)));

        repo.Setup(r => r.GetHistoryAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly today, DateOnly from, DateOnly to, string? branch, CancellationToken _) =>
                allRequests.Where(r => WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName)
                                     && r.CollectionDate < today
                                     && r.CollectionDate >= from
                                     && r.CollectionDate <= to
                                     && (string.IsNullOrEmpty(branch) || r.BouBranchCode == branch)));

        repo.Setup(r => r.SumApprovedAmountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? branch, CancellationToken _) =>
                allRequests.Where(r => r.StatusTypeName == WithdrawalStatusNames.Approved
                                     && (string.IsNullOrEmpty(branch) || r.BouBranchCode == branch))
                           .Sum(r => r.TotalAmount));

        var configClient = new Mock<IConfigurationsServiceClient>();
        configClient.Setup(c => c.GetAllCommercialBankAsync())
            .ReturnsAsync([]);
        configClient.Setup(c => c.GetAllBouBranchesAsync())
            .ReturnsAsync([
                new BranchDto { Id = 1, Name = "Kampala", BranchCode = "KLA" },
                new BranchDto { Id = 2, Name = "Mbale", BranchCode = "MBL" },
            ]);

        var wrDenomRepo = new Mock<IWithdrawalRequestDenominationRepository>();
        wrDenomRepo.Setup(r => r.GetRequestIdsWithBouOriginAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = new WithdrawalRequestsController(
            repo.Object,
            Mock.Of<IWorkflowServiceClient>(),
            Mock.Of<IWorkflowService>(),
            Mock.Of<IAgentDetailRepository>(),
            configClient.Object,
            Mock.Of<IDenominationRepository>(),
            wrDenomRepo.Object,
            Mock.Of<IAttachmentRepository>(),
            Mock.Of<IWithdrawalUnitOfWork>(),
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
                        roles.Select(r => new Claim(ClaimType.AppRole, $"{SystemSettings.AppAcronym}:{r}")))),
                },
            },
        };

        return controller;
    }

    [Fact]
    public async Task Index_Excludes_Terminal_Request_With_Past_Collection_Date()
    {
        var controller = BuildController([NewRequest(WithdrawalStatusNames.RecallComplete, LastWeek)]);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.Empty(vm.Requests);
    }

    [Fact]
    public async Task Index_Includes_Terminal_Request_With_Todays_Collection_Date()
    {
        var controller = BuildController([NewRequest(WithdrawalStatusNames.Approved, Today)]);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.Single(vm.Requests);
    }

    [Fact]
    public async Task Index_Keeps_Rejected_Visible_Past_Date_Since_Return_RTGS_Is_Still_Owed()
    {
        var controller = BuildController([NewRequest(WithdrawalStatusNames.Rejected, LastWeek)]);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        var row = Assert.Single(vm.Requests);
        Assert.True(row.ReturnRtgsPending);
    }

    [Fact]
    public async Task Index_Hero_Card_Counts_Reflect_Only_The_Active_Set()
    {
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.PendingInternalApproval),
            NewRequest(WithdrawalStatusNames.PendingWithdrawalConfirmation),
            NewRequest(WithdrawalStatusNames.RecallRequested),
            NewRequest(WithdrawalStatusNames.RecallComplete, LastWeek), // excluded — terminal + past
        ]);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.Equal(1, vm.PendingCbCount);
        Assert.Equal(1, vm.UnderBouProcessing);
        Assert.Equal(1, vm.RecallCount);
        Assert.Equal(3, vm.Requests.Count());
    }

    [Fact]
    public async Task Index_TotalProcessed_Is_A_Lifetime_Sum_Unaffected_By_The_Active_History_Split()
    {
        // Regression check for the pre-existing bug: this card was always UGX 0 because it summed
        // a collection that had already excluded Approved rows.
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.Approved, ThirteenMonthsAgo, totalAmount: 15_000_000m),
            NewRequest(WithdrawalStatusNames.Approved, Today, totalAmount: 25_000_000m),
        ]);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.Equal(40_000_000m, vm.TotalProcessed);
    }

    [Fact]
    public async Task Index_With_Explicit_BranchCode_Scopes_Rows_And_TotalProcessed_To_That_Branch()
    {
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "KLA"),
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "MBL"),
            NewRequest(WithdrawalStatusNames.Approved, ThirteenMonthsAgo, bouBranchCode: "KLA", totalAmount: 5_000_000m),
            NewRequest(WithdrawalStatusNames.Approved, ThirteenMonthsAgo, bouBranchCode: "MBL", totalAmount: 9_000_000m),
        ]);

        var view = Assert.IsType<ViewResult>(await controller.Index(branchCode: "KLA"));
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        var row = Assert.Single(vm.Requests);
        Assert.Equal("KLA", row.BouBranchCode);
        Assert.Equal(5_000_000m, vm.TotalProcessed);
    }

    [Fact]
    public async Task Index_Back_Office_Sees_All_Branches_Via_CanViewAllLocations()
    {
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "KLA"),
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "MBL"),
        ], Roles.BackOffice);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.Equal(2, vm.Requests.Count());
    }

    [Fact]
    public async Task Index_Panel_A_User_Is_Scoped_To_Their_Own_Branch_Role()
    {
        // BM/ABM users carry a branch-named role (e.g. "Kampala Branch") alongside their panel
        // role — confirmed against IdentityDB (ticket 16) — rather than a substring collision
        // between the role name and the branch name.
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "KLA"),
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "MBL"),
        ], Roles.PanelA, "Kampala Branch");

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        var row = Assert.Single(vm.Requests);
        Assert.Equal("KLA", row.BouBranchCode);
    }

    [Fact]
    public async Task Index_Panel_A_User_With_No_Matching_Branch_Role_Sees_Nothing_Not_Everything()
    {
        // A Panel A/B user with a missing or misspelled branch role must not fall through to the
        // "no branch filter = show every branch" behavior GetActiveAsync/GetHistoryAsync apply
        // when branchCode is null/empty.
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "KLA"),
            NewRequest(WithdrawalStatusNames.PendingApproval, bouBranchCode: "MBL"),
        ], Roles.PanelA);

        var view = Assert.IsType<ViewResult>(await controller.Index());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.True(vm.NotAttachedToBranch);
        Assert.Empty(vm.Requests);
    }

    [Fact]
    public async Task History_Panel_A_User_With_No_Matching_Branch_Role_Sees_Nothing_Not_Everything()
    {
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.Approved, LastWeek, bouBranchCode: "KLA"),
            NewRequest(WithdrawalStatusNames.Approved, LastWeek, bouBranchCode: "MBL"),
        ], Roles.PanelA);

        var view = Assert.IsType<ViewResult>(await controller.History());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.True(vm.NotAttachedToBranch);
        Assert.Empty(vm.Requests);
    }

    [Fact]
    public async Task History_Defaults_To_The_Last_Week_When_No_Range_Given()
    {
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.Approved, LastWeek),           // in the default range
            NewRequest(WithdrawalStatusNames.Approved, Today),              // still active
            NewRequest(WithdrawalStatusNames.Rejected, LastWeek),           // not terminal
            NewRequest(WithdrawalStatusNames.Approved, ThirteenMonthsAgo),  // outside the default 1-week range
        ]);

        var view = Assert.IsType<ViewResult>(await controller.History());
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        var row = Assert.Single(vm.Requests);
        Assert.Equal(LastWeek, row.CollectionDate);
        Assert.NotNull(vm.DateRange);
        Assert.Equal(Today.AddDays(-7), vm.DateRange.From);
        Assert.Equal(Today, vm.DateRange.To);
    }

    [Fact]
    public async Task History_Has_No_Floor_On_From_Unlike_The_CB_Side()
    {
        // BOU is the authoritative side — an explicit range reaching further back than a year
        // (which the CB side would clamp) must be honored as-is.
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.Approved, ThirteenMonthsAgo),
        ]);

        var view = Assert.IsType<ViewResult>(await controller.History(from: ThirteenMonthsAgo, to: Today));
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        Assert.Single(vm.Requests);
        Assert.NotNull(vm.DateRange);
        Assert.Equal(ThirteenMonthsAgo, vm.DateRange.From);
    }

    [Fact]
    public async Task History_Date_Range_Composes_With_Branch_Scoping()
    {
        // A Panel A user narrowing the date range must still only see their own branch's rows —
        // the date filter and branch scoping are independent dimensions.
        var controller = BuildController([
            NewRequest(WithdrawalStatusNames.Approved, LastWeek, bouBranchCode: "KLA"),
            NewRequest(WithdrawalStatusNames.Approved, LastWeek, bouBranchCode: "MBL"),
        ], Roles.PanelA, "Kampala Branch");

        var view = Assert.IsType<ViewResult>(await controller.History(from: LastWeek, to: Today));
        var vm = Assert.IsType<BouWithdrawalRequestViewModel>(view.Model);

        var row = Assert.Single(vm.Requests);
        Assert.Equal("KLA", row.BouBranchCode);
    }
}
