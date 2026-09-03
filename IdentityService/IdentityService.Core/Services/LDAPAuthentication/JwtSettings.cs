namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class JwtSettings
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Absolute cap on a session, measured from the original login (the "auth_time"
        /// claim), regardless of how many times the token has been renewed. Once passed,
        /// the user must authenticate again with password + RSA passcode.
        /// </summary>
        public int MaxSessionHours { get; set; } = 8;
    }
}
