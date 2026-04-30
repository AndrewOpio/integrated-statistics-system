using IdentityService.Core.Repository.ApplicationRoles;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppRoleController(IApplicationRoleRepository _appRoleRepo, ILogger<AppController> _logger) : ControllerBase
    {
        [HttpGet("app-roles")]
        public async Task<IActionResult> GetRolesForApplication(int applicationId)
        {
            List<ApplicationRole> data = await _appRoleRepo.FindByApplicationId(applicationId);
            return Ok(data);
        }

        [HttpGet("app-role")]
        public async Task<IActionResult> GetAppRoleById(int id)
        {
            ApplicationRole data = await _appRoleRepo.FindById(id);
            return Ok(data);
        }
    }
}
