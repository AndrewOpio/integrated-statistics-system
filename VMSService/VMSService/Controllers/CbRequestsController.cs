using Microsoft.AspNetCore.Mvc;
using NotificationService.Client;
using NotificationService.Client.Dtos;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;

namespace VMSService.Web.Controllers;
public class CbRequestsController(IWithdrawalUnitOfWork _uow) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cbId = int.TryParse(User.Claims
            .FirstOrDefault(c => c.Type == "cbId")?.Value, out var id) ? id : 0;

        var requests = await _uow.Requests.GetByCbAsync(cbId, ct);

        var rows = requests.Select(r => new RecentRequestRowViewModel
        {
            Id = r.Id,
            Type = "Withdrawal",
            Amount = r.TotalAmount,
            Status = r.StatusTypeName,
            StatusName = r.StatusTypeName,
            Date = r.CollectionDate,
            Purpose = string.IsNullOrEmpty(r.RtgsCode) ? "—" : r.RtgsCode,
        });

        return View(new CbRequestsIndexViewModel
        {
            RecentRequests = rows,
        });

        //return View();
    }
}
