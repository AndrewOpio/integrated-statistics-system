namespace GatewayService.Services.Identity
{
    public interface IIdentityService
    {
        Task<HttpResponseMessage> LoginAsync(string username, string password);
        Task<HttpResponseMessage> GetApplicationsAsync();
        Task<HttpResponseMessage> LogoutAsync(string username);
        Task<HttpResponseMessage> ChangePasswordAsync(string username, string currentPassword, string newPassword, string confirmNewPassword);
    }
}
