using IdentityService.Core.Static;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [Authorize(Policy = SystemPolicy.Roles.IsAdmin)]
        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
