using IdentityService.Core.Models;
using IdentityService.Core.Services.AdAuthentication;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public interface ILdapAuthenticationService
    {
        Task<bool> ValidateCredentials(string username, string password);
        Task<LdapUser> GetUserDetails(string username);
        AdUserResult IsExistsOnAd(UserViewModel model);
    }
}
