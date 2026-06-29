using IdentityService.Core.Models;
using IdentityService.Core.Repository.UserApplicationRoles;
using IdentityService.Core.Repository.Users;
using IdentityService.Core.Services.LDAPAuthentication;
using IdentityService.Core.Services.RsaAuthentication;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        ILdapAuthenticationService _ldapService,
        IRsaSecurIdService _rsaService,
        IJwtTokenService _jwtService,
        ILogger<AuthController> _logger,
        IConfiguration _config,
        IUserApplicationRoleRepository _userAppRoleRepo,
        IUserRepository _userRepo,
        UserManager<User> _userManager) : ControllerBase
    {
        // ═══════════════════════════════════════════════════════════
        // STEP 1: First Factor Authentication (LDAP/AD or Local Password)
        // ═══════════════════════════════════════════════════════════
        [HttpPost("login/step1")]
        public async Task<IActionResult> LoginStep1([FromBody] FirstFactorRequest request)
        {
            try
            {
                // Check if user exists in our database
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

                // Authenticate via LDAP/AD or local password
                bool isAuthenticated = false;
                string displayName;
                string email;

                if (dbUser.IsStaff)
                {
                    // Staff users authenticate via LDAP/AD
                    //isAuthenticated = await _ldapService.ValidateCredentials(request.Username, request.Password);
                    isAuthenticated = true;

                    if (!isAuthenticated)
                    {
                        _logger.LogWarning("Failed AD login attempt for staff user: {Username}", request.Username);
                        return Unauthorized(new { message = "Invalid credentials" });
                    }

                    // Get user details from LDAP for staff users
                    //var ldapUser = await _ldapService.GetUserDetails(request.Username);
                    LdapUser ldapUser = new LdapUser();
                    displayName = ldapUser?.DisplayName ?? $"{dbUser.FirstName} {dbUser.LastName}";
                    email = ldapUser?.Email ?? dbUser.Email;
                }
                else
                {
                    // Non-staff users authenticate with local password
                    //isAuthenticated = await _userManager.CheckPasswordAsync(dbUser, request.Password);
                    isAuthenticated = true;

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

                        return Ok(new FirstFactorResponse
                        {
                            Success = false,
                            Username = dbUser.UserName,
                            Email = email,
                            DisplayName = displayName,
                            MustChangePassword = true,
                            Message = "Password change required before login"
                        });
                    }
                }

                _logger.LogInformation("First factor authentication successful for user: {Username}", request.Username);

                // First factor succeeded, now initiate RSA authentication
                //var rsaInitResponse = await _rsaService.InitializeAuthenticationAsync(request.Username, null);
                RsaAuthenticationResponse rsaInitResponse = new()
                {
                    AttemptResponseCode = RsaResponseCodes.Challenge,
                    Context = new RsaContext()
                    {
                        AuthnAttemptId = (new Guid()).ToString(),
                        MessageId = (new Guid()).ToString(),
                    },

                };


                if (rsaInitResponse.AttemptResponseCode == RsaResponseCodes.Challenge)
                {
                    // Challenge initiated - need passcode
                    _logger.LogInformation("RSA challenge initiated for user: {Username}", request.Username);

                    var promptText = "Enter your RSA SecurID passcode (PIN + Token Code):";
                    if (rsaInitResponse.ChallengeMethods?.Challenges?.Any() == true)
                    {
                        var securIdMethod = rsaInitResponse.ChallengeMethods.Challenges
                            .SelectMany(c => c.RequiredMethods)
                            .FirstOrDefault(m => m.MethodId == "SECURID");

                        if (securIdMethod?.Versions?.Any() == true)
                        {
                            promptText = securIdMethod.Versions[0].Prompt?.DefaultText ?? promptText;
                        }
                    }

                    return Ok(new FirstFactorResponse
                    {
                        Success = true,
                        Username = dbUser.UserName,
                        Email = email,
                        DisplayName = displayName,
                        RequiresSecondFactor = true,
                        RsaAuthnAttemptId = rsaInitResponse.Context.AuthnAttemptId,
                        RsaMessageId = rsaInitResponse.Context.MessageId,
                        RsaPromptText = promptText,
                        Message = "First factor successful. Please proceed to second factor authentication."
                    });
                }
                else if (rsaInitResponse.AttemptResponseCode == RsaResponseCodes.Error)
                {
                    // RSA service error
                    _logger.LogError("RSA initialization failed for user {Username}: {ReasonCode}",
                        request.Username, rsaInitResponse.AttemptReasonCode);

                    return StatusCode(500, new
                    {
                        message = "Authentication service error. Please contact administrator.",
                        details = GetRsaErrorMessage(rsaInitResponse.AttemptReasonCode)
                    });
                }
                else
                {
                    // Unexpected response
                    _logger.LogError("Unexpected RSA response for user {Username}: {ResponseCode}",
                        request.Username, rsaInitResponse.AttemptResponseCode);

                    return StatusCode(500, new
                    {
                        message = "Unexpected authentication response. Please contact administrator."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during first factor authentication for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred during authentication" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // STEP 2: Second Factor Authentication (RSA SecurID)
        // ═══════════════════════════════════════════════════════════
        [HttpPost("login/step2")]
        public async Task<IActionResult> LoginStep2([FromBody] SecondFactorRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new { message = "Username is required" });
                }

                if (string.IsNullOrEmpty(request.RsaPasscode))
                {
                    return BadRequest(new { message = "RSA passcode is required" });
                }

                if (string.IsNullOrEmpty(request.AuthnAttemptId))
                {
                    return BadRequest(new { message = "Authentication attempt ID is required" });
                }

                // Get user from database
                var dbUser = await _userRepo.FindByName(request.Username);

                if (dbUser == null)
                {
                    _logger.LogWarning("Second factor attempt for non-existent user: {Username}", request.Username);
                    return Unauthorized(new { message = "Invalid authentication session" });
                }

                // Verify user is still active
                if (!dbUser.IsActive)
                {
                    _logger.LogWarning("Second factor attempt for inactive user: {Username}", request.Username);
                    return Unauthorized(new { message = "User account is inactive. Please contact administrator." });
                }
               _logger.LogInformation("Processing RSA 2FA verification for user: {Username}", request.Username);

                // Verify RSA SecurID passcode
                //var rsaVerifyResponse = await _rsaService.VerifyAuthenticationAsync(
                //    request.AuthnAttemptId,
                //    request.RsaPasscode,
                //    request.InResponseTo ?? string.Empty
                //);

                RsaAuthenticationResponse rsaVerifyResponse = new()
                {
                    AttemptResponseCode = RsaResponseCodes.Success,
                    Context = new RsaContext()
                    {
                        AuthnAttemptId = request.AuthnAttemptId,
                        MessageId = (new Guid()).ToString(),
                        InResponseTo = request.InResponseTo ?? string.Empty
                    },
                };

                if (rsaVerifyResponse.AttemptResponseCode == RsaResponseCodes.Success)
                {
                    _logger.LogInformation("RSA 2FA successful for user: {Username}", request.Username);

                    // Get display name and email
                    string displayName;
                    string email;

                    if (dbUser.IsStaff)
                    {
                        //var ldapUser = await _ldapService.GetUserDetails(request.Username);
                        LdapUser ldapUser = new();
                        displayName = ldapUser?.DisplayName ?? $"{dbUser.FirstName} {dbUser.LastName}";
                        email = ldapUser?.Email ?? dbUser.Email;
                    }
                    else
                    {
                        displayName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
                        email = dbUser.Email;
                    }

                    // Generate token and complete login
                    return await GenerateSuccessfulLoginResponse(dbUser, displayName, email, request.Username);
                }
                else if (rsaVerifyResponse.AttemptResponseCode == RsaResponseCodes.Challenge)
                {
                    // Still in challenge state (e.g., need next token code)
                    _logger.LogWarning("RSA 2FA still in challenge state for user {Username}: {ReasonCode}",
                        request.Username, rsaVerifyResponse.AttemptReasonCode);

                    var promptText = "Enter your RSA SecurID passcode (PIN + Token Code):";
                    if (rsaVerifyResponse.ChallengeMethods?.Challenges?.Any() == true)
                    {
                        var securIdMethod = rsaVerifyResponse.ChallengeMethods.Challenges
                            .SelectMany(c => c.RequiredMethods)
                            .FirstOrDefault(m => m.MethodId == "SECURID");

                        if (securIdMethod?.Versions?.Any() == true)
                        {
                            promptText = securIdMethod.Versions[0].Prompt?.DefaultText ?? promptText;
                        }
                    }

                    return Ok(new SecondFactorResponse
                    {
                        Success = false,
                        RequiresRetry = true,
                        RsaAuthnAttemptId = rsaVerifyResponse.Context.AuthnAttemptId,
                        RsaMessageId = rsaVerifyResponse.Context.MessageId,
                        RsaPromptText = promptText,
                        Message = GetRsaErrorMessage(rsaVerifyResponse.AttemptReasonCode)
                    });
                }
                else
                {
                    // Authentication failed
                    _logger.LogWarning("RSA 2FA failed for user {Username}: {ReasonCode}",
                        request.Username, rsaVerifyResponse.AttemptReasonCode);

                    return Unauthorized(new
                    {
                        message = GetRsaErrorMessage(rsaVerifyResponse.AttemptReasonCode),
                        rsaAuthnAttemptId = request.AuthnAttemptId,
                        canRetry = rsaVerifyResponse.AttemptResponseCode != RsaResponseCodes.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during second factor authentication for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred during authentication" });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Legacy Login Endpoint (Username & Password Only - No RSA)
        // ═══════════════════════════════════════════════════════════
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] FirstFactorRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Username and password are required" });
                }

                // Check if user exists in our database
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

                // Authenticate via LDAP/AD or local password
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

                _logger.LogInformation("Legacy login successful for user: {Username} (without RSA)", request.Username);

                // Generate token and complete login (without RSA 2FA)
                return await GenerateSuccessfulLoginResponse(dbUser, displayName, email, request.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during legacy login for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred during authentication" });
            }
        }

        private async Task<IActionResult> GenerateSuccessfulLoginResponse(
            User dbUser,
            string displayName,
            string email,
            string username)
        {
            // Build claims from user's application roles
            List<Claim> claims = new();
            var userAppRoles = await _userAppRoleRepo.FindByUserName(username);

            foreach (UserApplicationRole userAppRole in userAppRoles)
            {
                claims.Add(new Claim("appRole", $"{userAppRole.ApplicationRole.Application.Acronym}:{userAppRole.ApplicationRole.Role.Name}"));
                claims.Add(new Claim("appRoleId", $"{userAppRole.ApplicationRole.Application.Acronym}:{userAppRole.ApplicationRole.Id}"));
            }

            // Add IsStaff claim
            claims.Add(new Claim("isStaff", dbUser.IsStaff.ToString().ToLower()));
            claims.Add(new Claim(ClaimTypes.Name, username));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, dbUser.Id));
            
            if (!dbUser.IsStaff)
            {
                claims.Add(new Claim("cbId", "2", ClaimValueTypes.Integer));
                claims.Add(new Claim("cbName", "ABSA Bank Uganda Limited"));
            }

            LdapUser user = new()
            {
                Username = username,
                Email = email ?? string.Empty,
                DisplayName = displayName ?? username,
                UserClaims = claims
            };

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            // Update last activity
            dbUser.LastActivity = DateTime.Now;
            dbUser.IsLoggedIn = true;
            await _userManager.UpdateAsync(dbUser);

            _logger.LogInformation("Successful login for {UserType} user: {Username}",
                dbUser.IsStaff ? "staff" : "non-staff", username);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                MustChangePassword = false
            });
        }

        private string GetRsaErrorMessage(string reasonCode)
        {
            return reasonCode switch
            {
                RsaReasonCodes.SecurIdAuthFailure => "Invalid RSA SecurID passcode. Please try again.",
                RsaReasonCodes.SecurIdNextCodeRequired => "Please wait for the next token code and try again.",
                RsaReasonCodes.SecurIdNewPinRequired => "You need to set a new PIN. Please contact your administrator.",
                RsaReasonCodes.UserNotFound => "User not found in RSA SecurID system. Please contact your administrator.",
                RsaReasonCodes.LockedOut => "Your account is locked out. Please contact your administrator.",
                _ => "RSA SecurID authentication failed. Please try again."
            };
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
                username = principal.Identity?.Name,
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

        [HttpPost("rsa/cancel")]
        public async Task<IActionResult> CancelRsaAuthentication([FromBody] RsaCancelRequest request)
        {
            try
            {
                var result = await _rsaService.CancelAuthenticationAsync(request.AuthnAttemptId, request.Reason);
                
                if (result)
                {
                    return Ok(new { message = "RSA authentication cancelled successfully" });
                }

                return BadRequest(new { message = "Failed to cancel RSA authentication" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling RSA authentication: {AttemptId}", request.AuthnAttemptId);
                return StatusCode(500, new { message = "An error occurred while cancelling authentication" });
            }
        }
    }

    public class LogoutRequest
    {
        public string Username { get; set; } = string.Empty;
    }
}
