using System.Collections.Generic;

namespace WebAppApiPhim.Models
{
    // View models for frontend
    public class ServerViewModel
    {
        public string Name { get; set; }
        public List<EpisodeViewModel> Episodes { get; set; } = new List<EpisodeViewModel>();
    }

    public class EpisodeViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string EmbedUrl { get; set; }
        public string M3u8Url { get; set; }
        public bool IsWatched { get; set; }
        public double WatchedPercentage { get; set; }
    }

  
    // Response models for API
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasPreviousPage => CurrentPage > 1;
    }

    // Movie detail view model for frontend
    public class MovieDetailViewModel
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Description { get; set; }
        public string Year { get; set; }
        public string ThumbnailUrl { get; set; } 
        public string PosterUrl { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public string Genres { get; set; }
        public string Director { get; set; }
        public string Actors { get; set; }
        public string Duration { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
        public int ViewCount { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public bool IsFavorite { get; set; }
        public double? UserRating { get; set; }
        public List<ServerViewModel> Servers { get; set; } = new List<ServerViewModel>();
    }

    // Movie list item view model for frontend
    public class MovieListItemViewModel
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Year { get; set; }
        public string ThumbUrl { get; set; }
        public string PosterUrl { get; set; }
        public string Type { get; set; }
        public string Quality { get; set; }
        public int ViewCount { get; set; }
        public double AverageRating { get; set; }
        public bool IsFavorite { get; set; }
        public double? WatchedPercentage { get; set; }
    }

    // User profile view model
    public class UserProfileViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public int FavoriteCount { get; set; }
        public int RatingCount { get; set; }
        public int CommentCount { get; set; }
    }

    // Statistics view model
    public class StatisticsViewModel
    {
        public int TotalMovies { get; set; }
        public int TotalEpisodes { get; set; }
        public int TotalUsers { get; set; }
        public int TotalViews { get; set; }
        public int TodayViews { get; set; }
        public List<MovieListItemViewModel> PopularMovies { get; set; } = new List<MovieListItemViewModel>();
        public List<MovieListItemViewModel> RecentMovies { get; set; } = new List<MovieListItemViewModel>();
    }
}
