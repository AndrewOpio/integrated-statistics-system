using IdentityService.Core.Models;
using IdentityService.Core.Repository.UserApplicationRoles;
using IdentityService.Core.Repository.Users;
using IdentityService.Core.Services.LDAPAuthentication;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        ILdapAuthenticationService _ldapService,
        IJwtTokenService _jwtService,
        ILogger<AuthController> _logger,
        IConfiguration _config,
        IUserApplicationRoleRepository _userAppRoleRepo,
        IUserRepository _userRepo,
        UserManager<User> _userManager) : ControllerBase
    {

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // First, check if user exists in our database
                var dbUser = await _userRepo.FindByName(request.Username);

                if (dbUser == null)
                {
                    _logger.LogWarning("Login attempt for non-existent user: {Username}", request.Username);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Check if user is active
                if (!dbUser.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive user: {Username}", request.Username);
                    return Unauthorized(new { message = "User account is inactive. Please contact administrator." });
                }

                // Check if user account has expired
                if (dbUser.EndDate.HasValue && dbUser.EndDate.Value < DateTime.Now)
                {
                    _logger.LogWarning("Login attempt for expired user account: {Username}", request.Username);
                    return Unauthorized(new { message = "User account has expired. Please contact administrator." });
                }

                bool isAuthenticated = false;
                string displayName;
                string email;

                if (dbUser.IsStaff)
                {
                    // Staff users authenticate via LDAP/AD
                    isAuthenticated = await _ldapService.ValidateCredentials(request.Username, request.Password);

                    if (!isAuthenticated)
                    {
                        _logger.LogWarning("Failed AD login attempt for staff user: {Username}", request.Username);
                        return Unauthorized(new { message = "Invalid credentials" });
                    }

                    // Get user details from LDAP for staff users
                    var ldapUser = await _ldapService.GetUserDetails(request.Username);
                    displayName = ldapUser?.DisplayName ?? $"{dbUser.FirstName} {dbUser.LastName}";
                    email = ldapUser?.Email ?? dbUser.Email;
                }
                else
                {
                    // Non-staff users authenticate with local password
                    isAuthenticated = await _userManager.CheckPasswordAsync(dbUser, request.Password);

                    if (!isAuthenticated)
                    {
                        _logger.LogWarning("Failed local login attempt for non-staff user: {Username}", request.Username);
                        return Unauthorized(new { message = "Invalid credentials" });
                    }

                    displayName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
                    email = dbUser.Email;

                    // Check if non-staff user must change password
                    if (dbUser.MustChangePassword)
                    {
                        _logger.LogInformation("User {Username} must change password before proceeding", request.Username);

                        return Ok(new LoginResponse
                        {
                            Token = null,
                            Username = dbUser.UserName,
                            Email = email,
                            DisplayName = displayName,
                            MustChangePassword = true
                        });
                    }
                }

                // Build claims from user's application roles
                List<Claim> claims = new();
                var userAppRoles = await _userAppRoleRepo.FindByUserName(request.Username);

                foreach (UserApplicationRole userAppRole in userAppRoles)
                {
                    claims.Add(new Claim("appRole", $"{userAppRole.ApplicationRole.Application.Acronym}:{userAppRole.ApplicationRole.Role.Name}"));
                }

                // Add IsStaff claim
                claims.Add(new Claim("isStaff", dbUser.IsStaff.ToString().ToLower()));
                if (!dbUser.IsStaff)
                {
                    claims.Add(new Claim("cbId", "2", ClaimValueTypes.Integer));
                    claims.Add(new Claim("cbName", "ABSA Bank Uganda Limited"));
                }
                claims.Add(new Claim(ClaimTypes.Name, request.Username));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, dbUser.Id));

                LdapUser user = new()
                {
                    Username = request.Username,
                    Email = email ?? string.Empty,
                    DisplayName = displayName ?? request.Username,
                    UserClaims = claims
                };

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                // Update last activity
                dbUser.LastActivity = DateTime.Now;
                dbUser.IsLoggedIn = true;
                await _userManager.UpdateAsync(dbUser);

                _logger.LogInformation("Successful login for {UserType} user: {Username}",
                    dbUser.IsStaff ? "staff" : "non-staff", request.Username);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    MustChangePassword = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred during authentication" });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var dbUser = await _userRepo.FindByName(request.Username);

                if (dbUser == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                if (dbUser.IsStaff)
                {
                    return BadRequest(new { message = "Staff users cannot change their password here. Please use Active Directory." });
                }

                // Verify current password first
                var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(dbUser, request.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    return BadRequest(new { message = "Current password is incorrect." });
                }

                // Change password using repository method (includes validation)
                var result = await _userRepo.ChangePassword(dbUser.Id, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = errors });
                }

                _logger.LogInformation("Password changed successfully for user {Username}", request.Username);

                // Now generate full login response
                var displayName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
                var email = dbUser.Email;

                // Build claims from user's application roles
                List<Claim> claims = new();
                var userAppRoles = await _userAppRoleRepo.FindByUserName(request.Username);

                foreach (UserApplicationRole userAppRole in userAppRoles)
                {
                    claims.Add(new Claim("appRole", $"{userAppRole.ApplicationRole.Application.Acronym}:{userAppRole.ApplicationRole.Role.Name}"));
                }

                claims.Add(new Claim("isStaff", dbUser.IsStaff.ToString().ToLower()));

                LdapUser user = new()
                {
                    Username = request.Username,
                    Email = email ?? string.Empty,
                    DisplayName = displayName ?? request.Username,
                    UserClaims = claims
                };

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                // Update last activity
                dbUser.LastActivity = DateTime.Now;
                dbUser.IsLoggedIn = true;
                await _userManager.UpdateAsync(dbUser);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    MustChangePassword = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred while changing password" });
            }
        }

        [HttpPost("validate")]
        public IActionResult ValidateToken([FromBody] TokenValidationRequest request)
        {
            var principal = _jwtService.ValidateToken(request.Token);

            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid or expired token" });
            }

            return Ok(new
            {
                valid = true,
                username = principal.Identity.Name,
                claims = principal.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                var dbUser = await _userRepo.FindByName(request.Username);
                if (dbUser != null)
                {
                    dbUser.IsLoggedIn = false;
                    await _userManager.UpdateAsync(dbUser);
                    _logger.LogInformation("User logged out: {Username}", request.Username);
                }

                string message = $"{request.Username} logged out successfully";

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        private string GenerateJwtToken(string username, string[] roles)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LogoutRequest
    {
        public string Username { get; set; }
    }
}
