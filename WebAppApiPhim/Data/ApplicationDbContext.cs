using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

namespace WebAppApiPhim
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Các bảng người dùng
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<WatchHistory> WatchHistories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<EpisodeProgress> EpisodeProgresses { get; set; }

        // Các bảng metadata
        public DbSet<MovieType> MovieTypes { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Genre> Genres { get; set; }

        // Các bảng phim
        public DbSet<Movie> Movies { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<MovieCountry> MovieCountries { get; set; }

        // Các bảng cache
        public DbSet<CachedMovie> CachedMovies { get; set; }
        public DbSet<CachedEpisode> CachedEpisodes { get; set; }

        // Các bảng khác
        public DbSet<FeaturedMovie> FeaturedMovies { get; set; }
        public DbSet<MovieStatistic> MovieStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình Movie
            builder.Entity<Movie>()
                .HasKey(m => m.Slug);

            // Cấu hình MovieGenre
            builder.Entity<MovieGenre>()
                .HasKey(mg => new { mg.MovieSlug, mg.GenreId });

            builder.Entity<MovieGenre>()
                .HasOne(mg => mg.Movie)
                .WithMany(m => m.MovieGenres)
                .HasForeignKey(mg => mg.MovieSlug);

            builder.Entity<MovieGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MovieGenres)
                .HasForeignKey(mg => mg.GenreId);

            // Cấu hình MovieCountry
            builder.Entity<MovieCountry>()
                .HasKey(mc => new { mc.MovieSlug, mc.CountryId });

            builder.Entity<MovieCountry>()
                .HasOne(mc => mc.Movie)
                .WithMany(m => m.MovieCountries)
                .HasForeignKey(mc => mc.MovieSlug);

            builder.Entity<MovieCountry>()
                .HasOne(mc => mc.Country)
                .WithMany(c => c.MovieCountries)
                .HasForeignKey(mc => mc.CountryId);

            // Cấu hình các mối quan hệ và chỉ mục khác

            // Favorites
            builder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.MovieSlug })
                .IsUnique();

            // WatchHistory
            builder.Entity<WatchHistory>()
                .HasIndex(w => w.UserId);

            builder.Entity<WatchHistory>()
                .HasIndex(w => w.MovieSlug);

            builder.Entity<WatchHistory>()
                .HasIndex(w => w.WatchedAt);

            // Comments
            builder.Entity<Comment>()
                .HasIndex(c => c.UserId);

            builder.Entity<Comment>()
                .HasIndex(c => c.MovieSlug);

            builder.Entity<Comment>()
                .HasIndex(c => c.CreatedAt);

            // Ratings
            builder.Entity<Rating>()
                .HasIndex(r => new { r.UserId, r.MovieSlug })
                .IsUnique();

            // CachedMovies
            builder.Entity<CachedMovie>()
                .HasIndex(m => m.Slug)
                .IsUnique();

            // CachedEpisodes
            builder.Entity<CachedEpisode>()
                .HasIndex(e => new { e.MovieSlug, e.EpisodeSlug });

            // EpisodeProgresses
            builder.Entity<EpisodeProgress>()
                .HasIndex(p => new { p.UserId, p.MovieSlug, p.EpisodeSlug })
                .IsUnique();

            // FeaturedMovies
            builder.Entity<FeaturedMovie>()
                .HasIndex(f => new { f.Category, f.DisplayOrder });

            // MovieStatistics
            builder.Entity<MovieStatistic>()
                .HasIndex(s => s.MovieSlug)
                .IsUnique();

            // UserActivities
            builder.Entity<UserActivity>()
                .HasIndex(a => a.UserId);
        }
    }
}