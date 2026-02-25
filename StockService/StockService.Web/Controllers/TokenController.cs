using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StockService.Core.Models.Token;
using BCrypt.Net;

namespace StockService.Web.Controllers
{
    [ApiController]
    [Route("api")]
    public class TokenController(IConfiguration _config) : ControllerBase
    {
        [HttpPost("token")]
        public IActionResult GetToken([FromBody] ClientCredentials model)
        {
            // 1. Load clients from config
            var clients = _config.GetSection("Clients").Get<List<Client>>();

            // 2. Find the client
            var client = clients?.FirstOrDefault(c => c.ClientId == model.ClientId);

            // 3. Validate client and secret
            if (client == null || !BCrypt.Net.BCrypt.Verify(model.ClientSecret, client.ClientSecret))
                return Unauthorized(new { message = "Invalid client credentials" });

            // 4. Generate token
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: expiry,
                signingCredentials: creds,
                claims: new[]
                {
                    new Claim("client_id", client.ClientId),
                    new Claim("client_name", client.Name)
                }
            );

            // 5. Return token
            return Ok(new TokenResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresIn = 3600 // 1 hour in seconds
            });
        }

        //[HttpGet("hash")]
        //public IActionResult GenerateHash(string password)
        //{
        //    var hash = BCrypt.Net.BCrypt.HashPassword(password);
        //    return Ok(hash);
        //}
    }
}
