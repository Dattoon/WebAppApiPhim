using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppApiPhim.Models;
using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email already exists"
                    });
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    DisplayName = request.DisplayName ?? request.Email.Split('@')[0],
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");

                    var token = await GenerateJwtTokenAsync(user);

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "User registered successfully",
                        Data = new
                        {
                            Token = token,
                            User = new
                            {
                                user.Id,
                                user.Email,
                                user.DisplayName,
                                user.CreatedAt
                            }
                        }
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

                if (result.Succeeded)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    var token = await GenerateJwtTokenAsync(user);

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Login successful",
                        Data = new
                        {
                            Token = token,
                            User = new
                            {
                                user.Id,
                                user.Email,
                                user.DisplayName,
                                user.LastLoginAt
                            }
                        }
                    });
                }

                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        [HttpPost("test-token")]
        public async Task<IActionResult> GenerateTestToken()
        {
            try
            {
                // Tạo user test nếu chưa có
                var testEmail = "test@example.com";
                var testUser = await _userManager.FindByEmailAsync(testEmail);

                if (testUser == null)
                {
                    testUser = new ApplicationUser
                    {
                        UserName = testEmail,
                        Email = testEmail,
                        DisplayName = "Test User",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(testUser, "Test123!");
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(testUser, "User");
                    }
                }

                var token = await GenerateJwtTokenAsync(testUser);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Test token generated",
                    Data = new
                    {
                        Token = token,
                        User = new
                        {
                            testUser.Id,
                            testUser.Email,
                            testUser.DisplayName
                        },
                        Instructions = "Use this token in Authorization header: Bearer {token}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test token");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("DisplayName", user.DisplayName ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["TokenExpirationMinutes"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        public string DisplayName { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
