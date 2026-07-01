using System.Net.Http.Json;
using System.Security.Claims;
using VMSService.Core.Static;

namespace VMSService.Web.Services
{
    public class PermissionService(
        HttpClient _httpClient,
        IHttpContextAccessor _httpContextAccessor,
        ILogger<PermissionService> _logger) : IPermissionService
    {
        private const string ItemsKey = "CAS_UserPermissions";

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            var permissions = await GetPermissionsAsync(user);
            return permissions.Contains(permission);
        }

        private async Task<HashSet<string>> GetPermissionsAsync(ClaimsPrincipal user)
        {
            var context = _httpContextAccessor.HttpContext;

            // Return request-scoped cache if already fetched this request
            if (context?.Items[ItemsKey] is HashSet<string> cached)
                return cached;

            var acronym = SystemPolicy.SystemSettings.AppAcronym;

            var roleIds = user.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{acronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .OrderBy(x => x)
                .ToList();

            if (roleIds.Count == 0) return [];

            var url = $"api/permissions?acronym={acronym}&roleIds={string.Join(",", roleIds)}";
            try
            {
                var response = await _httpClient.GetFromJsonAsync<PermissionsResponse>(url);
                var permissions = response?.Permissions?.ToHashSet() ?? [];

                _logger.LogInformation("Fetched {Count} permissions for roles [{RoleIds}]",
                    permissions.Count, string.Join(",", roleIds));

                if (context != null)
                    context.Items[ItemsKey] = permissions;

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch permissions from IdentityService. URL: {Url}", url);
                return [];
            }
        }

        private record PermissionsResponse(List<string> Permissions);
    }
}
