using Microsoft.AspNetCore.Mvc;
using NotificationService.Client;
using NotificationService.Client.Dtos;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using WorkflowService.Client;

namespace VMSService.Web.Controllers;
public class CbRequestsController(
    IWorkflowServiceClient _workflowServiceClient,
    IWithdrawalUnitOfWork _uow,
    IDepositUnitOfWork _duow,
    IWithdrawalRequestRepository _requestRepository
    ) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cbId = int.TryParse(User.Claims
            .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;

        var requests = await _uow.Requests.GetByCbAsync(cbId, ct);

        var rows = await Task.WhenAll(requests.Select(async r => new RecentRequestRowViewModel
        {
            Id = r.Id,
            //Type = "Withdrawal",
            Amount = r.TotalAmount,
            StatusTypeName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name,
            CollectionDate = r.CollectionDate,
            TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
            CreationDate = r.CreatedDate,
            RequiresSpecialApproval = r.RequiresSpecialApproval,

            RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
        }));

        var depositRequests = await _duow.Requests.GetByCbAsync(cbId, ct);

        var depositRows = await Task.WhenAll(depositRequests.Select(async r => new RecentRequestRowViewModel
        {
            Id = r.Id,
            Amount = r.TotalAmount,
            StatusTypeName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name,
            CollectionDate = r.DepositDate,
            TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
            CreationDate = r.CreatedDate,
            RequiresSpecialApproval = r.RequiresSpecialApproval,
            RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
        }));

        var allRows = rows.Concat(depositRows).ToArray();

        return View(new CbRequestsIndexViewModel
        {
            RecentRequests = [.. allRows.OrderByDescending(r => r.CreationDate)],
        });

        //return View();
    }
}
