using IdentityService.Core.Services.LDAPAuthentication;
using IdentityService.Static;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace IdentityService.Controllers
{
    /// <summary>
    /// Backs the session-expiry prompt in the layout. Same contract as the SessionController
    /// in the other Aswam services, except this one is the token authority: it renews through
    /// <see cref="IJwtTokenService"/> directly rather than calling itself over HTTP.
    /// </summary>
    public class SessionController(
        IJwtTokenService _jwtService,
        IConfiguration _configuration,
        ILogger<SessionController> _logger) : Controller
    {
        /// <summary>
        /// How long the session has left, read straight from the cookie's token.
        /// No network call, so the countdown is cheap to poll.
        /// </summary>
        [HttpGet]
        public IActionResult Status()
        {
            var expiresAt = ReadExpiry(Request.Cookies[AuthCookie.TokenCookieName]);

            if (expiresAt is null)
            {
                return Json(new { authenticated = false, secondsRemaining = 0 });
            }

            return Json(new
            {
                authenticated = true,
                expiresAtUtc = expiresAt.Value,
                secondsRemaining = SecondsUntil(expiresAt.Value)
            });
        }

        /// <summary>
        /// Swaps the current token for one with a later expiry. Called both silently
        /// (user is active) and from the "Stay signed in" button (user was idle).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Extend()
        {
            var token = Request.Cookies[AuthCookie.TokenCookieName];

            if (string.IsNullOrWhiteSpace(token))
            {
                return Json(new { success = false, message = "Not signed in." });
            }

            try
            {
                var result = _jwtService.RenewToken(token);

                if (!result.Success || result.Token is null)
                {
                    // Session cap reached, or the token was already dead. Either way the
                    // user has to sign in again; let the client run its timeout path.
                    return Json(new
                    {
                        success = false,
                        message = result.Message ?? "Your session could not be extended."
                    });
                }

                var options = AuthCookie.Build(_configuration, httpOnly: true);
                options.Expires = result.ExpiresAtUtc;
                Response.Cookies.Append(AuthCookie.TokenCookieName, result.Token, options);

                // Keep the companion cookie in step so it never outlives the token.
                var username = Request.Cookies[AuthCookie.UsernameCookieName];
                if (!string.IsNullOrEmpty(username))
                {
                    var usernameOptions = AuthCookie.Build(_configuration, httpOnly: false);
                    usernameOptions.Expires = result.ExpiresAtUtc;
                    Response.Cookies.Append(AuthCookie.UsernameCookieName, username, usernameOptions);
                }

                return Json(new
                {
                    success = true,
                    expiresAtUtc = result.ExpiresAtUtc,
                    secondsRemaining = SecondsUntil(result.ExpiresAtUtc ?? DateTime.UtcNow)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending session");
                return Json(new { success = false, message = "Your session could not be extended." });
            }
        }

        /// <summary>
        /// Ends the session and sends the user to the login page. Used when the countdown
        /// runs out or the user declines to stay signed in. The cookie is HttpOnly, so the
        /// browser cannot clear it itself - it has to come back through here.
        /// </summary>
        [HttpGet]
        public IActionResult End(bool timedOut = false)
        {
            Response.Cookies.Delete(AuthCookie.TokenCookieName, AuthCookie.Build(_configuration, httpOnly: true));
            Response.Cookies.Delete(AuthCookie.UsernameCookieName, AuthCookie.Build(_configuration, httpOnly: false));

            var loginPath = _configuration["Authentication:LoginPath"] ?? "/Login";

            if (timedOut)
            {
                var separator = loginPath.Contains('?') ? "&" : "?";
                loginPath += $"{separator}errorMessage={Uri.EscapeDataString("Your session expired due to inactivity. Please sign in again.")}";
            }

            return Redirect(loginPath);
        }

        private static DateTime? ReadExpiry(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                // Read-only: the JwtBearer middleware is what actually validates this token.
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.ValidTo == DateTime.MinValue ? null : jwt.ValidTo;
            }
            catch
            {
                return null;
            }
        }

        private static int SecondsUntil(DateTime expiresAtUtc)
        {
            var seconds = (expiresAtUtc - DateTime.UtcNow).TotalSeconds;
            return seconds <= 0 ? 0 : (int)seconds;
        }
    }
}
