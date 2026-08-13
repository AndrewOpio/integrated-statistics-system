using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class JwtTokenService : IJwtTokenService
    {
        /// <summary>
        /// Unix-seconds timestamp of the original login. Survives every renewal so the
        /// absolute session cap cannot be reset by extending.
        /// </summary>
        public const string AuthTimeClaim = "auth_time";

        /// <summary>
        /// Claims the JwtSecurityToken constructor writes itself. Carrying them over from
        /// the old token would duplicate them, so they are dropped on renewal.
        /// </summary>
        private static readonly HashSet<string> ReservedClaims = new(StringComparer.Ordinal)
        {
            JwtRegisteredClaimNames.Exp,
            JwtRegisteredClaimNames.Iat,
            JwtRegisteredClaimNames.Nbf,
            JwtRegisteredClaimNames.Jti,
            JwtRegisteredClaimNames.Iss,
            JwtRegisteredClaimNames.Aud
        };

        private readonly JwtSettings _jwtSettings;

        public JwtTokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string GenerateToken(LdapUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //var claims = new[]
            //{
            //    new Claim(ClaimTypes.Name, user.Username),
            //    new Claim(ClaimTypes.Email, user.Email ?? ""),
            //    new Claim("DisplayName", user.DisplayName ?? user.Username),
            //    new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            //};

            user.UserClaims.Add(new Claim(ClaimTypes.Name, user.Username));
            user.UserClaims.Add(new Claim(ClaimTypes.Email, user.Email ?? ""));
            user.UserClaims.Add(new Claim("DisplayName", user.DisplayName ?? user.Username));
            user.UserClaims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Username));
            user.UserClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            user.UserClaims.Add(new Claim(AuthTimeClaim, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: user.UserClaims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public TokenRenewalResult RenewToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return TokenRenewalResult.Failed("No token supplied.");
            }

            // Read the raw claims rather than going through ValidateToken: that returns a
            // ClaimsPrincipal with inbound claim types remapped ("sub" -> nameidentifier
            // and friends), which would not round-trip back into a new token intact.
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            JwtSecurityToken jwt;

            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    // An already-expired token cannot be extended - that would be a silent
                    // re-login. The client is expected to renew before expiry.
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                jwt = (JwtSecurityToken)validatedToken;
            }
            catch (SecurityTokenExpiredException)
            {
                return TokenRenewalResult.Failed("Session has already expired. Please sign in again.");
            }
            catch
            {
                return TokenRenewalResult.Failed("Invalid token.");
            }

            var authenticatedAt = ReadAuthTime(jwt);

            // Absolute cap: renewals extend a session, they never restart it.
            var sessionEndsAt = authenticatedAt.AddHours(_jwtSettings.MaxSessionHours);
            if (DateTime.UtcNow >= sessionEndsAt)
            {
                return new TokenRenewalResult
                {
                    Success = false,
                    AuthenticatedAtUtc = authenticatedAt,
                    Message = $"Maximum session length of {_jwtSettings.MaxSessionHours} hours reached. Please sign in again."
                };
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            // Never let a renewal run past the cap.
            if (expiresAt > sessionEndsAt)
            {
                expiresAt = sessionEndsAt;
            }

            var claims = jwt.Claims
                .Where(c => !ReservedClaims.Contains(c.Type))
                .ToList();

            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            // Tokens issued before auth_time existed derive it from their issued-at. Stamp it
            // onto the renewed token, otherwise the next renewal derives it from the new
            // issued-at instead and the session cap resets every time it is extended.
            if (!claims.Any(c => c.Type == AuthTimeClaim))
            {
                var authTimeSeconds = new DateTimeOffset(
                    DateTime.SpecifyKind(authenticatedAt, DateTimeKind.Utc)).ToUnixTimeSeconds();

                claims.Add(new Claim(AuthTimeClaim, authTimeSeconds.ToString()));
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                SecurityAlgorithms.HmacSha256);

            var renewed = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new TokenRenewalResult
            {
                Success = true,
                Token = new JwtSecurityTokenHandler().WriteToken(renewed),
                ExpiresAtUtc = expiresAt,
                AuthenticatedAtUtc = authenticatedAt
            };
        }

        /// <summary>
        /// Original login time. Tokens issued before the auth_time claim existed fall back
        /// to their own issued-at, so sessions live at that point keep working.
        /// </summary>
        private static DateTime ReadAuthTime(JwtSecurityToken jwt)
        {
            var authTime = jwt.Claims.FirstOrDefault(c => c.Type == AuthTimeClaim)?.Value;

            if (long.TryParse(authTime, out var unixSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            }

            return jwt.IssuedAt != DateTime.MinValue ? jwt.IssuedAt : DateTime.UtcNow;
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
