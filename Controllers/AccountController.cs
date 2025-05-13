using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppApiPhim.Models;
using WebAppApiPhim.Models.ViewModels;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IUserService userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Xóa các cookie đăng nhập ngoài hiện có
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Kiểm tra xem đầu vào là email hay username
                var user = await _userManager.FindByEmailAsync(model.EmailOrUsername) ??
                           await _userManager.FindByNameAsync(model.EmailOrUsername);

                if (user != null)
                {
                    // Kiểm tra mật khẩu
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Người dùng đã đăng nhập.");

                        // Cập nhật thời gian đăng nhập
                        user.LastLoginAt = DateTime.Now;
                        await _userManager.UpdateAsync(user);

                        return RedirectToLocal(returnUrl);
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Tài khoản người dùng bị khóa.");
                        return RedirectToAction(nameof(Lockout));
                    }
                }

                ModelState.AddModelError(string.Empty, "Đăng nhập không hợp lệ.");
                return View(model);
            }

            // Nếu có lỗi, hiển thị lại form
            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Kiểm tra xem email đã tồn tại chưa
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
                    return View(model);
                }

                // Kiểm tra xem username đã tồn tại chưa
                var existingUserByUsername = await _userManager.FindByNameAsync(model.Username);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập này đã được sử dụng.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    DisplayName = model.DisplayName,
                    AvatarUrl = "/images/default-avatar.png", // Avatar mặc định
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Người dùng đã tạo tài khoản mới với mật khẩu.");

                    // Đăng nhập người dùng sau khi đăng ký
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("Người dùng đã đăng nhập sau khi đăng ký.");

                    return RedirectToLocal(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Nếu có lỗi, hiển thị lại form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Người dùng đã đăng xuất.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Không tiết lộ rằng người dùng không tồn tại hoặc chưa được xác nhận
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // Tạo mã đặt lại mật khẩu và gửi email
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { email = user.Email, code = code },
                    protocol: Request.Scheme);

                // Gửi email với link đặt lại mật khẩu
                // Trong môi trường thực tế, bạn sẽ gửi email thực sự ở đây
                _logger.LogInformation($"Link đặt lại mật khẩu: {callbackUrl}");

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Cần có mã để đặt lại mật khẩu.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Không tiết lộ rằng người dùng không tồn tại
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Email = user.Email,
                Username = user.UserName
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userService.UpdateUserProfileAsync(user.Id, model.DisplayName, model.AvatarUrl);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Thông tin cá nhân đã được cập nhật.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation("Người dùng đã thay đổi mật khẩu thành công.");
                TempData["StatusMessage"] = "Mật khẩu của bạn đã được thay đổi.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Đăng nhập bằng mạng xã hội
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Yêu cầu chuyển hướng đến nhà cung cấp đăng nhập ngoài
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp ngoài: {remoteError}");
                return View(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Đăng nhập người dùng với nhà cung cấp đăng nhập ngoài nếu họ đã có tài khoản
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("Người dùng đã đăng nhập với {Name}.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // Nếu người dùng không có tài khoản, yêu cầu họ tạo một tài khoản
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;

                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                return View("ExternalLogin", new ExternalLoginViewModel
                {
                    Email = email,
                    DisplayName = name,
                    Username = GenerateUsername(name)
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Lấy thông tin từ đăng nhập ngoài
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return RedirectToAction(nameof(Login));
                }

                // Kiểm tra xem email đã tồn tại chưa
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
                    return View("ExternalLogin", model);
                }

                // Kiểm tra xem username đã tồn tại chưa
                var existingUserByUsername = await _userManager.FindByNameAsync(model.Username);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập này đã được sử dụng.");
                    return View("ExternalLogin", model);
                }

                // Tạo người dùng mới
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    DisplayName = model.DisplayName,
                    AvatarUrl = GetAvatarFromExternalProvider(info),
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("Người dùng đã tạo tài khoản bằng {Name}.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View("ExternalLogin", model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        private string GenerateUsername(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "user" + Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            // Loại bỏ các ký tự không hợp lệ và thay thế khoảng trắng bằng dấu gạch dưới
            var username = new string(name.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray())
                .Replace(" ", "_")
                .ToLower();

            // Nếu username quá ngắn, thêm một số ngẫu nhiên
            if (username.Length < 3)
            {
                username += Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            // Nếu username quá dài, cắt bớt
            if (username.Length > 20)
            {
                username = username.Substring(0, 20);
            }

            return username;
        }

        private string GetAvatarFromExternalProvider(ExternalLoginInfo info)
        {
            // Thử lấy avatar từ các nhà cung cấp phổ biến
            if (info.LoginProvider == "Facebook")
            {
                var facebookId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(facebookId))
                {
                    return $"https://graph.facebook.com/{facebookId}/picture?type=large";
                }
            }
            else if (info.LoginProvider == "Google")
            {
                var picture = info.Principal.FindFirstValue("picture");
                if (!string.IsNullOrEmpty(picture))
                {
                    return picture;
                }
            }

            // Nếu không lấy được, sử dụng avatar mặc định
            return "/images/default-avatar.png";
        }
    }
}
