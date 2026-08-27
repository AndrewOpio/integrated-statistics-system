using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Controller-level coverage of the CB landing page (Index + History) — bucket classification,
/// the new terminal-and-past-date exclusion wired end-to-end, the stale-pre-submission banner
/// flag, and cross-request-type behavior (withdrawals only; deposits are out of scope and left
/// untouched). Complements the repository-level tests, which cover the raw query logic.
/// </summary>
public class CbRequestsControllerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly LastWeek = Today.AddDays(-7);

    private static WithdrawalRequest NewRequest(int statusTypeId, string statusName, DateOnly? collectionDate = null, int cbId = 1)
        => new()
        {
            Id = Guid.NewGuid(),
            CbId = cbId,
            StatusTypeId = statusTypeId,
            StatusTypeName = statusName,
            CollectionDate = collectionDate ?? Today,
            TotalAmount = 10_000_000m,
            CreatedDate = DateTime.Now,
        };

    private static CbRequestsController BuildController(
        List<WithdrawalRequest> withdrawalRequests,
        List<DepositRequest>? depositRequests = null,
        Dictionary<int, string>? statusNamesById = null)
    {
        var uow = new Mock<IWithdrawalUnitOfWork>();
        var withdrawalRepo = new Mock<IWithdrawalRequestRepository>();

        withdrawalRepo
            .Setup(r => r.GetActiveByCbAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int cbId, DateOnly today, CancellationToken _) =>
                withdrawalRequests.Where(r => r.CbId == cbId
                    && !(WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName) && r.CollectionDate < today)));

        withdrawalRepo
            .Setup(r => r.GetHistoryByCbAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int cbId, DateOnly today, DateOnly from, DateOnly to, CancellationToken _) =>
                withdrawalRequests.Where(r => r.CbId == cbId
                    && WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName)
                    && r.CollectionDate < today
                    && r.CollectionDate >= from
                    && r.CollectionDate <= to));

        uow.Setup(u => u.Requests).Returns(withdrawalRepo.Object);

        var duow = new Mock<IDepositUnitOfWork>();
        var depositRepo = new Mock<IDepositRequestRepository>();
        depositRepo
            .Setup(r => r.GetByCbAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(depositRequests ?? []);
        duow.Setup(d => d.Requests).Returns(depositRepo.Object);

        var workflowClient = new Mock<IWorkflowServiceClient>();
        workflowClient
            .Setup(c => c.FindStatusById(It.IsAny<int>()))
            .ReturnsAsync((int id) => new StatusTypeDto { Id = id, Name = statusNamesById?.GetValueOrDefault(id) ?? "Unknown" });

        var requestRepository = new Mock<IWithdrawalRequestRepository>();

        var controller = new CbRequestsController(workflowClient.Object, uow.Object, duow.Object, requestRepository.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("cbId", "1")])),
                },
            },
        };

        return controller;
    }

    [Fact]
    public async Task Index_Excludes_Terminal_Request_With_Past_Collection_Date_From_Recent_Requests()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Approved, LastWeek),
        };
        var controller = BuildController(requests, statusNamesById: new() { [1] = WithdrawalStatusNames.Approved });

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        Assert.Empty(vm.RecentRequests);
    }

    [Fact]
    public async Task Index_Includes_Terminal_Request_With_Todays_Collection_Date()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Approved, Today),
        };
        var controller = BuildController(requests, statusNamesById: new() { [1] = WithdrawalStatusNames.Approved });

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        Assert.Single(vm.RecentRequests);
    }

    [Theory]
    [InlineData(WithdrawalStatusNames.Draft, "awaiting-action")]
    [InlineData(WithdrawalStatusNames.PendingInternalApproval, "awaiting-action")]
    [InlineData(WithdrawalStatusNames.ReturnedForRevision, "awaiting-action")]
    [InlineData(WithdrawalStatusNames.PendingRTGSVerification, "awaiting-action")]
    [InlineData(WithdrawalStatusNames.PendingExceptionalApproval, "pending-exceptional")]
    [InlineData(WithdrawalStatusNames.PendingWithdrawalConfirmation, "with-bou")]
    [InlineData(WithdrawalStatusNames.PendingVerification, "with-bou")]
    [InlineData(WithdrawalStatusNames.RecallRequested, "with-bou")]
    [InlineData(WithdrawalStatusNames.Rejected, "unsuccessful")]
    [InlineData(WithdrawalStatusNames.RejectComplete, "unsuccessful")]
    [InlineData(WithdrawalStatusNames.Expired, "unsuccessful")]
    [InlineData(WithdrawalStatusNames.Approved, "")]
    public async Task Index_Classifies_Each_Status_Into_The_Correct_Dashboard_Bucket(string status, string expectedBucket)
    {
        var requests = new List<WithdrawalRequest> { NewRequest(1, status, Today) };
        var controller = BuildController(requests, statusNamesById: new() { [1] = status });

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        var row = Assert.Single(vm.RecentRequests);
        Assert.Equal(expectedBucket, row.StatusBucket);
    }

    [Fact]
    public async Task Index_Bucket_Counts_Match_The_Rows_Actually_Shown()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Draft, Today),
            NewRequest(2, WithdrawalStatusNames.PendingExceptionalApproval, Today),
            NewRequest(3, WithdrawalStatusNames.PendingVerification, Today),
            NewRequest(4, WithdrawalStatusNames.Rejected, Today),
            NewRequest(5, WithdrawalStatusNames.Rejected, Today),
        };
        var statusNames = new Dictionary<int, string>
        {
            [1] = WithdrawalStatusNames.Draft,
            [2] = WithdrawalStatusNames.PendingExceptionalApproval,
            [3] = WithdrawalStatusNames.PendingVerification,
            [4] = WithdrawalStatusNames.Rejected,
            [5] = WithdrawalStatusNames.Rejected,
        };
        var controller = BuildController(requests, statusNamesById: statusNames);

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        Assert.Equal(1, vm.AwaitingActionCount);
        Assert.Equal(1, vm.PendingExceptionalApprovalCount);
        Assert.Equal(1, vm.WithBouCount);
        Assert.Equal(2, vm.UnsuccessfulCount);
        Assert.Equal(5, vm.RecentRequests.Count());
    }

    [Fact]
    public async Task Index_Only_Returns_Requests_For_The_Authenticated_CB()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Draft, Today, cbId: 1),
            NewRequest(1, WithdrawalStatusNames.Draft, Today, cbId: 2),
        };
        var controller = BuildController(requests, statusNamesById: new() { [1] = WithdrawalStatusNames.Draft });

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        Assert.Single(vm.RecentRequests);
    }

    [Fact]
    public async Task Index_Flags_StalePreSubmission_Request()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Draft, LastWeek),
        };
        var controller = BuildController(requests, statusNamesById: new() { [1] = WithdrawalStatusNames.Draft });

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        var row = Assert.Single(vm.RecentRequests);
        Assert.True(row.IsStalePreSubmission);
    }

    [Fact]
    public async Task Index_Does_Not_Flag_NonPreSubmission_Status_Even_With_Past_Collection_Date()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.PendingVerification, LastWeek),
        };
        var controller = BuildController(requests, statusNamesById: new() { [1] = WithdrawalStatusNames.PendingVerification });

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        var vm = Assert.IsType<CbRequestsIndexViewModel>(view.Model);

        var row = Assert.Single(vm.RecentRequests);
        Assert.False(row.IsStalePreSubmission);
    }

    [Fact]
    public async Task History_Returns_Only_Terminal_Past_Requests()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Approved, LastWeek),   // in history
            NewRequest(2, WithdrawalStatusNames.Approved, Today),      // still active, not history
            NewRequest(3, WithdrawalStatusNames.Rejected, LastWeek),   // not terminal, not history
        };
        var statusNames = new Dictionary<int, string>
        {
            [1] = WithdrawalStatusNames.Approved,
            [2] = WithdrawalStatusNames.Approved,
            [3] = WithdrawalStatusNames.Rejected,
        };
        var controller = BuildController(requests, statusNamesById: statusNames);

        var view = Assert.IsType<ViewResult>(await controller.History(ct: CancellationToken.None));
        var vm = Assert.IsType<CbRequestHistoryViewModel>(view.Model);

        Assert.Single(vm.Rows);
    }

    [Fact]
    public async Task History_Defaults_To_The_Last_Week_When_No_Range_Given()
    {
        var requests = new List<WithdrawalRequest>
        {
            NewRequest(1, WithdrawalStatusNames.Approved, LastWeek),           // within default range
            NewRequest(2, WithdrawalStatusNames.Approved, Today.AddDays(-10)), // outside default range
        };
        var statusNames = new Dictionary<int, string>
        {
            [1] = WithdrawalStatusNames.Approved,
            [2] = WithdrawalStatusNames.Approved,
        };
        var controller = BuildController(requests, statusNamesById: statusNames);

        var view = Assert.IsType<ViewResult>(await controller.History(ct: CancellationToken.None));
        var vm = Assert.IsType<CbRequestHistoryViewModel>(view.Model);

        var row = Assert.Single(vm.Rows);
        Assert.Equal(LastWeek, row.CollectionDate);
        Assert.Equal(Today.AddDays(-7), vm.DateRange.From);
        Assert.Equal(Today, vm.DateRange.To);
    }

    [Fact]
    public async Task History_Clamps_An_Explicit_From_Date_To_One_Year_Back()
    {
        var requests = new List<WithdrawalRequest>();
        var controller = BuildController(requests);

        var view = Assert.IsType<ViewResult>(await controller.History(from: Today.AddYears(-5), ct: CancellationToken.None));
        var vm = Assert.IsType<CbRequestHistoryViewModel>(view.Model);

        Assert.Equal(Today.AddYears(-1), vm.DateRange.From);
    }
}
