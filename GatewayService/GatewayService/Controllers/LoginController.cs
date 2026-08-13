using GatewayService.Models;
using GatewayService.Services;
using GatewayService.Services.Identity;
using GatewayService.Static;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static GatewayService.Models.LoginModel;

namespace GatewayService.Controllers
{
    public class LoginController(ILogger<LoginController> _logger, IIdentityService _identityService, IConfiguration _configuration) : Controller
    {
        private CookieOptions BuildAuthCookieOptions(bool httpOnly) =>
            AuthCookie.Build(_configuration, httpOnly);

        public IActionResult Index(string? errorMessage)
        {
            @ViewBag.Error = errorMessage;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            try
            {
                var firstFactorRequest = new FirstFactorRequest
                {
                    Username = username,
                    Password = password
                };

                var response = await _identityService.LoginStep1Async(firstFactorRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FirstFactorResponse>();

                    if (result.MustChangePassword)
                    {
                        return RedirectToAction("ResetPassword", new { username = result.Username });
                    }

                    if (result.Success)
                    {
                        return RedirectToAction("Login2FA", result);
                    }
                }

                var errorResult = await response.Content.ReadFromJsonAsync<FirstFactorResponse>();
                string errorMessage = errorResult?.Message ?? "Login failed";
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

        public IActionResult Login2FA(FirstFactorResponse firstFactorResponse)
        {
            if (firstFactorResponse == null || !firstFactorResponse.RequiresSecondFactor)
            {
                return RedirectToAction("Index");
            }

            return View(firstFactorResponse);
        }

        [HttpPost]
        [ActionName("Login2FA")]
        public async Task<IActionResult> Login2FAPost(string username, string rsaPasscode, string authnAttemptId, string? inResponseTo)
        {
            try
            {
                var secondFactorRequest = new SecondFactorRequest
                {
                    Username = username,
                    RsaPasscode = rsaPasscode,
                    AuthnAttemptId = authnAttemptId,
                    InResponseTo = inResponseTo
                };

                var response = await _identityService.LoginStep2Async(secondFactorRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    // Keep the cookie lifetime aligned with the JWT lifetime. A cookie that
                    // outlives its token just means the browser keeps replaying a token
                    // every service is bound to reject.
                    var expiresAt = DateTimeOffset.UtcNow.AddMinutes(
                        _configuration.GetValue<int?>("JwtSettings:ExpirationMinutes") ?? 15);

                    var authCookieOptions = BuildAuthCookieOptions(httpOnly: true);
                    authCookieOptions.Expires = expiresAt;
                    Response.Cookies.Append("AuthToken", result.Token, authCookieOptions);

                    // Read by client-side script, so not HttpOnly.
                    var usernameCookieOptions = BuildAuthCookieOptions(httpOnly: false);
                    usernameCookieOptions.Expires = expiresAt;
                    Response.Cookies.Append("Username", result.Username, usernameCookieOptions);

                    return RedirectToAction("Index", "Home");

                    //if (result.RequiresRetry)
                    //{
                    //    var firstFactorResponse1 = new FirstFactorResponse
                    //    {
                    //        Success = false,
                    //        Username = username,
                    //        RequiresSecondFactor = true,
                    //        RsaAuthnAttemptId = authnAttemptId,
                    //        RsaMessageId = result.RsaMessageId,
                    //        RsaPromptText = result.RsaPromptText,
                    //        Message = result.Message
                    //    };

                    //    ViewBag.Error = result.Message;
                    //    return View("Login2FA", firstFactorResponse1);
                    //}
                } 
                //else if ()
                //{

                //}

                var errorResult = await response.Content.ReadFromJsonAsync<SecondFactorResponse>();
                string errorMessage = errorResult?.Message ?? "2FA verification failed";
                ViewBag.Error = $"{response.ReasonPhrase} - {errorMessage}";
                _logger.LogError($"{response.ReasonPhrase} - {errorMessage}");

                //return RedirectToAction("Index");
                if (errorResult.RequiresRetry)
                {
                    var firstFactorResponse = new FirstFactorResponse
                    {
                        Success = false,
                        Username = username,
                        RequiresSecondFactor = true,
                        RsaAuthnAttemptId = authnAttemptId,
                        RsaMessageId = inResponseTo,
                        RsaPromptText = errorResult?.RsaPromptText,
                        Message = errorMessage
                    };

                    return View("Login2FA", firstFactorResponse);
                } else
                {
                    return RedirectToAction("Index", new { errorMessage });
                }                
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                ViewBag.Error = errorMessage;
                _logger.LogError(ex, "Error during 2FA verification");

                var firstFactorResponse = new FirstFactorResponse
                {
                    Success = false,
                    Username = username,
                    RequiresSecondFactor = true,
                    RsaAuthnAttemptId = authnAttemptId,
                    RsaMessageId = inResponseTo,
                    Message = errorMessage
                };

                return View("Login2FA", firstFactorResponse);
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

            // Delete with the same domain/path/attributes used to issue them.
            Response.Cookies.Delete("AuthToken", BuildAuthCookieOptions(httpOnly: true));
            Response.Cookies.Delete("Username", BuildAuthCookieOptions(httpOnly: false));

            //string errorMessage = await response.Content.ReadAsStringAsync();
            //ViewBag.Error = $"{response.ReasonPhrase} - {errorMessage}";
            //_logger.LogError($"{response.ReasonPhrase} - {errorMessage}");

            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Index");
        }
    }
}