using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using VMSService.Core.Services.Identity.IdentityEntities;

namespace VMSService.Core.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdentityService> _logger;
        private readonly string _baseUrl;

        public IdentityService(HttpClient httpClient, ILogger<IdentityService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["MicroServices:identityservice"];
        }

        public async Task<ICollection<NonStaffUser>> GetNonStaffUsers()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/User/non-staff");

                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<ICollection<NonStaffUser>>();
                    return users ?? new List<NonStaffUser>();
                }

                _logger.LogError("Failed to retrieve non-staff users. Status: {StatusCode}", response.StatusCode);
                return new List<NonStaffUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving non-staff users from Identity API");
                return new List<NonStaffUser>();
            }
        }
    }
}