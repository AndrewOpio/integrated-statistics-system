using System.Security.Claims;
using VMSService.Core.Static;

namespace VMSService.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static List<int> GetAppRoleIds(this ClaimsPrincipal user)
        {
            var prefix = $"{SystemPolicy.SystemSettings.AppAcronym}:";
            return user.FindAll(SystemPolicy.SystemSettings.AppRoleClaimType) 
                .Where(c => c.Value.StartsWith(prefix))
                .Select(c => int.TryParse(c.Value.AsSpan(prefix.Length), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
        }

        public static string GetUsernameOrSystem(this ClaimsPrincipal user)
            => user.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        public static List<string> GetAppRoleNames(this ClaimsPrincipal user)
        {
            var prefix = $"{SystemPolicy.SystemSettings.AppAcronym}:";
            return user.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith(prefix))
                .Select(c => c.Value[prefix.Length..])
                .ToList();
        }
    }
}
