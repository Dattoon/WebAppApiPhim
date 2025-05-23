using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IUserService
    {
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> CreateUserAsync(ApplicationUser user, string password);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<List<ApplicationUser>> GetAllUsersAsync(int page = 1, int pageSize = 20);
        Task<bool> AddToRoleAsync(string userId, string roleName);
        Task<bool> RemoveFromRoleAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
    }
}
