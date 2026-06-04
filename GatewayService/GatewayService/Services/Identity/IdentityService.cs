using GatewayService.Controllers;
using static GatewayService.Models.LoginModel;

namespace GatewayService.Services.Identity
{
    public class IdentityService(IHttpClientFactory _clientFactory, IConfiguration _configuration) : IIdentityService
    {
        //private readonly IHttpClientFactory _clientFactory;
        //private readonly string _baseUrl;
        //private const string ClientName = "IdentityService";

        //public IdentityService(IHttpClientFactory clientFactory, IConfiguration configuration)
        //{
        //    _clientFactory = clientFactory;
        //    _baseUrl = configuration["MicroServices:identityservice"]
        //        ?? throw new InvalidOperationException("IdentityService base URL is not configured");
        //}

        public async Task<HttpResponseMessage> LoginAsync(string username, string password)
        {
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();

            var loginData = new { username, password };

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/login", loginData);
        }

        public async Task<HttpResponseMessage> GetApplicationsAsync()
        {
            //var client = _clientFactory.CreateClient(ClientName);
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();

            return await client.GetAsync($"{baseUrl}/api/app/applications");
        }

        public async Task<HttpResponseMessage> LogoutAsync(string username)
        {
            //var client = _clientFactory.CreateClient(ClientName);
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();
            var logoutData = new { username };

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/logout", logoutData);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(string username, string currentPassword, string newPassword, string confirmNewPassword)
        {
            //var client = _clientFactory.CreateClient(ClientName);
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();
            var changePasswordData = new
            {
                username,
                currentPassword,
                newPassword,
                confirmNewPassword
            };

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/change-password", changePasswordData);
        }

        public async Task<HttpResponseMessage> LoginStep1Async(FirstFactorRequest loginData)
        {
            //var client = _clientFactory.CreateClient(ClientName);
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/login/step1", loginData);
        }

        public async Task<HttpResponseMessage> LoginStep2Async(SecondFactorRequest loginData)
        {
            //var client = _clientFactory.CreateClient(ClientName);
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/login/step2", loginData);
        }

        public async Task<HttpResponseMessage> CancelRsaAuthenticationAsync(RsaCancelRequest request)
        {
            //var client = _clientFactory.CreateClient(ClientName);
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/rsa/cancel", request);
        }
    }
}
