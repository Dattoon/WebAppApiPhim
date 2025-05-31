namespace WebAppApiPhim.Models.DTOs
{
    // Dashboard DTOs
    public class AdminDashboardResponse
    {
        public AdminStatistics Statistics { get; set; } = new AdminStatistics();
        public object RecentUsers { get; set; } = new object();
        public object RecentComments { get; set; } = new object();
        public object PopularMovies { get; set; } = new object();
    }

    public class AdminStatistics
    {
        public int TotalUsers { get; set; }
        public int TotalMovies { get; set; }
        public int TotalEpisodes { get; set; }
        public int TotalComments { get; set; }
        public int TotalRatings { get; set; }
        public int TotalFavorites { get; set; }
    }

    public class SystemHealthResponse
    {
        public bool DatabaseConnection { get; set; }
        public bool ExternalApiConnection { get; set; }
        public string OverallStatus { get; set; } = "";
        public DateTime LastChecked { get; set; }
    }

    // User Management DTOs
    public class AdminUserDetailResponse
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public UserActivityStats ActivityStats { get; set; } = new UserActivityStats();
    }

    public class UserActivityStats
    {
        public int TotalFavorites { get; set; }
        public int TotalRatings { get; set; }
        public int TotalComments { get; set; }
        public int TotalWatchHistory { get; set; }
    }

    // Movie Management DTOs
    public class AdminMovieListResponse
    {
        public List<AdminMovieResponse> Movies { get; set; } = new List<AdminMovieResponse>();
        public PaginationResponse Pagination { get; set; } = new PaginationResponse();
    }

    public class AdminMovieResponse
    {
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Year { get; set; }
        public long Views { get; set; }
        public DateTime LastUpdated { get; set; }
        public int EpisodeCount { get; set; }
        public int CommentCount { get; set; }
        public int RatingCount { get; set; }
        public int FavoriteCount { get; set; }
    }

    // Comment Moderation DTOs
    public class AdminCommentListResponse
    {
        public List<AdminCommentResponse> Comments { get; set; } = new List<AdminCommentResponse>();
        public PaginationResponse Pagination { get; set; } = new PaginationResponse();
    }

    public class AdminCommentResponse
    {
        public string Id { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; } = "";
        public string UserDisplayName { get; set; } = "";
        public string MovieTitle { get; set; } = "";
        public string MovieSlug { get; set; } = "";
    }
}
