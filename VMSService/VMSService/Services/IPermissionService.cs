using System.Security.Claims;

namespace VMSService.Web.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
    }
}
