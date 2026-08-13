namespace VMSService.Static
{
    /// <summary>
    /// The AuthToken cookie is shared by every Aswam service on the host, so it is scoped
    /// to the parent domain. A cookie is identified by name + domain + path: issuing and
    /// deleting it with different options creates a second, host-only cookie and leaves
    /// the original in place. Every write to these cookies must go through here.
    /// </summary>
    public static class AuthCookie
    {
        public const string TokenCookieName = "AuthToken";
        public const string UsernameCookieName = "Username";

        public static CookieOptions Build(IConfiguration configuration, bool httpOnly)
        {
            var cookieDomain = configuration["Authentication:CookieDomain"];

            return new CookieOptions
            {
                HttpOnly = httpOnly,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Domain = string.IsNullOrWhiteSpace(cookieDomain) ? null : cookieDomain
            };
        }
    }
}
