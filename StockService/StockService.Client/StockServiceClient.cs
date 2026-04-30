using StockService.Client.Dtos;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace StockService.Client;
public class StockServiceClient : IStockServiceClient
{
    private readonly HttpClient _http;
    public StockServiceClient(HttpClient http) => _http = http;

    public async Task<IEnumerable<DenominationDto>> GetAllDenominations()
            => await _http.GetFromJsonAsync<IEnumerable<DenominationDto>>("api/denominations/get-all")
            ?? Enumerable.Empty<DenominationDto>();
}