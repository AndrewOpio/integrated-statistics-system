using IdentityService.Core.Static;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using System.Reflection;
using System.Security.Claims;

namespace IdentityService.Core.Repository.ApplicationPermissions
{
    public class ApplicationPermissionRepository(RoleManager<Role> _roleManager
    , UserManager<User> _userManager) : IApplicationPermissionRepository
    {
        public async Task AddPermissionsToRole(string acronym, string roleId, IEnumerable<string> permissions)
        {
            var role = await _roleManager.FindByIdAsync(roleId) ?? throw new ArgumentException($"Role does not exist.");
            var currentPermissions = await GetAllPermissionsForApplicationForRole(acronym, roleId);
            var newPermissions = permissions.Except(currentPermissions);

            foreach (var permission in newPermissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }
        }

        public Task<List<string>> GetAllPermissionsForApplication(string acronym)
        {
            // Reflect to get all permission constants
            var permissions = new List<string>();

            // Get all nested classes in the SystemPermissions class
            var nestedTypes = typeof(SystemPermissions).GetNestedTypes();

            foreach (var type in nestedTypes)
            {
                // Get all string constants
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string));

                foreach (var field in fields)
                {
                    if ((field.GetValue(null) is string permissionValue) && permissionValue.StartsWith(acronym))
                    {
                        permissions.Add(permissionValue);
                    }
                }
            }

            return Task.FromResult(permissions);
        }

        public async Task<List<string>> GetAllPermissionsForApplicationForRole(string acronym, string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                return new List<string>();

            var claims = await _roleManager.GetClaimsAsync(role);
            return claims
                .Where(c => c.Type == "Permission" && c.Value.StartsWith(acronym))
                .Select(c => c.Value)
                .ToList();
        }

        public Task<List<string>> GetAllUserPermissions(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task RemovePermissionsFromRole(string roleId, IEnumerable<string> permissions)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                throw new ArgumentException($"Role does not exist.");

            var currentClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var permission in permissions)
            {
                var claimToRemove = currentClaims.FirstOrDefault(c =>
                    c.Type == "Permission" && c.Value == permission);

                if (claimToRemove != null)
                {
                    await _roleManager.RemoveClaimAsync(role, claimToRemove);
                }
            }
        }

        public Task<bool> UserHasPermission(string userId, string permission)
        {
            throw new NotImplementedException();
        }
    }
}
