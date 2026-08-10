using System.Net.Http.Headers;
using System.Net.Http.Json;
using StockService.Client.Dtos;

namespace StockService.Client
{
    /// <summary>
    /// Attaches a client-credentials bearer token to every outgoing request. The token is
    /// fetched once and cached (process-wide, thread-safe) for TokenCacheDurationMinutes so
    /// callers of IStockServiceClient never have to know auth is involved.
    /// </summary>
    internal class StockServiceAuthHandler(IHttpClientFactory _httpClientFactory, StockServiceAuthOptions _options) : DelegatingHandler
    {
        private static string? _cachedToken;
        private static DateTime _tokenExpiryUtc = DateTime.MinValue;
        private static readonly SemaphoreSlim _lock = new(1, 1);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(cancellationToken));
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiryUtc)
                return _cachedToken;

            await _lock.WaitAsync(ct);
            try
            {
                if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiryUtc)
                    return _cachedToken;

                var client = _httpClientFactory.CreateClient("StockServiceTokenClient");
                var response = await client.PostAsJsonAsync(_options.TokenEndpoint, new
                {
                    clientId = _options.ClientId,
                    clientSecret = _options.ClientSecret,
                }, ct);

                response.EnsureSuccessStatusCode();

                var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("Invalid token response from Stock API.");

                _cachedToken = token.AccessToken;
                _tokenExpiryUtc = DateTime.UtcNow.AddMinutes(_options.TokenCacheDurationMinutes);
                return _cachedToken;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
