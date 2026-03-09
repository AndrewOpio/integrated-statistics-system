
namespace GatewayService.Services.Identity
{
    public class IdentityService(IHttpClientFactory _clientFactory, IConfiguration _configuration) : IIdentityService
    {
        public async Task<HttpResponseMessage> LoginAsync(string username, string password)
        {
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();
            var loginData = new { username, password };

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/login", loginData);
        }

        public async Task<HttpResponseMessage> GetApplicationsAsync()
        {
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();

            return await client.GetAsync($"{baseUrl}/api/app/applications");
        }
    }
}
