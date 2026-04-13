using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Static;
using WorkflowService.Client;

namespace VMSService.Web.Controllers
{
    public class WithdrawalRequestsController(
    IWorkflowServiceClient _workflowServiceClient,
    IWithdrawalUnitOfWork _uow) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var all = (await _uow.Requests.FindAll()
                ).Where(x => !x.IsDeleted);

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // ── Counts ───────────────────────────────────────────────────────────────

            // Pending CB approval (Draft or ReturnedForRevision — still with the CB)
            var pendingCb = all.Count(r =>
                !r.IsDeleted &&
                (r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingInternalApproval));

            // Special approval pending
            var specialApproval = all.Count(r =>
                !r.IsDeleted &&
                r.RequiresSpecialApproval &&
                r.StatusTypeName == StatusConstants.WithdrawalRequest.SpecialApproval);

            // Under bank processing (submitted to bank, recommended — not yet approved)
            var underBankProcessing = all.Count(r =>
                !r.IsDeleted &&
                (r.StatusTypeName == StatusConstants.WithdrawalRequest.PendingDisbursement
                 || r.StatusTypeName == StatusConstants.WithdrawalRequest.Recommended));

            // Processed by bank this month (approved this month)
            //var processedThisMonth = all.Count(r =>
            //    !r.IsDeleted &&
            //    r.StatusTypeId == WithdrawalStatusIds.Approved &&
            //    r.ModifiedDate >= startOfMonth);

            // All ever processed by bank (all approved, all time)
            var totalProcessed = all
                .Where(r => !r.IsDeleted && r.StatusTypeName == StatusConstants.WithdrawalRequest.Authorized)
                .Sum(r => r.TotalAmount);

            // ── Requests table ───────────────────────────────────────────────────────
            var rows = await Task.WhenAll(all
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.CreatedDate)
                .Select(async r =>
                {
                    var status = await _workflowServiceClient.FindStatusById(r.StatusTypeId);
                    return new RecentRequestRowViewModel
                    {
                        Id = r.Id,
                        TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
                        Amount = r.TotalAmount,
                        StatusTypeName = status?.Name ?? r.StatusTypeName,
                        //StatusId = r.StatusTypeId,
                        CollectionDate = r.CollectionDate,
                        CreationDate = r.CreatedDate,
                        RequiresSpecialApproval = r.RequiresSpecialApproval,
                        RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
                        BouBranchCode = r.BouBranchCode
                    };
                }));

            var vm = new BouWithdrawalRequestViewModel
            {
                PendingCbCount = pendingCb,
                SpecialApprovalCount = specialApproval,
                UnderBouProcessing = underBankProcessing,
                TotalProcessed = totalProcessed,
                Requests = rows,
                
            };

            return View(vm); 
        }
    }
}
