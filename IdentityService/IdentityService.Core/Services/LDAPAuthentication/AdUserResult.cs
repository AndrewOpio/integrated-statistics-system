using IdentityService.Data.Entities;
using System.DirectoryServices.AccountManagement;

namespace IdentityService.Core.Services.AdAuthentication
{
    public class AdUserResult
    {
        public UserPrincipal User { get; set; }
        public User AppUser { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public string ResultType { get; set; }
    }
}
