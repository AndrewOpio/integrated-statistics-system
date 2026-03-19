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

        public async Task<HttpResponseMessage> LogoutAsync(string username)
        {
            var baseUrl = _configuration["MicroServices:identityservice"];
            var client = _clientFactory.CreateClient();
            var logoutData = new { username };

            return await client.PostAsJsonAsync($"{baseUrl}/api/auth/logout", logoutData);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(string username, string currentPassword, string newPassword, string confirmNewPassword)
        {
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
    }
}
