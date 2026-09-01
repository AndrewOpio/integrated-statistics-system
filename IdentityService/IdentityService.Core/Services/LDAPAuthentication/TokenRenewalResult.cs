namespace IdentityService.Core.Services.LDAPAuthentication
{
    /// <summary>
    /// Outcome of attempting to extend a session by re-issuing its token.
    /// </summary>
    public class TokenRenewalResult
    {
        public bool Success { get; set; }

        /// <summary>Freshly signed token. Only populated when <see cref="Success"/> is true.</summary>
        public string? Token { get; set; }

        /// <summary>Expiry of the new token, in UTC.</summary>
        public DateTime? ExpiresAtUtc { get; set; }

        /// <summary>When the original login happened, in UTC. Drives the absolute session cap.</summary>
        public DateTime? AuthenticatedAtUtc { get; set; }

        public string? Message { get; set; }

        public static TokenRenewalResult Failed(string message) =>
            new() { Success = false, Message = message };
    }
}
