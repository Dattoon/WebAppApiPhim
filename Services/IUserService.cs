using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> RegisterUserAsync(string username, string email, string password);
        Task<User> AuthenticateAsync(string username, string password);
        Task<bool> AddToFavoritesAsync(int userId, string movieSlug, string movieName);
        Task<bool> RemoveFromFavoritesAsync(int userId, string movieSlug);
        Task<List<Favorite>> GetFavoritesAsync(int userId);
        Task<bool> AddToWatchHistoryAsync(int userId, string movieSlug, string movieName, string episodeSlug = null, double watchedPercentage = 0);
        Task<List<WatchHistory>> GetWatchHistoryAsync(int userId);
        Task<bool> AddCommentAsync(int userId, string movieSlug, string content);
        Task<List<Comment>> GetCommentsAsync(string movieSlug);
    }
}
