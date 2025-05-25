using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Data
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>

    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CachedMovie> CachedMovies { get; set; }
        public DbSet<CachedEpisode> CachedEpisodes { get; set; }
        public DbSet<EpisodeServer> EpisodeServers { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<MovieCountry> MovieCountries { get; set; }
        public DbSet<MovieType> MovieTypes { get; set; }
        public DbSet<MovieStatistic> MovieStatistics { get; set; }
        public DbSet<DailyView> DailyViews { get; set; }
        public DbSet<EpisodeProgress> EpisodeProgresses { get; set; }
        public DbSet<ProductionCompany> ProductionCompanies { get; set; }
        public DbSet<StreamingPlatform> StreamingPlatforms { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<MovieRating> MovieRatings { get; set; }
        public DbSet<UserComment> UserComments { get; set; }
        public DbSet<UserMovie> UserMovies { get; set; }
        public DbSet<MovieGenreMapping> MovieGenreMappings { get; set; }
        public DbSet<MovieCountryMapping> MovieCountryMappings { get; set; }
        public DbSet<MovieTypeMapping> MovieTypeMappings { get; set; }
        public DbSet<MovieProductionCompany> MovieProductionCompanies { get; set; }
        public DbSet<MovieStreamingPlatform> MovieStreamingPlatforms { get; set; }
        public DbSet<MovieActor> MovieActors { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình mối quan hệ nhiều-nhiều cho MovieGenreMapping
            modelBuilder.Entity<MovieGenreMapping>()
                .HasKey(mgm => new { mgm.MovieSlug, mgm.GenreId });

            modelBuilder.Entity<MovieGenreMapping>()
                .HasOne(mgm => mgm.Movie)
                .WithMany(m => m.MovieGenreMappings)
                .HasForeignKey(mgm => mgm.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieGenreMapping>()
                .HasOne(mgm => mgm.Genre)
                .WithMany(g => g.MovieGenreMappings)
                .HasForeignKey(mgm => mgm.GenreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ nhiều-nhiều cho MovieCountryMapping
            modelBuilder.Entity<MovieCountryMapping>()
                .HasKey(mcm => new { mcm.MovieSlug, mcm.CountryId });

            modelBuilder.Entity<MovieCountryMapping>()
                .HasOne(mcm => mcm.Movie)
                .WithMany(m => m.MovieCountryMappings)
                .HasForeignKey(mcm => mcm.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieCountryMapping>()
                .HasOne(mcm => mcm.Country)
                .WithMany(c => c.MovieCountryMappings)
                .HasForeignKey(mcm => mcm.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ nhiều-nhiều cho MovieTypeMapping
            modelBuilder.Entity<MovieTypeMapping>()
                .HasKey(mtm => new { mtm.MovieSlug, mtm.TypeId });

            modelBuilder.Entity<MovieTypeMapping>()
                .HasOne(mtm => mtm.Movie)
                .WithMany(m => m.MovieTypeMappings)
                .HasForeignKey(mtm => mtm.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieTypeMapping>()
                .HasOne(mtm => mtm.MovieType)
                .WithMany(t => t.MovieTypeMappings)
                .HasForeignKey(mtm => mtm.TypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ nhiều-nhiều cho MovieProductionCompany
            modelBuilder.Entity<MovieProductionCompany>()
                .HasKey(mpc => new { mpc.MovieSlug, mpc.ProductionCompanyId });

            modelBuilder.Entity<MovieProductionCompany>()
                .HasOne(mpc => mpc.Movie)
                .WithMany(m => m.MovieProductionCompanies)
                .HasForeignKey(mpc => mpc.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieProductionCompany>()
                .HasOne(mpc => mpc.ProductionCompany)
                .WithMany(pc => pc.MovieProductionCompanies)
                .HasForeignKey(mpc => mpc.ProductionCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ nhiều-nhiều cho MovieStreamingPlatform
            modelBuilder.Entity<MovieStreamingPlatform>()
                .HasKey(msp => new { msp.MovieSlug, msp.StreamingPlatformId });

            modelBuilder.Entity<MovieStreamingPlatform>()
                .HasOne(msp => msp.Movie)
                .WithMany(m => m.MovieStreamingPlatforms)
                .HasForeignKey(msp => msp.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieStreamingPlatform>()
                .HasOne(msp => msp.StreamingPlatform)
                .WithMany(sp => sp.MovieStreamingPlatforms)
                .HasForeignKey(msp => msp.StreamingPlatformId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ nhiều-nhiều cho MovieActor
            modelBuilder.Entity<MovieActor>()
                .HasKey(ma => new { ma.MovieSlug, ma.ActorId });

            modelBuilder.Entity<MovieActor>()
                .HasOne(ma => ma.Movie)
                .WithMany(m => m.MovieActors)
                .HasForeignKey(ma => ma.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieActor>()
                .HasOne(ma => ma.Actor)
                .WithMany(a => a.MovieActors)
                .HasForeignKey(ma => ma.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ 1-1 cho MovieStatistic
            modelBuilder.Entity<MovieStatistic>()
                .HasOne(ms => ms.Movie)
                .WithOne(m => m.Statistic)
                .HasForeignKey<MovieStatistic>(ms => ms.MovieSlug)
                .OnDelete(DeleteBehavior.Cascade);

            // Thêm index cho hiệu suất
            modelBuilder.Entity<CachedMovie>()
                .HasIndex(m => m.Slug)
                .IsUnique();

            modelBuilder.Entity<CachedEpisode>()
                .HasIndex(e => new { e.MovieSlug, e.EpisodeNumber })
                .IsUnique();

            modelBuilder.Entity<UserFavorite>()
                .HasIndex(uf => new { uf.UserId, uf.MovieSlug });

            modelBuilder.Entity<MovieRating>()
                .HasIndex(mr => new { mr.UserId, mr.MovieSlug });

            modelBuilder.Entity<UserComment>()
                .HasIndex(uc => new { uc.UserId, uc.MovieSlug });

            modelBuilder.Entity<UserMovie>()
                .HasIndex(um => new { um.UserId, um.MovieSlug });

            modelBuilder.Entity<EpisodeProgress>()
                .HasIndex(ep => new { ep.UserId, ep.EpisodeId });

            // Cấu hình loại cột cho RawData
            modelBuilder.Entity<CachedMovie>()
                .Property(m => m.RawData)
                .HasColumnType("nvarchar(max)"); // Hoặc "TEXT" tùy theo DB
        }
    }
}