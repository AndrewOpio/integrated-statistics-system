using IdentityService.Core.Models;
using IdentityService.Core.Repository.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IUserRepository _userRepo, ILogger<UserController> _logger) : ControllerBase
    {
        [HttpGet("non-staff")]
        public async Task<ActionResult<List<NonStaffUserViewModel>>> GetNonStaffUsers()
        {
            try
            {
                var nonStaffUsers = await _userRepo.GetNonStaffUserList();
                return Ok(nonStaffUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving non-staff users");
                return StatusCode(500, "An error occurred while retrieving non-staff users.");
            }
        }
    }
}