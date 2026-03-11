using System.Diagnostics;
using GatewayService.Models;
using GatewayService.Services.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers
{
    public class HomeController(ILogger<HomeController> _logger, IIdentityService _identityService) : Controller
    {
        public async Task<IActionResult> Index([FromServices] IHttpClientFactory clientFactory)
        {
            List<Application> applications = new();
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = Request.Cookies["Username"];

            try
            {
                var response = await _identityService.GetApplicationsAsync();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<Application>>();

                    return View(result);
                }

                string errorMessage = await response.Content.ReadAsStringAsync();
                TempData["Error"] = errorMessage;
                _logger.LogError(errorMessage);
                return View(applications);
            } catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogError(ex, "Error getting applications");
            }

            return View(applications);
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
