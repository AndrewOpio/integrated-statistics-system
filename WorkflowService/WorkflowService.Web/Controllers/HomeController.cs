using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Diagnostics;
using WorkflowService.Core.Services.Identity;
using WorkflowService.Web.Models;

namespace WorkflowService.Web.Controllers
{
    [Authorize]
    [DefaultBreadcrumb("Home")]
    public class HomeController(ILogger<HomeController> _logger, IIdentityService _identityService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var model = await _identityService.GetApplicationsAsync();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
