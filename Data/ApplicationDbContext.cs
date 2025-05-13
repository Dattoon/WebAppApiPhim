using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<WatchHistory> WatchHistories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình các mối quan hệ và ràng buộc
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WatchHistory>()
                .HasOne(h => h.User)
                .WithMany(u => u.WatchHistories)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Đảm bảo mỗi người dùng chỉ có thể đánh giá một phim một lần
            modelBuilder.Entity<Rating>()
                .HasIndex(r => new { r.UserId, r.MovieSlug })
                .IsUnique();

            // Đảm bảo mỗi người dùng chỉ có thể thêm một phim vào danh sách yêu thích một lần
            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.MovieSlug })
                .IsUnique();
        }
    }
}
