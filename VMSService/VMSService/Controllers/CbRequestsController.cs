using Microsoft.AspNetCore.Mvc;
using NotificationService.Client;
using NotificationService.Client.Dtos;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
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
    // the request back, not a failure outcome.
    private static readonly HashSet<string> UnsuccessfulStatuses = new()
    {
        WithdrawalStatusNames.Rejected,
        WithdrawalStatusNames.RejectComplete,
        WithdrawalStatusNames.RejectExceptionalApproval,
        WithdrawalStatusNames.RejectExceptionalApprovalComplete,
        WithdrawalStatusNames.Cancelled,
        WithdrawalStatusNames.CancelComplete,
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

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cbId = int.TryParse(User.Claims
            .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;

        var requests = await _uow.Requests.GetByCbAsync(cbId, ct);

        var rows = await Task.WhenAll(requests.Select(async r =>
        {
            var statusName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name;
            return new RecentRequestRowViewModel
            {
                Id = r.Id,
                //Type = "Withdrawal",
                Amount = r.TotalAmount,
                StatusTypeName = statusName,
                StatusBucket = ClassifyBucket(statusName),
                CollectionDate = r.CollectionDate,
                TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                CreationDate = r.CreatedDate,
                RequiresSpecialApproval = r.RequiresSpecialApproval,

                RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
            };
        }));

        var depositRequests = await _duow.Requests.GetByCbAsync(cbId, ct);

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
}
