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

        public DbSet<CachedMovie> CachedMovies { get; set; }
        public DbSet<CachedEpisode> CachedEpisodes { get; set; }
        public DbSet<MovieStatistic> MovieStatistics { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<MovieType> MovieTypes { get; set; }
        public DbSet<EpisodeProgress> EpisodeProgresses { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<UserComment> UserComments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // CachedMovie configuration
            builder.Entity<CachedMovie>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Year);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Genres);
                entity.HasIndex(e => e.Country);

                entity.HasOne(e => e.Statistic)
                    .WithOne(s => s.Movie)
                    .HasForeignKey<MovieStatistic>(s => s.MovieSlug)
                    .HasPrincipalKey<CachedMovie>(m => m.Slug)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CachedEpisode configuration
            builder.Entity<CachedEpisode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.MovieSlug, e.EpisodeSlug }).IsUnique();

                entity.HasOne(e => e.Movie)
                    .WithMany(m => m.Episodes)
                    .HasForeignKey(e => e.MovieSlug)
                    .HasPrincipalKey(m => m.Slug)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MovieStatistic configuration
            builder.Entity<MovieStatistic>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MovieSlug).IsUnique();
                entity.HasIndex(e => e.ViewCount);
                entity.HasIndex(e => e.AverageRating);
            });

            // EpisodeProgress configuration
            builder.Entity<EpisodeProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.MovieSlug, e.EpisodeSlug }).IsUnique();
            });

            // UserFavorite configuration
            builder.Entity<UserFavorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.MovieSlug }).IsUnique();
            });

            // UserRating configuration
            builder.Entity<UserRating>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.MovieSlug }).IsUnique();
            });

            // UserComment configuration
            builder.Entity<UserComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MovieSlug);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Genre configuration
            builder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Slug).IsUnique();
            });

            // Country configuration
            builder.Entity<Country>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // MovieType configuration
            builder.Entity<MovieType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Slug).IsUnique();
            });
        }
    }
}
