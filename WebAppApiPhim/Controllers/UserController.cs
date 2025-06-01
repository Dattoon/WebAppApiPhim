using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppApiPhim.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<UserController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // POST: api/user/register
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> Register([FromQuery] string username, [FromQuery] string email, [FromQuery] string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Invalid registration parameters provided.");
                return BadRequest("Username, email, and password are required.");
            }

            try
            {
                // Kiểm tra user đã tồn tại chưa
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    _logger.LogWarning($"Registration failed: Email {email} already exists.");
                    return BadRequest("Email already exists.");
                }

                var existingUsername = await _userManager.FindByNameAsync(username);
                if (existingUsername != null)
                {
                    _logger.LogWarning($"Registration failed: Username {username} already exists.");
                    return BadRequest("Username already exists.");
                }

                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning($"Registration failed: {errors}");
                    return BadRequest($"Registration failed: {errors}");
                }

                _logger.LogInformation($"User {username} registered successfully.");
                var token = await GenerateJwtToken(user);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during registration.");
            }
        }

        // POST: api/user/login
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> Login([FromQuery] string email, [FromQuery] string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Invalid login parameters provided.");
                return BadRequest("Email and password are required.");
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: User with email {email} not found.");
                    return BadRequest("Invalid email or password.");
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning($"Login failed for user {email}.");
                    return BadRequest("Invalid email or password.");
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation($"User {user.UserName} logged in successfully.");
                var token = await GenerateJwtToken(user);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during login.");
            }
        }

        // GET: api/user/profile
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApplicationUser>> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found.");
                    return NotFound($"User with ID {userId} not found.");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the user profile.");
            }
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty), // Null safety
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),   // Null safety
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // SỬA: Sử dụng JwtSettings thay vì Jwt
            var jwtSecret = _configuration["JwtSettings:Secret"] ?? _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["JwtSettings:Issuer"] ?? _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["JwtSettings:Audience"] ?? _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtSecret))
            {
                _logger.LogError("JWT Secret not found in configuration");
                throw new InvalidOperationException("JWT Secret is not configured");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation($"JWT token generated for user {user.UserName} with expiry {DateTime.UtcNow.AddHours(24)}");

            return tokenString;
        }
    }
}
