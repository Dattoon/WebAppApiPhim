using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<WatchHistory> GetWatchHistoryAsync(int userId, string movieSlug)
        {
            return await _context.WatchHistories
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieSlug == movieSlug);
        }

        public async Task<List<WatchHistory>> GetUserWatchHistoryAsync(int userId, int limit = 10)
        {
            return await _context.WatchHistories
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.WatchedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<MovieWatchHistory> UpdateWatchProgressAsync(int userId, string movieSlug, string movieName, double percentage)
        {
            var watchHistory = await _context.WatchHistories
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieSlug == movieSlug);

            if (watchHistory == null)
            {
                watchHistory = new WatchHistory
                {
                    UserId = userId,
                    MovieSlug = movieSlug,
                    MovieName = movieName,
                    WatchedPercentage = percentage,
                    WatchedAt = DateTime.Now
                };
                _context.WatchHistories.Add(watchHistory);
            }
            else
            {
                watchHistory.WatchedPercentage = percentage;
                watchHistory.WatchedAt = DateTime.Now;
                _context.WatchHistories.Update(watchHistory);
            }

            await _context.SaveChangesAsync();

            return new MovieWatchHistory
            {
                Id = watchHistory.Id,
                UserId = watchHistory.UserId,
                MovieSlug = watchHistory.MovieSlug,
                MovieName = watchHistory.MovieName,
                WatchedPercentage = watchHistory.WatchedPercentage,
                LastWatchedAt = watchHistory.WatchedAt.ToString("dd/MM/yyyy HH:mm"),
                WatchedAt = watchHistory.WatchedAt
            };
        }

        public async Task<Favorite> ToggleFavoriteAsync(int userId, string movieSlug, string movieName)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);

            if (favorite == null)
            {
                favorite = new Favorite
                {
                    UserId = userId,
                    MovieSlug = movieSlug,
                    MovieName = movieName,
                    AddedAt = DateTime.Now
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return favorite;
            }
            else
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return null;
            }
        }

        public async Task<bool> IsFavoriteAsync(int userId, string movieSlug)
        {
            return await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);
        }

        public async Task<List<Favorite>> GetUserFavoritesAsync(int userId, int limit = 10)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<MovieWatchHistory> GetMovieWatchHistoryAsync(int userId, string movieSlug)
        {
            var watchHistory = await _context.WatchHistories
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieSlug == movieSlug);

            if (watchHistory == null)
                return null;

            return new MovieWatchHistory
            {
                Id = watchHistory.Id,
                UserId = watchHistory.UserId,
                MovieSlug = watchHistory.MovieSlug,
                MovieName = watchHistory.MovieName,
                WatchedPercentage = watchHistory.WatchedPercentage,
                LastWatchedAt = watchHistory.WatchedAt.ToString("dd/MM/yyyy HH:mm"),
                WatchedAt = watchHistory.WatchedAt
            };
        }

        public async Task<Comment> AddCommentAsync(int userId, string movieSlug, string content)
        {
            var comment = new Comment
            {
                UserId = userId,
                MovieSlug = movieSlug,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<List<CommentViewModel>> GetMovieCommentsAsync(string movieSlug, int limit = 20)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.MovieSlug == movieSlug)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return comments.Select(c => new CommentViewModel
            {
                Id = c.Id,
                Username = c.User.Username,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                User = c.User
            }).ToList();
        }

        public async Task<User> RegisterUserAsync(string username, string email, string password)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return null;
            }

            // Generate salt and hash password
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Salt = Convert.ToBase64String(salt),
                CreatedAt = DateTime.Now,
                IsActive = true,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return null;

            var salt = Convert.FromBase64String(user.Salt);
            var passwordHash = HashPassword(password, salt);

            if (user.PasswordHash != passwordHash)
                return null;

            // Update last login
            user.LastLoginAt = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private byte[] GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private string HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }
    }

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