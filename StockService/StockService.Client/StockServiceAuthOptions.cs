namespace StockService.Client
{
    /// <summary>Client-credentials settings for authenticating against the Stock API.</summary>
    public class StockServiceAuthOptions
    {
        public string TokenEndpoint { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public int TokenCacheDurationMinutes { get; set; } = 14;
    }
}
