namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class LdapSettings
    {
        public string Server { get; set; }
        public int Port { get; set; } = 636;
        public string BindDn { get; set; }
        public string BindPassword { get; set; }
        public string SearchBase { get; set; }
        public string SearchFilter { get; set; } = "(sAMAccountName={0})";
        public bool UseSSL { get; set; } = false;
    }
}
