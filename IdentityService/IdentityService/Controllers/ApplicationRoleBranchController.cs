using ConfigurationsService.Client;
using IdentityService.Core.Repository.ApplicationRoleBranches;
using IdentityService.Core.Repository.ApplicationRoles;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationRoleBranchController(
        IApplicationRoleBranchRepository _repo,
        IApplicationRoleRepository _appRoleRepo,
        IConfigurationsServiceClient _configServiceClient,
        ILogger<ApplicationRoleBranchController> _logger) : ControllerBase
    {
        [HttpGet("{applicationRoleId}/branches")]
        public async Task<IActionResult> GetBranchesForRole(int applicationRoleId)
        {
            var assignments = await _repo.FindByApplicationRoleId(applicationRoleId);
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var branchMap = branches?.ToDictionary(b => b.Id, b => b.Name) ?? [];

            var result = assignments.Select(a => new
            {
                a.Id,
                a.ApplicationRoleId,
                a.BranchId,
                BranchName = branchMap.TryGetValue(a.BranchId, out var name) ? name : "Unknown"
            });

            return Ok(result);
        }

        [HttpPost("{applicationRoleId}/branches")]
        public async Task<IActionResult> AssignBranch(int applicationRoleId, [FromBody] AssignBranchRequest request)
        {
            var roleExists = await _appRoleRepo.IsExists(applicationRoleId);
            if (!roleExists)
                return NotFound(new { message = "Application role not found." });

            if (await _repo.IsExists(applicationRoleId, request.BranchId))
                return Conflict(new { message = "This branch is already assigned to this role." });

            var entity = new ApplicationRoleBranch
            {
                ApplicationRoleId = applicationRoleId,
                BranchId = request.BranchId
            };

            var success = await _repo.Create(entity);
            if (!success)
                return StatusCode(500, new { message = "Failed to assign branch." });

            return Ok(new { message = "Branch assigned successfully.", entity.Id });
        }

        [HttpDelete("branches/{id}")]
        public async Task<IActionResult> RemoveBranch(int id)
        {
            var entity = await _repo.FindById(id);
            if (entity == null)
                return NotFound(new { message = "Assignment not found." });

            var success = await _repo.Delete(id);
            if (!success)
                return StatusCode(500, new { message = "Failed to remove branch assignment." });

            return Ok(new { message = "Branch assignment removed." });
        }
    }

    public class AssignBranchRequest
    {
        public int BranchId { get; set; }
    }
}
