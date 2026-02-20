using GatewayService.Models;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GatewayService.Controllers
{
    public class LoginController(ILogger<LoginController> _logger) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, [FromServices] IHttpClientFactory clientFactory)
        {
            var client = clientFactory.CreateClient();
            var loginData = new { username, password };

            try
            {
                //var response = await client.PostAsJsonAsync("http://identityservice/api/auth/login", loginData);
                var response = await client.PostAsJsonAsync("https://localhost:7026/api/auth/login", loginData);
                //var response = await client.PostAsJsonAsync("http://localhost:5000/api/auth/login", loginData);


                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    // Set authentication cookie
                    Response.Cookies.Append("AuthToken", result.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Set to true in production with HTTPS
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(2)
                    });

                    Response.Cookies.Append("Username", result.Username, new CookieOptions
                    {
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(2)
                    });

                    //ApplicationUser user = new ApplicationUser
                    //{
                    //    FirstName = "result.Username",
                    //    LastName = "result.Token",
                    //    UserName = username,
                    //    Title = "Admin",
                    //    IsActive = true,
                    //    LastActivity = DateTime.Now
                    //};
                    //await _authService.SignInApplicationUser(user);


                    return RedirectToAction("Index", "Home");
                }

                string errorMessage = await response.Content.ReadAsStringAsync();
                ViewBag.Error = errorMessage;
                _logger.LogError(errorMessage);
                return View();
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                ViewBag.Error = errorMessage;
                _logger.LogError(errorMessage);
                return View();
            }
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("Username");
            return RedirectToAction("Index");
        }
    }

    public record LoginResponse(string Token, string Username);
}
