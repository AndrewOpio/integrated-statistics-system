using AutoMapper;
using IdentityService.Core.Models;
using IdentityService.Core.Repository.UserApplicationRoles;
using IdentityService.Core.Repository.Users;
using IdentityService.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IUserRepository _userRepo, ILogger<UserController> _logger, IMapper _mapper,
        IUserApplicationRoleRepository _userApplicationRoleRepository) : ControllerBase
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

        [HttpPost("get-recipients")]
        public async Task<ActionResult<List<RecipientViewModel>>> GetRecipients([FromBody] RecipientRequest request)
        {
            try
            {
                var recipients = new List<RecipientDto>();

                if (!string.IsNullOrWhiteSpace(request.NotificationEmails))
                {
                    var emailAddresses = request.NotificationEmails
                        .Split(',')
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e));

                    foreach (var email in emailAddresses)
                    {
                        var recipient = await _userRepo.GetRecipientByEmail(email);
                        if (recipient != null)
                        {
                            recipients.Add(_mapper.Map<RecipientDto>(recipient));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NotificationRoleIds))
                {
                    var roleIds = request.NotificationRoleIds
                        .Split(',')
                        .Select(r => int.TryParse(r.Trim(), out var id) ? id : 0)
                        .Where(r => r > 0)
                        .ToList();

                    // Fetch users by role IDs from Identity Service
                    var usersWithRoles = await _userApplicationRoleRepository.GetUsersByRoleIds(roleIds);

                    foreach (var user in usersWithRoles)
                    {
                        recipients.Add(new RecipientDto
                        {
                            UserId = user.User.Id,
                            Email = user.User.Email,
                            DisplayName = $"{user.User.FirstName} {user.User.LastName}"
                        });
                    }
                }

                return Ok(recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipients");
                return StatusCode(500, "An error occurred while retrieving recipients.");
            }
        }

        [HttpGet("get-by-rolename/{roleName}")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByRoleName(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    return BadRequest("Role name is required.");
                }

                var userRoles = await _userApplicationRoleRepository.GetUsersByRoleName(roleName);
                var users = userRoles
                    .Select(x => x.User)
                    .DistinctBy(u => u.Id)
                    .Select(_mapper.Map<UserDto>)
                    .ToList();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role name: {RoleName}", roleName);
                return StatusCode(500, "An error occurred while retrieving users by role name.");
            }
        }

        [HttpGet("get-by-username/{username}")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest("Username is required.");
                }

                var user = await _userRepo.FindByName(username);
                
                if (user == null)
                {
                    _logger.LogWarning("User with username {Username} not found", username);
                    return NotFound($"User with username '{username}' not found.");
                }

                var userDto = _mapper.Map<UserDto>(user);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                return StatusCode(500, "An error occurred while retrieving the user.");
            }
        }
    }

    public class RecipientRequest
    {
        public string? NotificationEmails { get; set; }
        public string? NotificationRoleIds { get; set; }
    }
}