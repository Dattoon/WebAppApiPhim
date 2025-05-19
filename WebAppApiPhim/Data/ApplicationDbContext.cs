using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAppApiPhim.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Existing tables
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<WatchHistory> WatchHistories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    // New metadata tables
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MovieType> MovieTypes { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<MovieGenre> MovieGenres { get; set; }
    public DbSet<MovieCountry> MovieCountries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Existing configurations...

        // Configure Movie as the primary entity
        modelBuilder.Entity<Movie>()
            .HasKey(m => m.Slug);

        // Configure many-to-many relationships
        modelBuilder.Entity<MovieGenre>()
            .HasKey(mg => new { mg.MovieSlug, mg.GenreId });

        modelBuilder.Entity<MovieCountry>()
            .HasKey(mc => new { mc.MovieSlug, mc.CountryId });

        // Add unique constraints
        modelBuilder.Entity<MovieType>()
            .HasIndex(mt => mt.Name)
            .IsUnique();

        modelBuilder.Entity<Genre>()
            .HasIndex(g => g.Name)
            .IsUnique();

        modelBuilder.Entity<Country>()
            .HasIndex(c => c.Name)
            .IsUnique();
    }
}