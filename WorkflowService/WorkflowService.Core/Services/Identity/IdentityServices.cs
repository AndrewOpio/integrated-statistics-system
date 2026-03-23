using IdentityService.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace WorkflowService.Core.Services.Identity
{
    public class IdentityServices : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdentityServices> _logger;
        private readonly string _baseUrl;

        public IdentityServices(HttpClient httpClient, ILogger<IdentityServices> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["MicroServices:identityservice"];
        }

        public async Task<List<ApplicationDto>> GetApplicationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/App/applications");

                if (response.IsSuccessStatusCode)
                {
                    var applications = await response.Content.ReadFromJsonAsync<List<ApplicationDto>>();
                    return applications ?? new List<ApplicationDto>();
                }

                _logger.LogError("Failed to retrieve applications. Status: {StatusCode}", response.StatusCode);
                return new List<ApplicationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications from Identity API");
                return new List<ApplicationDto>();
            }
        }

        public async Task<ApplicationDto> GetApplicationByIdAsync(int applicationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/App/get-application?applicationId={applicationId}");

                if (response.IsSuccessStatusCode)
                {
                    var application = await response.Content.ReadFromJsonAsync<ApplicationDto>();
                    return application ?? new ApplicationDto();
                }

                _logger.LogError("Failed to retrieve applications. Status: {StatusCode}", response.StatusCode);
                return new ApplicationDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications from Identity API");
                return new ApplicationDto();
            }
        }

        public async Task<List<ApplicationRoleDto>> GetRolesForApplicationAsync(int applicationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/AppRole/app-roles?applicationId={applicationId}");

                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<ApplicationRoleDto>>();
                    return users ?? new List<ApplicationRoleDto>();
                }

                _logger.LogError("Failed to retrieve application roles. Status: {StatusCode}", response.StatusCode);
                return new List<ApplicationRoleDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application roles from Identity API");
                return new List<ApplicationRoleDto>();
            }
        }

        public async Task<ApplicationRoleDto> GetRoleByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/AppRole/app-role?id={id}");

                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<ApplicationRoleDto>();
                    return users ?? new ApplicationRoleDto();
                }

                _logger.LogError("Failed to retrieve application role. Status: {StatusCode}", response.StatusCode);
                return new ApplicationRoleDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application role from Identity API");
                return new ApplicationRoleDto();
            }
        }
    }
}