using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Models.ViewModels;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if input is email or username
            var user = await _userManager.FindByEmailAsync(model.EmailOrUsername) ??
                       await _userManager.FindByNameAsync(model.EmailOrUsername);

            if (user == null)
                return Unauthorized(new { message = "Invalid login credentials" });

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return BadRequest(new { message = "Account is locked out" });

                return Unauthorized(new { message = "Invalid login credentials" });
            }

            // Update last login time
            user.LastLoginAt = DateTime.Now;
            await _userManager.UpdateAsync(user);

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // Generate JWT token
            var token = _jwtService.GenerateJwtToken(user, roles);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    displayName = user.DisplayName,
                    avatarUrl = user.AvatarUrl
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if email already exists
            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
                return BadRequest(new { message = "Email is already taken" });

            // Check if username already exists
            var existingUserByUsername = await _userManager.FindByNameAsync(model.Username);
            if (existingUserByUsername != null)
                return BadRequest(new { message = "Username is already taken" });

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                DisplayName = model.DisplayName,
                AvatarUrl = "/images/default-avatar.png", // Default avatar
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = result.Errors.First().Description });

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // Generate JWT token
            var token = _jwtService.GenerateJwtToken(user, roles);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    displayName = user.DisplayName,
                    avatarUrl = user.AvatarUrl
                }
            });
        }
    }
}
