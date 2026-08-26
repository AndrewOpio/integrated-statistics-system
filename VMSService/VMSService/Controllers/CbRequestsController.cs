using Microsoft.AspNetCore.Mvc;
using NotificationService.Client;
using NotificationService.Client.Dtos;
using VMSService.Core.Extensions;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Web.Controllers;

public class CbRequestsController(
    IWorkflowServiceClient _workflowServiceClient,
    IWithdrawalUnitOfWork _uow,
    IDepositUnitOfWork _duow,
    IWithdrawalRequestRepository _requestRepository
    ) : Controller
{
    // Statuses where the next actor sits at the CB (Maker or Approver) — includes Pending
    // Internal Approval, since that's waiting on the CB Approver, not BOU.
    private static readonly HashSet<string> AwaitingActionStatuses = new()
    {
        WithdrawalStatusNames.Draft,
        WithdrawalStatusNames.ReturnedForRevision,
        WithdrawalStatusNames.PendingInternalApproval,
        WithdrawalStatusNames.PendingRTGSVerification,
        DepositStatuses.DepositStatusNames.Draft,
    };

    // Non-terminal statuses sitting with BOU, excluding Pending Exceptional Approval
    // (called out as its own bucket given the same-day urgency it represents).
    private static readonly HashSet<string> WithBouStatuses = new()
    {
        WithdrawalStatusNames.PendingWithdrawalConfirmation,
        WithdrawalStatusNames.PendingExceptionalVerification,
        WithdrawalStatusNames.PendingVerification,
        WithdrawalStatusNames.PendingApproval,
        WithdrawalStatusNames.RecallRequested,
        WithdrawalStatusNames.RecallVerified,
        WithdrawalStatusNames.RecallApproved,
        DepositStatuses.DepositStatusNames.SubmittedToBank,
        DepositStatuses.DepositStatusNames.Recommended,
    };

    // Ended without success — recalls are deliberately excluded, since a recall is CB pulling
    // the request back, not a failure outcome. Cancelled/Cancel Complete removed from this set:
    // there is no Cancel in this domain, Recall is the CB's only withdrawal mechanism (2026-08
    // transitions rebuild) — these statuses can no longer occur for a withdrawal request.
    private static readonly HashSet<string> UnsuccessfulStatuses = new()
    {
        WithdrawalStatusNames.Rejected,
        WithdrawalStatusNames.RejectComplete,
        WithdrawalStatusNames.RejectExceptionalApproval,
        WithdrawalStatusNames.RejectExceptionalApprovalComplete,
        WithdrawalStatusNames.Expired,
        DepositStatuses.DepositStatusNames.Rejected,
        DepositStatuses.DepositStatusNames.Cancelled,
    };

    // "Pending Exceptional Approval" is the same literal string for both withdrawal and
    // deposit statuses, so a single check covers both request types.
    private static string ClassifyBucket(string statusTypeName) => statusTypeName switch
    {
        _ when AwaitingActionStatuses.Contains(statusTypeName) => "awaiting-action",
        WithdrawalStatusNames.PendingExceptionalApproval => "pending-exceptional",
        _ when WithBouStatuses.Contains(statusTypeName) => "with-bou",
        _ when UnsuccessfulStatuses.Contains(statusTypeName) => "unsuccessful",
        _ => "",
    };

    private async Task<RecentRequestRowViewModel> MapWithdrawalRow(WithdrawalRequest r, DateOnly today)
    {
        var statusName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name;
        return new RecentRequestRowViewModel
        {
            Id = r.Id,
            Amount = r.TotalAmount,
            StatusTypeName = statusName,
            StatusBucket = ClassifyBucket(statusName),
            CollectionDate = r.CollectionDate,
            TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
            CreationDate = r.CreatedDate,
            RequiresSpecialApproval = r.RequiresSpecialApproval,
            // Always false for terminal (History) rows — Section 10's pre-submission set never
            // overlaps with TerminalStatuses — so one shared mapping covers both Index and History.
            IsStalePreSubmission = WithdrawalRequestRepository.IsStalePreSubmission(statusName, r.CollectionDate, today),
            RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
        };
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!User.IsCbUser()) return Forbid();

        var cbId = int.TryParse(User.Claims
            .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Main list: excludes requests that are both terminal and past their collection date —
        // those live in History instead (ticket 19). Deposits are untouched — out of scope here.
        var requests = await _uow.Requests.GetActiveByCbAsync(cbId, today, ct);

        var rows = await Task.WhenAll(requests.Select(r => MapWithdrawalRow(r, today)));

        // Legacy deposits are back-filled records, not requests the bank raised — they don't belong
        // in Recent Requests. GetByCbAsync doesn't filter them, unlike the other deposit queries.
        var depositRequests = (await _duow.Requests.GetByCbAsync(cbId, ct))
            .Where(r => !r.IsLegacyDeposit);

        var depositRows = await Task.WhenAll(depositRequests.Select(async r =>
        {
            var statusName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name;
            return new RecentRequestRowViewModel
            {
                Id = r.Id,
                Amount = r.TotalAmount,
                StatusTypeName = statusName,
                StatusBucket = ClassifyBucket(statusName),
                CollectionDate = r.DepositDate,
                TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                CreationDate = r.CreatedDate,
                RequiresSpecialApproval = r.RequiresSpecialApproval,
                RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
            };
        }));

        var allRows = rows.Concat(depositRows).ToArray();

        return View(new CbRequestsIndexViewModel
        {
            RecentRequests = [.. allRows.OrderByDescending(r => r.CreationDate)],
            AwaitingActionCount = allRows.Count(r => r.StatusBucket == "awaiting-action"),
            PendingExceptionalApprovalCount = allRows.Count(r => r.StatusBucket == "pending-exceptional"),
            WithBouCount = allRows.Count(r => r.StatusBucket == "with-bou"),
            UnsuccessfulCount = allRows.Count(r => r.StatusBucket == "unsuccessful"),
        });

        //return View();
    }

    /// <summary>Terminal, past-collection-date withdrawal requests excluded from the main list —
    /// bounded to the last 12 months back from today; older than that, the CB asks BOU directly.</summary>
    public async Task<IActionResult> History(CancellationToken ct)
    {
        if (!User.IsCbUser()) return Forbid();

        var cbId = int.TryParse(User.Claims
            .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var since = today.AddMonths(-12);

        var requests = await _uow.Requests.GetHistoryByCbAsync(cbId, today, since, ct);

        var rows = await Task.WhenAll(requests.Select(r => MapWithdrawalRow(r, today)));

        return View(rows.OrderByDescending(r => r.CollectionDate).ToList());
    }
}
