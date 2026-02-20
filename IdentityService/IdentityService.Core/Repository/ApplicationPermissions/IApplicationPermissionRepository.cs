namespace IdentityService.Core.Repository.ApplicationPermissions
{
    public interface IApplicationPermissionRepository
    {
        Task<List<string>> GetAllPermissionsForApplication(string acronym);
        Task<List<string>> GetAllPermissionsForApplicationForRole(string acronym, string roleId);
        Task AddPermissionsToRole(string acronym, string roleId, IEnumerable<string> permissions);
        Task RemovePermissionsFromRole(string roleId, IEnumerable<string> permissions);
        Task<bool> UserHasPermission(string userId, string permission);
        Task<List<string>> GetAllUserPermissions(string userId);
    }
}
