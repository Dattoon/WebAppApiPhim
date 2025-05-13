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

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password)
        {
            // Kiểm tra xem username hoặc email đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
            {
                return false;
            }

            // Tạo salt và hash password
            var salt = GenerateSalt();
            var passwordHash = HashPassword(password, salt);

            // Tạo user mới
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Salt = salt,
                CreatedAt = DateTime.Now,
                IsActive = true,
                Role = "User"
            };

            // Thêm user vào database
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return null;
            }

            // Kiểm tra password
            var passwordHash = HashPassword(password, user.Salt);
            if (passwordHash != user.PasswordHash)
            {
                return null;
            }

            // Cập nhật thời gian đăng nhập
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> AddToFavoritesAsync(int userId, string movieSlug, string movieName)
        {
            // Kiểm tra xem phim đã có trong danh sách yêu thích chưa
            if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.MovieSlug == movieSlug))
            {
                return false;
            }

            // Thêm phim vào danh sách yêu thích
            var favorite = new Favorite
            {
                UserId = userId,
                MovieSlug = movieSlug,
                MovieName = movieName,
                AddedAt = DateTime.Now
            };

            await _context.Favorites.AddAsync(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveFromFavoritesAsync(int userId, string movieSlug)
        {
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == movieSlug);

            if (favorite == null)
            {
                return false;
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Favorite>> GetFavoritesAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();
        }

        public async Task<bool> AddToWatchHistoryAsync(int userId, string movieSlug, string movieName, string episodeSlug = null, double watchedPercentage = 0)
        {
            // Kiểm tra xem phim đã có trong lịch sử xem chưa
            var history = await _context.WatchHistory
                .FirstOrDefaultAsync(h => h.UserId == userId && h.MovieSlug == movieSlug && h.EpisodeSlug == episodeSlug);

            if (history != null)
            {
                // Cập nhật lịch sử xem
                history.WatchedPercentage = watchedPercentage;
                history.WatchedAt = DateTime.Now;
            }
            else
            {
                // Thêm phim vào lịch sử xem
                history = new WatchHistory
                {
                    UserId = userId,
                    MovieSlug = movieSlug,
                    MovieName = movieName,
                    EpisodeSlug = episodeSlug,
                    WatchedPercentage = watchedPercentage,
                    WatchedAt = DateTime.Now
                };

                await _context.WatchHistory.AddAsync(history);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WatchHistory>> GetWatchHistoryAsync(int userId)
        {
            return await _context.WatchHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.WatchedAt)
                .ToListAsync();
        }

        public async Task<bool> AddCommentAsync(int userId, string movieSlug, string content)
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

            return true;
        }

        public async Task<List<Comment>> GetCommentsAsync(string movieSlug)
        {
            return await _context.Comments
                .Where(c => c.MovieSlug == movieSlug)
                .OrderByDescending(c => c.CreatedAt)
                .Include(c => c.User)
                .ToListAsync();
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hashBytes = sha256.ComputeHash(saltedPasswordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
