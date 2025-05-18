using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IUserService
    {
        // Quản lý người dùng
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<ApplicationUser> GetUserByUsernameAsync(string username);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<IdentityResult> UpdateUserProfileAsync(string userId, string displayName, string avatarUrl);

        // Danh sách yêu thích
        Task<bool> AddToFavoritesAsync(string userId, string movieSlug, string movieName, string moviePosterUrl);
        Task<bool> RemoveFromFavoritesAsync(string userId, string movieSlug);
        Task<bool> IsFavoriteAsync(string userId, string movieSlug);
        Task<List<Favorite>> GetFavoritesAsync(string userId, int page = 1, int pageSize = 10);
        Task<int> GetFavoritesCountAsync(string userId);

        // Lịch sử xem
        Task<bool> AddToWatchHistoryAsync(string userId, string movieSlug, string movieName, string moviePosterUrl, string episodeSlug = null, string episodeName = null, double watchedPercentage = 0);
        Task<List<WatchHistory>> GetWatchHistoryAsync(string userId, int page = 1, int pageSize = 10);
        Task<int> GetWatchHistoryCountAsync(string userId);
        Task<bool> ClearWatchHistoryAsync(string userId);
        Task<WatchHistory> GetLastWatchedAsync(string userId, string movieSlug);

        // Bình luận
        Task<Comment> AddCommentAsync(string userId, string movieSlug, string content);
        Task<bool> UpdateCommentAsync(int commentId, string userId, string content);
        Task<bool> DeleteCommentAsync(int commentId, string userId);
        Task<List<Comment>> GetCommentsByMovieAsync(string movieSlug, int page = 1, int pageSize = 10);
        Task<int> GetCommentsCountByMovieAsync(string movieSlug);

        // Đánh giá
        Task<bool> AddOrUpdateRatingAsync(string userId, string movieSlug, int value);
        Task<double> GetAverageRatingAsync(string movieSlug);
        Task<int> GetRatingCountAsync(string movieSlug);
        Task<Rating> GetUserRatingAsync(string userId, string movieSlug);
    }
}
