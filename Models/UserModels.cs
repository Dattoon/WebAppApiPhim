using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người dùng")]
        [Display(Name = "Tên người dùng")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập?")]
        public bool RememberMe { get; set; }
    }

    public class UserWatchHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public double WatchedPercentage { get; set; }
        public DateTime LastWatchedAt { get; set; }
    }

    public class UserFavorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class UserProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime JoinedDate { get; set; }
        public List<Favorite> Favorites { get; set; }
        public List<WatchHistory> WatchHistory { get; set; }
    }
}