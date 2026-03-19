using Microsoft.AspNetCore.Mvc;

namespace VMSService.Web.Controllers
{
    public class CbRequestsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
