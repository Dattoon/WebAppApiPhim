using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<CachedMovie> CachedMovies { get; set; }
        public DbSet<CachedEpisode> CachedEpisodes { get; set; }
        public DbSet<EpisodeServer> EpisodeServers { get; set; }
        public DbSet<MovieStatistic> MovieStatistics { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<MovieRating> MovieRatings { get; set; }
        public DbSet<UserComment> UserComments { get; set; }
        public DbSet<UserMovie> UserMovies { get; set; }
        public DbSet<EpisodeProgress> EpisodeProgresses { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<MovieCountry> MovieCountries { get; set; }
        public DbSet<MovieType> MovieTypes { get; set; }
        public DbSet<ProductionCompany> ProductionCompanies { get; set; }
        public DbSet<StreamingPlatform> StreamingPlatforms { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<MovieGenreMapping> MovieGenreMappings { get; set; }
        public DbSet<MovieCountryMapping> MovieCountryMappings { get; set; }
        public DbSet<MovieTypeMapping> MovieTypeMappings { get; set; }
        public DbSet<MovieProductionCompany> MovieProductionCompanies { get; set; }
        public DbSet<MovieStreamingPlatform> MovieStreamingPlatforms { get; set; }
        public DbSet<MovieActor> MovieActors { get; set; }
        public DbSet<DailyView> DailyViews { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình unique constraint cho Email
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Cấu hình các mối quan hệ
            modelBuilder.Entity<CachedMovie>()
                .HasMany(m => m.Episodes)
                .WithOne(e => e.Movie)
                .HasForeignKey(e => e.MovieSlug);

            modelBuilder.Entity<CachedEpisode>()
                .HasMany(e => e.EpisodeServers)
                .WithOne(s => s.Episode)
                .HasForeignKey(s => s.EpisodeId);

            modelBuilder.Entity<UserFavorite>()
                .HasKey(uf => new { uf.UserId, uf.MovieSlug });

            modelBuilder.Entity<MovieRating>()
                .HasKey(mr => new { mr.UserId, mr.MovieSlug });

            modelBuilder.Entity<UserComment>()
                .HasKey(uc => uc.Id);

            modelBuilder.Entity<UserMovie>()
                .HasKey(um => new { um.UserId, um.MovieSlug });

            modelBuilder.Entity<EpisodeProgress>()
                .HasKey(ep => new { ep.UserId, ep.EpisodeId });

            modelBuilder.Entity<MovieGenreMapping>()
                .HasKey(mgm => new { mgm.MovieSlug, mgm.GenreId });

            modelBuilder.Entity<MovieCountryMapping>()
                .HasKey(mcm => new { mcm.MovieSlug, mcm.CountryId });

            modelBuilder.Entity<MovieTypeMapping>()
                .HasKey(mtm => new { mtm.MovieSlug, mtm.TypeId });

            modelBuilder.Entity<MovieProductionCompany>()
                .HasKey(mpc => new { mpc.MovieSlug, mpc.ProductionCompanyId });

            modelBuilder.Entity<MovieStreamingPlatform>()
                .HasKey(msp => new { msp.MovieSlug, msp.StreamingPlatformId });

            modelBuilder.Entity<MovieActor>()
                .HasKey(ma => new { ma.MovieSlug, ma.ActorId });
        }
    }
}