using GatewayService.Models;
using GatewayService.Services;
using GatewayService.Services.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GatewayService.Controllers
{
    public class LoginController(ILogger<LoginController> _logger, IIdentityService _identityService) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, [FromServices] IHttpClientFactory clientFactory)
        {
            try
            {
                var response = await _identityService.LoginAsync(username, password);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    if(result.MustChangePassword)
                    {
                        
                    }

                    // Set authentication cookie (PROD)
                    //Response.Cookies.Append("AuthToken", result.Token, new CookieOptions
                    //{
                    //    Secure = true, // Set to true in production with HTTPS
                    //    SameSite = SameSiteMode.None,
                    //    Expires = DateTimeOffset.UtcNow.AddHours(2),
                    //    Domain = ".bcnet.bou.or.ug"
                    //});

                    //Response.Cookies.Append("Username", result.Username, new CookieOptions
                    //{
                    //    Secure = true,
                    //    SameSite = SameSiteMode.None,
                    //    Expires = DateTimeOffset.UtcNow.AddHours(2),
                    //    Domain = ".bcnet.bou.or.ug"
                    //});

                    // Set authentication cookie (TEST)
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

                var errorResult = await response.Content.ReadFromJsonAsync<LogoutResponse>();
                string errorMessage = errorResult.Message;
                ViewBag.Error = $"{response.ReasonPhrase} - {errorMessage}";
                _logger.LogError($"{response.ReasonPhrase} - {errorMessage}");
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

        public async Task<IActionResult> Logout(string username)
        {
            var response = await _identityService.LogoutAsync(username);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LogoutResponse>();

                Response.Cookies.Delete("AuthToken");
                Response.Cookies.Delete("Username");
                string message = result?.Message ?? $"{username} logged out successfully";
                _logger.LogInformation(message);

                return RedirectToAction("Index");
            }

            string errorMessage = await response.Content.ReadAsStringAsync();
            ViewBag.Error = $"{response.ReasonPhrase} - {errorMessage}";
            _logger.LogError($"{response.ReasonPhrase} - {errorMessage}");

            return RedirectToAction("Index", "Home");
        }
    }

    public record LoginResponse(string Token, string Username, bool MustChangePassword);
    public record LogoutResponse(string Message);
}