using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Seed Genres
            if (!await context.Genres.AnyAsync())
            {
                var genres = new List<Genre>
                {
                    new Genre { Name = "Hành Động", Slug = "hanh-dong", Description = "Phim hành động", IsActive = true },
                    new Genre { Name = "Tình Cảm", Slug = "tinh-cam", Description = "Phim tình cảm", IsActive = true },
                    new Genre { Name = "Hài Hước", Slug = "hai-huoc", Description = "Phim hài hước", IsActive = true },
                    new Genre { Name = "Cổ Trang", Slug = "co-trang", Description = "Phim cổ trang", IsActive = true },
                    new Genre { Name = "Kinh Dị", Slug = "kinh-di", Description = "Phim kinh dị", IsActive = true },
                    new Genre { Name = "Tâm Lý", Slug = "tam-ly", Description = "Phim tâm lý", IsActive = true },
                    new Genre { Name = "Khoa Học Viễn Tưởng", Slug = "khoa-hoc-vien-tuong", Description = "Phim khoa học viễn tưởng", IsActive = true },
                    new Genre { Name = "Phiêu Lưu", Slug = "phieu-luu", Description = "Phim phiêu lưu", IsActive = true },
                    new Genre { Name = "Hình Sự", Slug = "hinh-su", Description = "Phim hình sự", IsActive = true },
                    new Genre { Name = "Chiến Tranh", Slug = "chien-tranh", Description = "Phim chiến tranh", IsActive = true }
                };

                await context.Genres.AddRangeAsync(genres);
                await context.SaveChangesAsync();
            }

            // Seed Countries
            if (!await context.Countries.AnyAsync())
            {
                var countries = new List<Country>
                {
                    new Country { Name = "Việt Nam", Slug = "viet-nam", Code = "VN", IsActive = true },
                    new Country { Name = "Trung Quốc", Slug = "trung-quoc", Code = "CN", IsActive = true },
                    new Country { Name = "Hàn Quốc", Slug = "han-quoc", Code = "KR", IsActive = true },
                    new Country { Name = "Nhật Bản", Slug = "nhat-ban", Code = "JP", IsActive = true },
                    new Country { Name = "Thái Lan", Slug = "thai-lan", Code = "TH", IsActive = true },
                    new Country { Name = "Âu Mỹ", Slug = "au-my", Code = "US", IsActive = true },
                    new Country { Name = "Đài Loan", Slug = "dai-loan", Code = "TW", IsActive = true },
                    new Country { Name = "Hồng Kông", Slug = "hong-kong", Code = "HK", IsActive = true },
                    new Country { Name = "Ấn Độ", Slug = "an-do", Code = "IN", IsActive = true },
                    new Country { Name = "Philippines", Slug = "philippines", Code = "PH", IsActive = true },
                    new Country { Name = "Quốc gia khác", Slug = "quoc-gia-khac", Code = "OT", IsActive = true }
                };

                await context.Countries.AddRangeAsync(countries);
                await context.SaveChangesAsync();
            }

            // Seed MovieTypes
            if (!await context.MovieTypes.AnyAsync())
            {
                var movieTypes = new List<MovieType>
                {
                    new MovieType { Name = "Phim lẻ", Slug = "phim-le", Description = "Phim lẻ", IsActive = true },
                    new MovieType { Name = "Phim bộ", Slug = "phim-bo", Description = "Phim bộ", IsActive = true },
                    new MovieType { Name = "Phim chiếu rạp", Slug = "phim-chieu-rap", Description = "Phim chiếu rạp", IsActive = true },
                    new MovieType { Name = "Phim đang chiếu", Slug = "phim-dang-chieu", Description = "Phim đang chiếu", IsActive = true },
                    new MovieType { Name = "TV shows", Slug = "tv-shows", Description = "TV shows", IsActive = true },
                    new MovieType { Name = "Hoạt hình", Slug = "hoat-hinh", Description = "Phim hoạt hình", IsActive = true }
                };

                await context.MovieTypes.AddRangeAsync(movieTypes);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("Moderator"))
            {
                await roleManager.CreateAsync(new IdentityRole("Moderator"));
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }
        }

        public static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            // Seed Admin User
            if (await userManager.FindByEmailAsync("admin@example.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    DisplayName = "Admin",
                    AvatarUrl = "", // Set empty string instead of null
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
