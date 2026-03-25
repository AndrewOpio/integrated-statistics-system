using Microsoft.AspNetCore.Mvc;

namespace VMSService.Web.Controllers
{
    public class WithdrawalAuthorizationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
