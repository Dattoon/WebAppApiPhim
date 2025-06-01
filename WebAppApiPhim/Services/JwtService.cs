using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace WebAppApiPhim.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtService(
            IConfiguration configuration,
            ILogger<JwtService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                return GenerateToken(user, roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);
                throw;
            }
        }

        public async Task<string> GenerateTokenWithRolesAsync(ApplicationUser user, IList<string> roles)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                return await Task.FromResult(GenerateToken(user, roles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token with roles for user: {UserId}", user.Id);
                throw;
            }
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                    new Claim("displayName", user.DisplayName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                };

                // Add role claims
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                        claims.Add(new Claim("role", role));
                    }
                }

                var jwtSecret = _configuration["JwtSettings:Secret"]
                    ?? _configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Secret is not configured.");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
                    SigningCredentials = creds,
                    Issuer = _configuration["JwtSettings:Issuer"] ?? _configuration["Jwt:Issuer"],
                    Audience = _configuration["JwtSettings:Audience"] ?? _configuration["Jwt:Audience"]
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("JWT token generated successfully for user: {UserId}", user.Id);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);

                _logger.LogDebug("Refresh token generated successfully");
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw;
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token cannot be null or empty", nameof(token));
            }

            try
            {
                var jwtSecret = _configuration["JwtSettings:Secret"]
                    ?? _configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Secret is not configured.");

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateLifetime = false // We want to get principal from expired token
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                _logger.LogDebug("Successfully extracted principal from expired token");
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting principal from expired token");
                throw;
            }
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token validation failed: Token is null or empty");
                return false;
            }

            try
            {
                var jwtSecret = _configuration["JwtSettings:Secret"]
                    ?? _configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Secret is not configured.");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtSecret);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(_configuration["JwtSettings:Issuer"] ?? _configuration["Jwt:Issuer"]),
                    ValidIssuer = _configuration["JwtSettings:Issuer"] ?? _configuration["Jwt:Issuer"],
                    ValidateAudience = !string.IsNullOrEmpty(_configuration["JwtSettings:Audience"] ?? _configuration["Jwt:Audience"]),
                    ValidAudience = _configuration["JwtSettings:Audience"] ?? _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
                };

                tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

                _logger.LogDebug("Token validation successful");
                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token validation failed: Token has expired");
                return false;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token validation failed: Invalid token");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token validation");
                return false;
            }
        }

        /// <summary>
        /// Extracts user ID from JWT token
        /// </summary>
        public string? GetUserIdFromToken(string token)
        {
            try
            {
                if (!ValidateToken(token))
                {
                    return null;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                return jsonToken.Claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user ID from token");
                return null;
            }
        }

        /// <summary>
        /// Gets remaining time until token expires
        /// </summary>
        public TimeSpan? GetTokenRemainingTime(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var expClaim = jsonToken.Claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value;
                if (expClaim == null) return null;

                var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
                var remaining = exp - DateTimeOffset.UtcNow;

                return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token remaining time");
                return null;
            }
        }
    }
}
