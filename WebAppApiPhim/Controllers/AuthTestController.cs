using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthTestController : ControllerBase
    {
        private readonly ILogger<AuthTestController> _logger;

        public AuthTestController(ILogger<AuthTestController> logger)
        {
            _logger = logger;
        }

        // Test endpoint without authentication
        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok(new { message = "This is a public endpoint", timestamp = DateTime.UtcNow });
        }

        // Test endpoint with authentication
        [HttpGet("protected")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult ProtectedEndpoint()
        {
            var authInfo = new
            {
                message = "This is a protected endpoint",
                isAuthenticated = User.Identity?.IsAuthenticated,
                authenticationType = User.Identity?.AuthenticationType,
                name = User.Identity?.Name,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
                timestamp = DateTime.UtcNow
            };

            return Ok(authInfo);
        }

        // Test token validation
        [HttpPost("validate-token")]
        public IActionResult ValidateToken([FromBody] TokenValidationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return BadRequest("Token is required");
                }

                // Remove "Bearer " prefix if present
                var token = request.Token.StartsWith("Bearer ")
                    ? request.Token.Substring(7)
                    : request.Token;

                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var tokenInfo = new
                {
                    isValid = true,
                    issuer = jsonToken.Issuer,
                    audience = jsonToken.Audiences.FirstOrDefault(),
                    expiry = jsonToken.ValidTo,
                    isExpired = jsonToken.ValidTo < DateTime.UtcNow,
                    claims = jsonToken.Claims.Select(c => new { c.Type, c.Value }).ToList()
                };

                return Ok(tokenInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return BadRequest(new { message = "Invalid token", error = ex.Message });
            }
        }
    }

    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
