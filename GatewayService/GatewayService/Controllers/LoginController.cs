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
                        return RedirectToAction("ResetPassword", new { username = result.Username });
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

        public IActionResult ResetPassword(string username)
        {
            var model = new ResetPasswordViewModel
            {
                UserName = username,
                DisplayName = username
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _identityService.ChangePasswordAsync(
                    model.UserName!,
                    model.CurrentPassword,
                    model.NewPassword,
                    model.ConfirmNewPassword);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
                    //TempData["Success"] = result?.Message ?? "Password changed successfully. Please login with your new password.";
                    _logger.LogInformation("Password changed successfully for user: {Username}", model.UserName);

                    TempData["Success"] = "Password reset successfully! Please login with your new password.";
                    return RedirectToAction("Index");
                }

                var errorResult = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
                string errorMessage = errorResult?.Message ?? "Failed to change password.";
                ModelState.AddModelError(string.Empty, errorMessage);
                _logger.LogError("Password change failed for user {Username}: {Error}", model.UserName, errorMessage);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogError(ex, "Error changing password for user: {Username}", model.UserName);
                return View(model);
            }
        }

        public async Task<IActionResult> Logout(string username)
        {
            var response = await _identityService.LogoutAsync(username);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LogoutResponse>();

                //Response.Cookies.Delete("AuthToken");
                //Response.Cookies.Delete("Username");
                string message = result?.Message ?? $"{username} logged out successfully";
                _logger.LogInformation(message);

                //return RedirectToAction("Index");
            }

            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("Username");

            //string errorMessage = await response.Content.ReadAsStringAsync();
            //ViewBag.Error = $"{response.ReasonPhrase} - {errorMessage}";
            //_logger.LogError($"{response.ReasonPhrase} - {errorMessage}");

            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Index");
        }
    }

    public record LoginResponse(string Token, string Username, bool MustChangePassword);
    public record LogoutResponse(string Message);
}