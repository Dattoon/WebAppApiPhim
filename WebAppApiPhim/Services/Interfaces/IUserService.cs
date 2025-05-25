using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IUserService
    {
        Task<(bool Success, string Token, string ErrorMessage)> RegisterAsync(string email, string password, string displayName);
        Task<(bool Success, string Token, string ErrorMessage)> LoginAsync(string email, string password);
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<bool> UpdateUserAsync(string userId, string displayName);
        Task<bool> DeleteUserAsync(string userId);
    }
}