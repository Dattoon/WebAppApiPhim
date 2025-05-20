using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Models.ViewModels
{
    // Home page view models
    public class HomeViewModel
    {
        public List<MovieItem> LatestMovies { get; set; } = new List<MovieItem>();
        public List<MovieItem> TrendingMovies { get; set; } = new List<MovieItem>();
        public List<MovieItem> FeaturedMovies { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
    }

    // Search view models
    public class SearchViewModel
    {
        public string Query { get; set; }
        public List<MovieItem> Movies { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
        public int CurrentPage { get; set; } = 1;
    }

    // Filter view models
    public class FilterViewModel
    {
        public string Type { get; set; }
        public string Genre { get; set; }
        public string Country { get; set; }
        public string Year { get; set; }
        public List<MovieItem> Movies { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
        public int CurrentPage { get; set; } = 1;

        // Danh sách các bộ lọc
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Countries { get; set; } = new List<string>();
        public List<string> Years { get; set; } = new List<string>();
        public List<string> MovieTypes { get; set; } = new List<string>();
    }

    // Streaming view models
    public class StreamingViewModel
    {
        public CachedMovie Movie { get; set; }
        public List<ServerViewModel> Servers { get; set; } = new List<ServerViewModel>();
        public EpisodeViewModel CurrentEpisode { get; set; }
        public double CurrentTime { get; set; }
        public bool IsFavorite { get; set; }
        public double? UserRating { get; set; }
        public List<MovieItem> RelatedMovies { get; set; } = new List<MovieItem>();
    }

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

    // Dashboard view models
    public class DashboardViewModel
    {
        public List<MovieItem> ContinueWatching { get; set; } = new List<MovieItem>();
        public List<MovieItem> Favorites { get; set; } = new List<MovieItem>();
        public List<MovieItem> Recommended { get; set; } = new List<MovieItem>();
        public List<MovieItem> Trending { get; set; } = new List<MovieItem>();
        public List<MovieItem> Latest { get; set; } = new List<MovieItem>();
    }

    // Admin view models
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int TotalMovies { get; set; }
        public int TotalViews { get; set; }
        public int ViewsToday { get; set; }
        public List<UserActivity> RecentActivities { get; set; } = new List<UserActivity>();
    }

    // Pagination view model
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public object RouteValues { get; set; }
    }

    // Comment view model
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Error view model
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}