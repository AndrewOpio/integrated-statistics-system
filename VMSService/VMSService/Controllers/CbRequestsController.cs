using Microsoft.AspNetCore.Mvc;
using NotificationService.Client;
using NotificationService.Client.Dtos;

namespace VMSService.Web.Controllers
{
    public class CbRequestsController(INotificationServiceClient _notificationClient
        , INotificationEventPublisher publisher) : Controller
    {
        public async Task<IActionResult> Index()
        {

            return View();
        }
    }
}
