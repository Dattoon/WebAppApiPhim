using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<WatchHistory> GetWatchHistoryAsync(int userId, string movieSlug);
        Task<List<WatchHistory>> GetUserWatchHistoryAsync(int userId, int limit = 10);
        Task<MovieWatchHistory> UpdateWatchProgressAsync(int userId, string movieSlug, string movieName, double percentage);
        Task<Favorite> ToggleFavoriteAsync(int userId, string movieSlug, string movieName);
        Task<bool> IsFavoriteAsync(int userId, string movieSlug);
        Task<List<Favorite>> GetUserFavoritesAsync(int userId, int limit = 10);
        Task<MovieWatchHistory> GetMovieWatchHistoryAsync(int userId, string movieSlug);
        Task<Comment> AddCommentAsync(int userId, string movieSlug, string content);
        Task<List<CommentViewModel>> GetMovieCommentsAsync(string movieSlug, int limit = 20);
        Task<User> RegisterUserAsync(string username, string email, string password);
        Task<User> AuthenticateAsync(string email, string password);
    }
}