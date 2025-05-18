using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Quản lý người dùng
        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser> GetUserByUsernameAsync(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityResult> UpdateUserProfileAsync(string userId, string displayName, string avatarUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Không tìm thấy người dùng." });
            }

            user.DisplayName = displayName;
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
            }

            return await _userManager.UpdateAsync(user);
        }

        // Danh sách yêu thích
        public async Task<bool> AddToFavoritesAsync(string userId, string movieSlug, string movieName, string moviePosterUrl)
        {
            // Kiểm tra xem phim đã có trong danh sách yêu thích chưa
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);

            if (existingFavorite != null)
            {
                return true; // Đã có trong danh sách yêu thích
            }

            // Thêm phim vào danh sách yêu thích
            var favorite = new Favorite
            {
                UserId = userId,
                MovieSlug = movieSlug,
                MovieName = movieName,
                MoviePosterUrl = moviePosterUrl,
                AddedAt = DateTime.Now
            };

            await _context.Favorites.AddAsync(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveFromFavoritesAsync(string userId, string movieSlug)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);

            if (favorite == null)
            {
                return false;
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsFavoriteAsync(string userId, string movieSlug)
        {
            return await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);
        }

        public async Task<List<Favorite>> GetFavoritesAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetFavoritesCountAsync(string userId)
        {
            return await _context.Favorites
                .CountAsync(f => f.UserId == userId);
        }

        // Lịch sử xem
        public async Task<bool> AddToWatchHistoryAsync(string userId, string movieSlug, string movieName, string moviePosterUrl, string episodeSlug = null, string episodeName = null, double watchedPercentage = 0)
        {
            // Kiểm tra xem phim đã có trong lịch sử xem chưa
            var history = await _context.WatchHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.MovieSlug == movieSlug && h.EpisodeSlug == episodeSlug);

            if (history != null)
            {
                // Cập nhật lịch sử xem
                history.WatchedPercentage = watchedPercentage;
                history.WatchedAt = DateTime.Now;
                _context.WatchHistories.Update(history);
            }
            else
            {
                // Thêm phim vào lịch sử xem
                history = new WatchHistory
                {
                    UserId = userId,
                    MovieSlug = movieSlug,
                    MovieName = movieName,
                    MoviePosterUrl = moviePosterUrl,
                    EpisodeSlug = episodeSlug,
                    EpisodeName = episodeName,
                    WatchedPercentage = watchedPercentage,
                    WatchedAt = DateTime.Now
                };

                await _context.WatchHistories.AddAsync(history);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WatchHistory>> GetWatchHistoryAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.WatchHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.WatchedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetWatchHistoryCountAsync(string userId)
        {
            return await _context.WatchHistories
                .CountAsync(h => h.UserId == userId);
        }

        public async Task<bool> ClearWatchHistoryAsync(string userId)
        {
            var histories = await _context.WatchHistories
                .Where(h => h.UserId == userId)
                .ToListAsync();

            _context.WatchHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<WatchHistory> GetLastWatchedAsync(string userId, string movieSlug)
        {
            return await _context.WatchHistories
                .Where(h => h.UserId == userId && h.MovieSlug == movieSlug)
                .OrderByDescending(h => h.WatchedAt)
                .FirstOrDefaultAsync();
        }

        // Bình luận
        public async Task<Comment> AddCommentAsync(string userId, string movieSlug, string content)
        {
            var comment = new Comment
            {
                UserId = userId,
                MovieSlug = movieSlug,
                Content = content,
                CreatedAt = DateTime.Now
            };

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            // Lấy thông tin người dùng để trả về đầy đủ
            comment.User = await _userManager.FindByIdAsync(userId);

            return comment;
        }

        public async Task<bool> UpdateCommentAsync(int commentId, string userId, string content)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

            if (comment == null)
            {
                return false;
            }

            comment.Content = content;
            comment.UpdatedAt = DateTime.Now;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, string userId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

            if (comment == null)
            {
                return false;
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Comment>> GetCommentsByMovieAsync(string movieSlug, int page = 1, int pageSize = 10)
        {
            return await _context.Comments
                .Where(c => c.MovieSlug == movieSlug)
                .OrderByDescending(c => c.CreatedAt)
                .Include(c => c.User)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCommentsCountByMovieAsync(string movieSlug)
        {
            return await _context.Comments
                .CountAsync(c => c.MovieSlug == movieSlug);
        }

        // Đánh giá
        public async Task<bool> AddOrUpdateRatingAsync(string userId, string movieSlug, int value)
        {
            if (value < 1 || value > 10)
            {
                return false;
            }

            var rating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieSlug == movieSlug);

            if (rating != null)
            {
                rating.Value = value;
                rating.UpdatedAt = DateTime.Now;
                _context.Ratings.Update(rating);
            }
            else
            {
                rating = new Rating
                {
                    UserId = userId,
                    MovieSlug = movieSlug,
                    Value = value,
                    CreatedAt = DateTime.Now
                };

                await _context.Ratings.AddAsync(rating);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<double> GetAverageRatingAsync(string movieSlug)
        {
            var ratings = await _context.Ratings
                .Where(r => r.MovieSlug == movieSlug)
                .ToListAsync();

            if (!ratings.Any())
            {
                return 0;
            }

            return ratings.Average(r => r.Value);
        }

        public async Task<int> GetRatingCountAsync(string movieSlug)
        {
            return await _context.Ratings
                .CountAsync(r => r.MovieSlug == movieSlug);
        }

        public async Task<Rating> GetUserRatingAsync(string userId, string movieSlug)
        {
            return await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieSlug == movieSlug);
        }
    }
}
