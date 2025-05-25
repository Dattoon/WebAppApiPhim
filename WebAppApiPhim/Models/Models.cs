using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApiPhim.Models
{
    // Movie Detail Response
    public class MovieDetailResponse
    {
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }

        public List<Episode> Episodes { get; set; } = new List<Episode>();

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [StringLength(1000)]
        public string PosterUrl { get; set; }

        [StringLength(1000)]
        public string ThumbUrl { get; set; }

        [StringLength(4)]
        public string Year { get; set; }

        [StringLength(50)]
        public string Duration { get; set; }

        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(100)]
        public string Director { get; set; }

        [StringLength(50)]
        public string TmdbId { get; set; }

        public long Views { get; set; }

        [StringLength(50)]
        public string Modified { get; set; }

        public List<string> Genres { get; set; } = new List<string>();

        public List<string> Countries { get; set; } = new List<string>();

        [StringLength(50)]
        public string Type { get; set; }

        [StringLength(500)]
        public string Actors { get; set; }

        [Range(0, 10)]
        public double? Rating { get; set; }

        [StringLength(1000)]
        public string? TrailerUrl { get; set; }
    }
    // Episode Model
    public class Episode
    {
        [Required]
        [StringLength(100)]
        public string ServerName { get; set; }

        [Required]
        [StringLength(100)]
        public string EpisodeName { get; set; }

        [Required]
        [StringLength(100)]
        public string EpisodeSlug { get; set; }

        [StringLength(1000)]
        public string EmbedUrl { get; set; }

        [StringLength(1000)]
        public string M3u8Url { get; set; }

        public List<EpisodeItem> Items { get; set; } = new List<EpisodeItem>();
    }

    // EpisodeItem Model
    public class EpisodeItem
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Url { get; set; }

        [StringLength(1000)]
        public string Embed { get; set; }

        [StringLength(1000)]
        public string M3u8 { get; set; }
    }

    // Tmdb Data Response
    public class TmdbDataResponse
    {
        [Required]
        [StringLength(50)]
        public string TmdbId { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [Range(0, 10)]
        public double Rating { get; set; }
    }

    public class ApiErrorResponse
    {
        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
    }

    // Actor Response
    public class ActorResponse
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public List<string> Movies { get; set; } = new List<string>();
    }

    // Movie List Response
    public class MovieListResponse
    {
        public List<MovieItem> Data { get; set; } = new List<MovieItem>();

        public Pagination Pagination { get; set; }
    }

    public class MovieItem
    {
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [StringLength(50)]
        public string TmdbId { get; set; }

        [StringLength(4)]
        public string Year { get; set; }

        [Required]
        [StringLength(1000)]
        public string PosterUrl { get; set; }

        public ModifiedData Modified { get; set; }
    }

    public class ModifiedData
    {
        [Required]
        [StringLength(50)]
        public string Time { get; set; }
    }

    public class Pagination
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int Limit { get; set; }
    }

    public class ImageResponse
    {
        public bool Success { get; set; }

        [StringLength(1000)]
        public string SubThumb { get; set; }

        [StringLength(1000)]
        public string SubPoster { get; set; }
    }

    public class ProductionData
    {
        public List<ProductionCompanyData> ProductionCompanies { get; set; } = new List<ProductionCompanyData>();
        public List<StreamingPlatformData> StreamingPlatforms { get; set; } = new List<StreamingPlatformData>();
    }

    public class ProductionCompanyData
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Logo { get; set; }

        [StringLength(10)]
        public string OriginCountry { get; set; }
    }

    public class StreamingPlatformData
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Logo { get; set; }
    }

    public class ProductionApiResponse
    {
        [Required]
        public bool Success { get; set; }

        public List<ProductionCompanyData> ProductionCompanies { get; set; } = new List<ProductionCompanyData>();

        public List<StreamingPlatformData> StreamingPlatforms { get; set; } = new List<StreamingPlatformData>();

        [StringLength(1000)]
        public string DebugInfo { get; set; }
    }

  

    // User-related models
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Key]
        public override Guid Id { get; set; } = Guid.NewGuid();

        

        [Required]
        [StringLength(100)]
        public override string Email { get; set; }

        [Required]
        public override string PasswordHash { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        public List<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public List<MovieRating> Ratings { get; set; } = new List<MovieRating>();
        public List<UserComment> Comments { get; set; } = new List<UserComment>();
        public List<UserMovie> Watchlist { get; set; } = new List<UserMovie>();
        public List<EpisodeProgress> EpisodeProgresses { get; set; } = new List<EpisodeProgress>();
    }

    public class UserFavorite
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class MovieRating
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Range(0, 10)]
        public double Rating { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class UserComment
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class UserMovie
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    // Movie-related models
    public class CachedMovie
    {
        [Key]
        [Required]
        [StringLength(100)]
        public string Slug { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [StringLength(1000)]
        public string PosterUrl { get; set; }

        [StringLength(1000)]
        public string ThumbUrl { get; set; }

        [StringLength(4)]
        public string Year { get; set; }

        [StringLength(100)]
        public string Director { get; set; }

        [StringLength(50)]
        public string Duration { get; set; }

        [StringLength(50)]
        public string Language { get; set; }

        public long Views { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public string RawData { get; set; }

        [StringLength(50)]
        public string TmdbId { get; set; }

        [StringLength(50)]
        public string? Resolution { get; set; }

        [StringLength(1000)]
        public string? TrailerUrl { get; set; }

        [Range(0, 10)]
        public double? Rating { get; set; }

        public List<CachedEpisode> Episodes { get; set; } = new List<CachedEpisode>();
        public List<MovieGenreMapping> MovieGenreMappings { get; set; } = new List<MovieGenreMapping>();
        public List<MovieCountryMapping> MovieCountryMappings { get; set; } = new List<MovieCountryMapping>();
        public List<MovieTypeMapping> MovieTypeMappings { get; set; } = new List<MovieTypeMapping>();
        public List<MovieProductionCompany> MovieProductionCompanies { get; set; } = new List<MovieProductionCompany>();
        public List<MovieStreamingPlatform> MovieStreamingPlatforms { get; set; } = new List<MovieStreamingPlatform>();
        public List<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
        public List<UserMovie> Watchlist { get; set; } = new List<UserMovie>();
        public List<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public List<MovieRating> Ratings { get; set; } = new List<MovieRating>();
        public List<UserComment> Comments { get; set; } = new List<UserComment>();
        public List<DailyView> DailyViews { get; set; } = new List<DailyView>();

        [ForeignKey("MovieStatistic")]
        public string MovieSlug { get; set; }
        public MovieStatistic Statistic { get; set; }
    }
    public class CachedEpisode
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        public int EpisodeNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Url { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public CachedMovie Movie { get; set; }
        public List<EpisodeServer> EpisodeServers { get; set; } = new List<EpisodeServer>();
        public List<EpisodeProgress> EpisodeProgresses { get; set; } = new List<EpisodeProgress>();
    }

    public class EpisodeServer
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Episode")]
        public string EpisodeId { get; set; }

        [Required]
        [StringLength(100)]
        public string ServerName { get; set; }

        [Required]
        [StringLength(1000)]
        public string ServerUrl { get; set; }

        public CachedEpisode Episode { get; set; }
    }

    public class MovieGenre
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Slug { get; set; }

        public List<MovieGenreMapping> MovieGenreMappings { get; set; } = new List<MovieGenreMapping>();
    }

    public class MovieCountry
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(10)]
        public string Code { get; set; }

        [StringLength(100)]
        public string Slug { get; set; }

        public List<MovieCountryMapping> MovieCountryMappings { get; set; } = new List<MovieCountryMapping>();
    }

    public class MovieType
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Slug { get; set; }

        public List<MovieTypeMapping> MovieTypeMappings { get; set; } = new List<MovieTypeMapping>();
    }

    public class ProductionCompany
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public List<MovieProductionCompany> MovieProductionCompanies { get; set; } = new List<MovieProductionCompany>();
    }

    public class StreamingPlatform
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Url { get; set; }

        public List<MovieStreamingPlatform> MovieStreamingPlatforms { get; set; } = new List<MovieStreamingPlatform>();
    }

    public class Actor
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public List<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }

    public class MovieStatistic
    {
        [Key]
        [StringLength(100)]
        public string MovieSlug { get; set; }

        public long Views { get; set; }

        public double AverageRating { get; set; }

        public int FavoriteCount { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public CachedMovie Movie { get; set; }
    }

    public class DailyView
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public long ViewCount { get; set; }

        public long Views { get; set; } 

        public CachedMovie Movie { get; set; }
    }

    public class EpisodeProgress
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey("Episode")]
        public string EpisodeId { get; set; }

        [Range(0, 100)]
        public double WatchedPercentage { get; set; }

        [Required]
        public DateTime LastWatched { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public CachedEpisode Episode { get; set; }
    }

    

   

   

   
   

   
    

    // View models
    public class ServerViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Url { get; set; }

        public List<ServerViewModel> Servers { get; set; } = new List<ServerViewModel>();

        public int EpisodeNumber { get; set; } // Đổi internal thành public
    }

    public class EpisodeViewModel
    {
        [Required]
        [StringLength(50)]
        public string Id { get; set; }

        [Required]
        public int EpisodeNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Url { get; set; }

        public List<ServerViewModel> Servers { get; set; } = new List<ServerViewModel>();
    }




    public class MovieGenreMapping
    {
        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [ForeignKey("Genre")]
        public string GenreId { get; set; }

        public CachedMovie Movie { get; set; }
        public MovieGenre Genre { get; set; }
    }

    public class MovieCountryMapping
    {
        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [ForeignKey("Country")]
        public string CountryId { get; set; }

        public CachedMovie Movie { get; set; }
        public MovieCountry Country { get; set; }
    }

    public class MovieTypeMapping
    {
        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [ForeignKey("MovieType")]
        public string TypeId { get; set; }

        public CachedMovie Movie { get; set; }
        public MovieType MovieType { get; set; }
    }

    public class MovieProductionCompany
    {
        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [ForeignKey("ProductionCompany")]
        public string ProductionCompanyId { get; set; }

        public CachedMovie Movie { get; set; }
        public ProductionCompany ProductionCompany { get; set; }
    }

    public class MovieStreamingPlatform
    {
        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [ForeignKey("StreamingPlatform")]
        public string StreamingPlatformId { get; set; }

        public CachedMovie Movie { get; set; }
        public StreamingPlatform StreamingPlatform { get; set; }
    }

    public class MovieActor
    {
        [Required]
        [ForeignKey("Movie")]
        public string MovieSlug { get; set; }

        [Required]
        [ForeignKey("Actor")]
        public string ActorId { get; set; }

        public CachedMovie Movie { get; set; }
        public Actor Actor { get; set; }
    }
}