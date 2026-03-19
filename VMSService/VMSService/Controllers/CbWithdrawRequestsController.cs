using Microsoft.AspNetCore.Mvc;

namespace VMSService.Web.Controllers
{
    public class CbWithdrawRequestsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Approve()
        {
            return View();
        }
    }
}
