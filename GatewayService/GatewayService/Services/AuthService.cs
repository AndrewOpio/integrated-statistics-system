using GatewayService.Models;
using Microsoft.AspNetCore.Identity;

namespace GatewayService.Services;
public class AuthService(ILogger<AuthService> _logger
    , UserManager<ApplicationUser> _userManager
    , IHttpContextAccessor _httpContextAccessor
    , SignInManager<ApplicationUser> _signInManager) : IAuthService
{
    public async Task SignInApplicationUser(ApplicationUser user, bool isPersistent = false)
    {
        user.IsLoggedIn = true;
        user.LastActivity = DateTime.Now;

        await _userManager.UpdateAsync(user);
        await _signInManager.SignInAsync(user, isPersistent);
    }
    public async Task SignOutApplicationUser()
    {
        await _signInManager.SignOutAsync();
        
        //Clear session data if needed
        _httpContextAccessor.HttpContext?.Session.Clear();
    }

    public async Task<(bool Success, string Status, ApplicationUser User)> ValidateApplicationUser(string username, string password)
    {
        var user = new ApplicationUser();

        //// Find user
        //var user = await _accountRepo.FindByName(username);
        //if (user == null)
        //{
        //    _logger.LogError($"Failed login attempt by {username}. User does not exist.");
        //    return (false, "Wrong username or password.", null);
        //}

        //// Validate AD credentials
        //if (!_adAuthService.ValidateCredentials(username, password))
        //{
        //    _logger.LogError($"Failed login, Active Directory authentication for username {username}.");
        //    return (false, "Failed Active Directory authentication.", null);
        //}

        //// Check if user is active
        //if (!user.IsActive)
        //{
        //    _logger.LogError($"Failed login attempt by {username}. Account inactive.");
        //    return (false, "Account inactive.", null);
        //}

        //// Check if user account is disabled
        //if (user.DisableDate < DateTime.Now)
        //{
        //    _logger.LogError($"Failed login attempt by {username}. Account disabled.");
        //    return (false, "Account disabled.", null);
        //}

        ////Check if account has expired
        //if (user.EndDate < DateTime.Now)
        //{
        //    _logger.LogError($"Failed login by {username}, account was disabled because it reached its end date.");
        //    return (false, "Account expired.", null);
        //}

        ////Check it user is already logged in
        //// if (user.IsLoggedIn == true)
        //// {
        ////     if (user.LastActivity < DateTime.UtcNow.AddMinutes(-15))
        ////     {
        ////         return (true, "success", user);
        ////     }q

        ////     _logger.LogError($"Attempted login by {username} for already logged in user.");
        ////     return (false, "You are already logged in using another browser or device.", null);
        //// }

        return (true, "success", user);

    }
}
