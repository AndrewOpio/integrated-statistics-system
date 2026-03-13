namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }

        /// <summary>
        /// Indicates if the user must change their password before proceeding.
        /// </summary>
        public bool MustChangePassword { get; set; } = false;
    }
}
