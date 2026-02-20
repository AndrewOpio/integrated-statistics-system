using GatewayService.Models;

namespace GatewayService.Services
{
    public interface IAuthService
    {
        Task SignInApplicationUser(ApplicationUser user, bool isPersistent = false);
        Task SignOutApplicationUser();
        Task<(bool Success, string Status, ApplicationUser User)> ValidateApplicationUser(string username, string password);
    }
}
