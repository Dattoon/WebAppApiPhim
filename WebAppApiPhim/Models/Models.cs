using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    // Movie Detail Response
    public class MovieDetailResponse
    {
        public string? Slug { get; set; }
        public List<Episode> Episodes { get; set; } // Sử dụng List<Episode> làm cấu trúc chính
        public string Title { get; set; }
        public string Description { get; set; }
        public string PosterUrl { get; set; }
        public string ThumbUrl { get; set; }
        public string Year { get; set; }
        public string Duration { get; set; }
        public string Language { get; set; }
        public string Director { get; set; }
        public string TmdbId { get; set; }
        public long View { get; set; }
        public string Modified { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Countries { get; set; }
        public string Type { get; set; }
        public string Actors { get; set; }
        public double? Rating { get; set; } 
        public string? TrailerUrl { get; set; }
    }

    // Episode Model
    public class Episode
    {
        public string ServerName { get; set; }
        public string EpisodeName { get; set; }
        public string EpisodeSlug { get; set; }
        public string EmbedUrl { get; set; }
        public string M3u8Url { get; set; }
        public List<EpisodeItem> Items { get; set; } // Danh sách các item (tập phim) trong mỗi server
    }

    // EpisodeItem Model
    public class EpisodeItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Embed { get; set; }
        public string M3u8 { get; set; }
    }

    // EpisodeData (có thể loại bỏ nếu không cần thiết)
   // public class EpisodeData
   // {
     //   public string ServerName { get; set; }
     //   public List<EpisodeItem> Items { get; set; }
   // }

    // Tmdb Data Response
    public class TmdbDataResponse
    {
        public string TmdbId { get; set; }
        public string Title { get; set; }
        public double Rating { get; set; }
    }
    public class ApiErrorResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }
    // Actor Response
    public class ActorResponse
    {
        public string Name { get; set; }
        public List<string> Movies { get; set; }
    }

    // Movie List Response
    public class MovieListResponse
    {
        public List<MovieItem> Data { get; set; }
        public Pagination Pagination { get; set; }
    }

    public class MovieItem
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string TmdbId { get; set; }
        public string Year { get; set; }
        public string PosterUrl { get; set; }
        public ModifiedData Modified { get; set; }
    }

    public class ModifiedData
    {
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
        public string SubThumb { get; set; }
        public string SubPoster { get; set; }
    }

    public class ProductionData
    {
        public List<ProductionCompanyData> ProductionCompanies { get; set; }
        public List<StreamingPlatformData> StreamingPlatforms { get; set; }
    }

    public class ProductionCompanyData
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public string OriginCountry { get; set; }
    }

    public class StreamingPlatformData
    {
        public string Name { get; set; }
        public string Logo { get; set; }
    }

    public class ProductionApiResponse
    {
        internal List<ProductionCompanyData>? ProductionCompanies;
        internal List<StreamingPlatformData>? StreamingPlatforms;

        public string Status { get; set; }
        public ProductionApiData Data { get; set; }
    }

    public class ProductionApiData
    {
        public List<ProductionCompanyData> ProductionCompanies { get; set; }
        public List<StreamingPlatformData> StreamingPlatforms { get; set; }
    }

    // User-related models
    public class ApplicationUser : IdentityUser
    {
        public string Id { get; set; }
        [StringLength(50)]
        public string Username { get; set; }
        [StringLength(100)]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? DisplayName { get; set; }
        public List<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public List<MovieRating> Ratings { get; set; } = new List<MovieRating>();
        public List<UserComment> Comments { get; set; } = new List<UserComment>();
        public List<UserMovie> Watchlist { get; set; } = new List<UserMovie>();
        public List<EpisodeProgress> EpisodeProgresses { get; set; } = new List<EpisodeProgress>();
    }

    public class UserFavorite
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public DateTime AddedAt { get; set; }
        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class MovieRating
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public double Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class UserComment
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        [StringLength(1000)]
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class UserMovie
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public DateTime AddedAt { get; set; }
        public ApplicationUser User { get; set; }
        public CachedMovie Movie { get; set; }
    }

    // Movie-related models
    public class CachedMovie
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PosterUrl { get; set; }
        public string ThumbUrl { get; set; }
        public string Year { get; set; }
        public string Director { get; set; }
        public string Duration { get; set; }
        public string Language { get; set; }
        public long Views { get; set; }
        public DateTime LastUpdated { get; set; }
        public string RawData { get; set; }
        public string TmdbId { get; set; }
       
        public string? Resolution { get; set; }
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
        public MovieStatistic Statistic { get; set; }
        public List<DailyView> DailyViews { get; set; } = new List<DailyView>();
        public int ViewCount { get; internal set; }
        public object? TrailerUrl { get; internal set; }
    }

    public class CachedEpisode
    {
        public string Id { get; set; }
        public string MovieSlug { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CachedMovie Movie { get; set; }
        public List<EpisodeServer> EpisodeServers { get; set; } = new List<EpisodeServer>();
        public List<EpisodeProgress> EpisodeProgresses { get; set; } = new List<EpisodeProgress>();
    }

    public class EpisodeServer
    {
        public string Id { get; set; }
        public string EpisodeId { get; set; }
        public string ServerName { get; set; }
        public string ServerUrl { get; set; }
        public CachedEpisode Episode { get; set; }
    }

    public class MovieGenre
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MovieGenreMapping> MovieGenreMappings { get; set; } = new List<MovieGenreMapping>();
    }

    public class MovieCountry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MovieCountryMapping> MovieCountryMappings { get; set; } = new List<MovieCountryMapping>();
    }

    public class MovieType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MovieTypeMapping> MovieTypeMappings { get; set; } = new List<MovieTypeMapping>();
    }

    public class MovieStatistic
    {
        public string MovieSlug { get; set; }
        public long Views { get; set; }
        public double AverageRating { get; set; }
        public int FavoriteCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public CachedMovie Movie { get; set; }
    }

    public class DailyView
    {
        public string Id { get; set; }
        public string MovieSlug { get; set; }
        public DateTime Date { get; set; }
        public int ViewCount { get; set; }
        public CachedMovie Movie { get; set; }
        public int Views { get; internal set; }
    }

    public class EpisodeProgress
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string EpisodeId { get; set; }
        public double WatchedPercentage { get; set; }
        public DateTime LastWatched { get; set; }
        public ApplicationUser User { get; set; }
        public CachedEpisode Episode { get; set; }
    }

    public class ProductionCompany
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MovieProductionCompany> MovieProductionCompanies { get; set; } = new List<MovieProductionCompany>();
    }

    public class StreamingPlatform
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public List<MovieStreamingPlatform> MovieStreamingPlatforms { get; set; } = new List<MovieStreamingPlatform>();
    }

    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }

    // Mapping models
    public class MovieGenreMapping
    {
        public string MovieSlug { get; set; }
        public int GenreId { get; set; }
        public CachedMovie Movie { get; set; }
        public MovieGenre Genre { get; set; }
    }

    public class MovieCountryMapping
    {
        public string MovieSlug { get; set; }
        public int CountryId { get; set; }
        public CachedMovie Movie { get; set; }
        public MovieCountry Country { get; set; }
    }

    public class MovieTypeMapping
    {
        public string MovieSlug { get; set; }
        public int TypeId { get; set; }
        public CachedMovie Movie { get; set; }
        public MovieType MovieType { get; set; }
    }

    public class MovieProductionCompany
    {
        public string MovieSlug { get; set; }
        public int ProductionCompanyId { get; set; }
        public CachedMovie Movie { get; set; }
        public ProductionCompany ProductionCompany { get; set; }
    }

    public class MovieStreamingPlatform
    {
        public string MovieSlug { get; set; }
        public int StreamingPlatformId { get; set; }
        public CachedMovie Movie { get; set; }
        public StreamingPlatform StreamingPlatform { get; set; }
    }

    public class MovieActor
    {
        public string MovieSlug { get; set; }
        public int ActorId { get; set; }
        public CachedMovie Movie { get; set; }
        public Actor Actor { get; set; }
    }

    // View models
    public class ServerViewModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public List<ServerViewModel> Servers { get; set; } = new List<ServerViewModel>();
        public int EpisodeNumber { get; internal set; }
    }

    public class EpisodeViewModel
    {
        public string Id { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public List<ServerViewModel> Servers { get; set; } = new List<ServerViewModel>();
    }



   


}