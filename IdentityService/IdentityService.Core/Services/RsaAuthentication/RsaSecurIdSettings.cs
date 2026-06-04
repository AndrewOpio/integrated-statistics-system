namespace IdentityService.Core.Services.RsaAuthentication
{
    public class RsaSecurIdSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ClientKey { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public int AuthnAttemptTimeout { get; set; } = 180;
        public string Language { get; set; } = "en-US";
    }
}