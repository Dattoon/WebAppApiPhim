using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    // User Management ViewModels
    public class AdminUserListViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new List<AdminUserViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminUserViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public string BanReason { get; set; }
    }

    public class UserActivitySummaryViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public int WatchHistoryCount { get; set; }
        public int FavoritesCount { get; set; }
        public int RatingsCount { get; set; }
        public int CommentsCount { get; set; }
        public List<RecentActivityViewModel> RecentActivity { get; set; } = new List<RecentActivityViewModel>();
    }

    public class RecentActivityViewModel
    {
        public string Type { get; set; }
        public string MovieSlug { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Movie Management ViewModels
    public class AdminMovieListViewModel
    {
        public List<AdminMovieViewModel> Movies { get; set; } = new List<AdminMovieViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminMovieViewModel
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public long Views { get; set; }
        public string PosterUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        public int EpisodeCount { get; set; }
        public int CommentsCount { get; set; }
        public int FavoritesCount { get; set; }
    }

    // Comment Management ViewModels
    public class AdminCommentListViewModel
    {
        public List<AdminCommentViewModel> Comments { get; set; } = new List<AdminCommentViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminCommentViewModel
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public string UserName { get; set; }
        public string MovieTitle { get; set; }
        public string MovieSlug { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFlagged { get; set; }
    }

    // Dashboard ViewModels
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public long TotalViews { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsersThisWeek { get; set; }
        public int CommentsThisWeek { get; set; }
        public List<TopMovieViewModel> TopMovies { get; set; } = new List<TopMovieViewModel>();
        public List<DailyViewsViewModel> DailyViews { get; set; } = new List<DailyViewsViewModel>();
    }

    public class TopMovieViewModel
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public long Views { get; set; }
        public string PosterUrl { get; set; }
    }

    public class DailyViewsViewModel
    {
        public DateTime Date { get; set; }
        public long Views { get; set; }
    }

    // Reports ViewModels
    public class AdminReportsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailyCountViewModel> UserRegistrations { get; set; } = new List<DailyCountViewModel>();
        public List<DailyCountViewModel> MovieWatching { get; set; } = new List<DailyCountViewModel>();
        public List<GenrePopularityViewModel> PopularGenres { get; set; } = new List<GenrePopularityViewModel>();
    }

    public class DailyCountViewModel
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class GenrePopularityViewModel
    {
        public string GenreName { get; set; }
        public int Count { get; set; }
    }

    // System Management ViewModels
    public class SystemStatusViewModel
    {
        public bool DatabaseConnected { get; set; }
        public bool ApiConnected { get; set; }
        public bool CacheConnected { get; set; }
        public DateTime LastSync { get; set; }
        public long DatabaseSize { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class BackupInfoViewModel
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }
}
