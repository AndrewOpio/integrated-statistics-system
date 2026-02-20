using IdentityService.Core.Repository.Applications;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppController(IApplicationRepository _appRepo, ILogger<AppController> _logger) : ControllerBase
    {
        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications()
        {
            var data = await _appRepo.FindAll();
            return Ok(data);
        }
    }
}
