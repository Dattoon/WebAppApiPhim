using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAppApiPhim.Models
{
    // For movie list API
    public class MovieListResponse
    {
        [JsonPropertyName("data")]
        public List<MovieItem> Data { get; set; } = new List<MovieItem>();

        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; } = new Pagination();
    }

    public class MovieItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }

        [JsonPropertyName("loai_phim")]
        public string Loai_phim { get; set; }

        [JsonPropertyName("quoc_gia")]
        public string Quoc_gia { get; set; }

        [JsonPropertyName("modified")]
        public string Modified { get; set; }

        [JsonPropertyName("modified_time")]
        public string ModifiedTime { get; set; }

        [JsonPropertyName("tmdb_id")]
        public string TmdbId { get; set; }

        // Additional fields that might be in the API response
        [JsonPropertyName("poster_url")]
        public string PosterUrl { get; set; }

        [JsonPropertyName("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonPropertyName("backdrop_url")]
        public string BackdropUrl { get; set; }
    }

    public class Pagination
    {
        [JsonPropertyName("current_page")]
        public int Current_page { get; set; }

        [JsonPropertyName("total_pages")]
        public int Total_pages { get; set; }

        [JsonPropertyName("total_items")]
        public int Total_items { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    // For movie detail API
    public class MovieDetailResponse
    {
        // The API might return the movie details directly or nested
        [JsonPropertyName("movie")]
        public MovieDetail Movie { get; set; }

        [JsonPropertyName("episodes")]
        public List<object> Episodes { get; set; } = new List<object>();

        // For direct properties if the API returns them at the root level
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("original_name")]
        public string OriginalName { get; set; }

        [JsonPropertyName("origin_name")]
        public string OriginName { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; }

        [JsonPropertyName("categories")]
        public string Categories { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("countries")]
        public string Countries { get; set; }

        [JsonPropertyName("poster_url")]
        public string PosterUrl { get; set; }

        [JsonPropertyName("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonPropertyName("backdrop_url")]
        public string BackdropUrl { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("sub_thumb")]
        public string SubThumb { get; set; }

        [JsonPropertyName("sub_poster")]
        public string SubPoster { get; set; }
    }

    public class MovieDetail
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Slug { get; set; }
        public string Year { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public string Country { get; set; }
        public string PosterUrl { get; set; }
        public string BackdropUrl { get; set; }
        public double Rating { get; set; }
        public string Director { get; set; }
        public string Actors { get; set; }
        public string Duration { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
    }

    public class Episode
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Filename { get; set; }
        public string Link { get; set; }
    }

    // View models
    public class MovieDetailViewModel
    {
        public MovieDetail Movie { get; set; }
        public List<Episode> Episodes { get; set; } = new List<Episode>();
        public bool IsFavorite { get; set; }
        public MovieWatchHistory WatchHistory { get; set; }
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
        public List<MovieItem> RelatedMovies { get; set; } = new List<MovieItem>();
    }

    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public string CreatedAt { get; set; }
        public User User { get; set; }
    }

    public class MovieWatchHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public double WatchedPercentage { get; set; }
        public string LastWatchedAt { get; set; }
        public DateTime WatchedAt { get; set; } = DateTime.Now;
    }
}