using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<ApplicationUser?> CreateUserAsync(ApplicationUser user, string password);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<List<ApplicationUser>> GetAllUsersAsync(int page = 1, int pageSize = 20);
        Task<bool> AddToRoleAsync(string userId, string roleName);
        Task<bool> RemoveFromRoleAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<(bool Success, string Token, string ErrorMessage)> RegisterAsync(string email, string password, string displayName);
        Task<(bool Success, string Token, string ErrorMessage)> LoginAsync(string email, string password);
        Task<bool> UpdateUserAsync(string userId, string displayName);
    }
}