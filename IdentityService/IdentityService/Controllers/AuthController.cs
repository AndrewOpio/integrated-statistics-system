using IdentityService.Core.Repository.UserApplicationRoles;
using IdentityService.Core.Services.LDAPAuthentication;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ILdapAuthenticationService _ldapService,
        IJwtTokenService _jwtService,
        ILogger<AuthController> _logger,
            IConfiguration _config,
            IUserApplicationRoleRepository _userAppRoleRepo) : ControllerBase
    {

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate credentials against LDAP
                if (!(await _ldapService.ValidateCredentials(request.Username, request.Password)))
                {
                    _logger.LogError("Failed login attempt for user: {Username}", request.Username);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                List<Claim> claims = new();
                var userAppRoles = await _userAppRoleRepo.FindByUserName(request.Username);

                foreach (UserApplicationRole userAppRole in userAppRoles)
                {
                    claims.Add(new Claim("appRole", $"{userAppRole.ApplicationRole.Application.Acronym}:{userAppRole.ApplicationRole.Role.Name}"));
                }

                LdapUser user = new()
                {
                    Username = request.Username,
                    Email = "malinda@bou.or.ug",
                    DisplayName = "Martina Alinda",
                    UserClaims = claims
                };

                // Get user details from LDAP
                //var user = await _ldapService.GetUserDetails(request.Username);
                if (user == null)
                {
                    _logger.LogError("User authenticated but details not found: {Username}", request.Username);
                    return StatusCode(500, new { message = "Authentication succeeded but user details unavailable" });
                }

                user.UserClaims = claims;

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Successful login for user: {Username}", request.Username);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred during authentication" });
            }

            //Validate credentials(simplified -use real user store)
            //if (request.Username == "admin" && request.Password == "password123")
            //{
            //    var token = GenerateJwtToken(request.Username, new[] { "Admin", "User" });
            //    return Ok(new { token, username = request.Username });
            //}

            //return Unauthorized(new { message = "Invalid credentials" });
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


        //[HttpPost("validate")]
        //public IActionResult ValidateToken([FromBody] TokenRequest request)
        //{
        //    var handler = new JwtSecurityTokenHandler();
        //    var jwtSettings = _config.GetSection("JwtSettings");
        //    var secretKey = jwtSettings["SecretKey"];

        //    try
        //    {
        //        handler.ValidateToken(request.Token, new TokenValidationParameters
        //        {
        //            ValidateIssuer = true,
        //            ValidateAudience = true,
        //            ValidateLifetime = true,
        //            ValidateIssuerSigningKey = true,
        //            ValidIssuer = jwtSettings["Issuer"],
        //            ValidAudience = jwtSettings["Audience"],
        //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        //        }, out SecurityToken validatedToken);

        //        var jwtToken = (JwtSecurityToken)validatedToken;
        //        var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;

        //        return Ok(new { valid = true, username });
        //    }
        //    catch
        //    {
        //        return Ok(new { valid = false });
        //    }
        //}

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

    //public record LoginRequest(string Username, string Password);
    //public record TokenRequest(string Token);
}
