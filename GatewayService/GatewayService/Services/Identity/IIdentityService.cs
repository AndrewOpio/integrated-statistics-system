namespace GatewayService.Services.Identity
{
    public interface IIdentityService
    {
        Task<HttpResponseMessage> LoginAsync(string username, string password);
        Task<HttpResponseMessage> GetApplicationsAsync();
    }
}
