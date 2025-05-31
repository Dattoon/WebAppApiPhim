using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await Task.CompletedTask;
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            // Seed Roles
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>("Admin") { Id = Guid.NewGuid() });
            }

            if (!await roleManager.RoleExistsAsync("Moderator"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>("Moderator") { Id = Guid.NewGuid() });
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>("User") { Id = Guid.NewGuid() });
            }
        }

        public static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            // Seed Admin User
            const string adminEmail = "admin@movieapi.com";
            const string adminPassword = "Admin123456"; 

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Administrator"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        public static async Task Initialize(
            IServiceProvider serviceProvider,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            await SeedAsync(context);
            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager);
        }
    }
}
