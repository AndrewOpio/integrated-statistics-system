using IdentityService.Core.Models;
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
            List<UserViewModel> users = new List<UserViewModel>
            {
                new UserViewModel { Id = "1", UserName = "alice", Email = "alice@bou.or.ug", FirstName = "Alice", LastName = "Smith", IsActive = true },
            };

            return View(users);
        }
    }
}
