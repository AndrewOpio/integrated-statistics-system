using Microsoft.AspNetCore.Mvc;
using NotificationService.Client;
using NotificationService.Client.Dtos;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using WorkflowService.Client;

namespace VMSService.Web.Controllers;
public class CbRequestsController(
    IWorkflowServiceClient _workflowServiceClient,
    IWithdrawalUnitOfWork _uow, IDepositUnitOfWork _duow) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cbId = int.TryParse(User.Claims
            .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;

        var requests = await _uow.Requests.GetByCbAsync(cbId, ct);

        //var rows = requests.Select(r => new RecentRequestRowViewModel
        //{
        //    Id = r.Id,
        //    //Type = "Withdrawal",
        //    Amount = r.TotalAmount,
        //    StatusTypeName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name,
        //    CollectionDate = r.CollectionDate,
        //    TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
        //    CreationDate = r.CreatedDate,
        //    RequiresSpecialApproval = r.RequiresSpecialApproval,

        //    RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
        //});

        var rows = await Task.WhenAll(requests.Select(async r => new RecentRequestRowViewModel
        {
            Id = r.Id,
            Amount = r.TotalAmount,
            StatusTypeName = (await _workflowServiceClient.FindStatusById(r.StatusTypeId)).Name,
            CollectionDate = r.CollectionDate,
            TransactionRefNumber = r.TransactionRefNumber ?? "Not Generated",
            CreationDate = r.CreatedDate,
            RequiresSpecialApproval = r.RequiresSpecialApproval,
            RtgsCode = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
        }));

        var depositRequests = await _duow.Requests.GetByCbAsync(cbId, ct);
        var depositRows = depositRequests.Select(r => new RecentRequestRowViewModel
        {
            Id = r.Id,
            Amount = r.TotalAmount,
            StatusTypeName = "r.StatusTypeName",
            CollectionDate = r.DepositDate,
            RequiresSpecialApproval = false,

        });

        return View(new CbRequestsIndexViewModel
        {
            RecentRequests = depositRows,
        });

        //return View();
    }
}
