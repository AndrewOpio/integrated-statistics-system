using ConfigurationsService.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using VMSService.Core.Services.Configurations;

namespace VMSService.Core.Services.Identity
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdentityService> _logger;
        private readonly string _baseUrl;

        public ConfigurationService(HttpClient httpClient, ILogger<IdentityService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["MicroServices:configurationsservice"];
        }

        #region Branch Methods

        public async Task<ICollection<BranchDto>> GetAllBranches()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/branch");

                if (response.IsSuccessStatusCode)
                {
                    var branches = await response.Content.ReadFromJsonAsync<ICollection<BranchDto>>();
                    return branches ?? new List<BranchDto>();
                }

                _logger.LogError("Failed to retrieve branches. Status: {StatusCode}", response.StatusCode);
                return new List<BranchDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches from API");
                return new List<BranchDto>();
            }
        }

        public async Task<BranchDto> GetBranchById(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/branch/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<BranchDto>();
                }

                _logger.LogWarning("Branch with id {BranchId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch {BranchId} from API", id);
                return null;
            }
        }

        #endregion

        #region CommercialBank Methods

        public async Task<ICollection<CommercialBankDto>> GetAllCommercialBanks()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/commercialbank");

                if (response.IsSuccessStatusCode)
                {
                    var commercialBanks = await response.Content.ReadFromJsonAsync<ICollection<CommercialBankDto>>();
                    return commercialBanks ?? new List<CommercialBankDto>();
                }

                _logger.LogError("Failed to retrieve commercial banks. Status: {StatusCode}", response.StatusCode);
                return new List<CommercialBankDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial banks from API");
                return new List<CommercialBankDto>();
            }
        }

        public async Task<CommercialBankDto> GetCommercialBankById(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/commercialbank/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CommercialBankDto>();
                }

                _logger.LogWarning("Commercial bank with id {CommercialBankId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank {CommercialBankId} from API", id);
                return null;
            }
        }

        //public async Task<CommercialBankBranchDto> GetCbBranchesByCommercialBankId(int id)
        //{
        //    try
        //    {
        //        var response = await _httpClient.GetAsync($"{_baseUrl}/api/commercialbankbranch/{id}");

        //        if (response.IsSuccessStatusCode)
        //        {
        //            return await response.Content.ReadFromJsonAsync<CommercialBankBranchDto>();
        //        }

        //        _logger.LogWarning("Commercial bank branch with id Commercial Bank Id: {CommercialBankId} not found", id);
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving commercial bank branch with Commercial Bank Id:{CommercialBankId} from API", id);
        //        return null;
        //    }
        //}




        #endregion

        #region CommercialBankBranch Methods

        public async Task<ICollection<CommercialBankBranchDto>> GetAllCommercialBankBranches()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/commercialbankbranch");

                if (response.IsSuccessStatusCode)
                {
                    var commercialBankBranches = await response.Content.ReadFromJsonAsync<ICollection<CommercialBankBranchDto>>();
                    return commercialBankBranches ?? new List<CommercialBankBranchDto>();
                }

                _logger.LogError("Failed to retrieve commercial bank branches. Status: {StatusCode}", response.StatusCode);
                return new List<CommercialBankBranchDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branches from API");
                return new List<CommercialBankBranchDto>();
            }
        }

        public async Task<CommercialBankBranchDto> GetCommercialBankBranchById(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/commercialbankbranch/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CommercialBankBranchDto>();
                }

                _logger.LogWarning("Commercial bank branch with id {CommercialBankBranchId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branch {CommercialBankBranchId} from API", id);
                return null;
            }
        }

        public async Task<ICollection<CommercialBankBranchDto>> GetCommercialBankBranchesByBankId(int commercialBankId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/commercialbankbranch/bybank/{commercialBankId}");

                if (response.IsSuccessStatusCode)
                {
                    var commercialBankBranches = await response.Content.ReadFromJsonAsync<ICollection<CommercialBankBranchDto>>();
                    return commercialBankBranches ?? new List<CommercialBankBranchDto>();
                }

                _logger.LogError("Failed to retrieve commercial bank branches. Status: {StatusCode}", response.StatusCode);
                return new List<CommercialBankBranchDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branches from API");
                return new List<CommercialBankBranchDto>();
            }
        }

        #endregion
    }
}