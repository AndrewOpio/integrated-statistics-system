using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationsService.Client
{
    public class ConfigurationsServiceClient : IConfigurationsServiceClient
    {
        private readonly HttpClient _http;
        public ConfigurationsServiceClient(HttpClient http) => _http = http;

        public async Task<IEnumerable<BranchDto>> GetAllBouBranchesAsync()
            => await _http.GetFromJsonAsync<IEnumerable<BranchDto>>("api/Branch")
            ?? Enumerable.Empty<BranchDto>();
        public async Task<BranchDto> GetBouBranchByIdAsync(int id)
            => await _http.GetFromJsonAsync<BranchDto>($"api/Branch/{id}")
            ?? new BranchDto();

        public async Task<IEnumerable<CommercialBankBranchDto>> GetCbBranchesByCbIdAsync(int id)
            => await _http.GetFromJsonAsync<IEnumerable<CommercialBankBranchDto>>($"api/CommercialBankBranch/cb/{id}")
            ?? Enumerable.Empty<CommercialBankBranchDto>();
        public async Task<CommercialBankBranchDto> GetCbBranchByIdAsync(int id)
            => await _http.GetFromJsonAsync<CommercialBankBranchDto>($"api/CommercialBankBranch/{id}")
            ?? new CommercialBankBranchDto();
    }
}
