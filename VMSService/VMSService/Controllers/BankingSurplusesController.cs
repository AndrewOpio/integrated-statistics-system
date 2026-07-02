using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Static;
using VMSService.Web.Authorization;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class BankingSurplusesController : Controller
    {
        [RequirePermission(SystemPermissions.BankingSurpluses.View)]
        [Breadcrumb("Banking Surpluses", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public IActionResult Index()
        {
            return View();
        }
    }
}
