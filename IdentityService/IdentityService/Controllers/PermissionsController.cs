using IdentityService.Core.Repository.ApplicationPermissions;
using IdentityService.Core.Repository.ApplicationRoles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class PermissionsController(
        IApplicationPermissionRepository _permissionRepo,
        IApplicationRoleRepository _appRoleRepo) : ControllerBase
    {
        /// <summary>
        /// Returns all permissions assigned to the given application roles.
        /// Called by microservices to resolve permissions without embedding them in the JWT.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPermissions([FromQuery] string acronym, [FromQuery] string roleIds)
        {
            if (string.IsNullOrWhiteSpace(acronym) || string.IsNullOrWhiteSpace(roleIds))
                return BadRequest(new { message = "acronym and roleIds are required." });

            var ids = roleIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out var n) ? (int?)n : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var permissions = new HashSet<string>();

            foreach (var appRoleId in ids)
            {
                var appRole = await _appRoleRepo.FindById(appRoleId);
                if (appRole == null) continue;

                var rolePermissions = await _permissionRepo.GetAllPermissionsForApplicationForRole(acronym, appRole.RoleId);
                foreach (var p in rolePermissions)
                    permissions.Add(p);
            }

            return Ok(new { permissions = permissions.ToList() });
        }
    }
}
