using System.Text.Json.Serialization;

namespace WebAppApiPhim.Models.DuLieuPhim
{
    // Base response class
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    // Movie list response
    public class MovieListResponse
    {
        public List<MovieItem> Data { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; } = new Pagination();
    }

    public class MovieItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;

        [JsonPropertyName("loai_phim")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("tmdb_id")]
        public string? TmdbId { get; set; }

        [JsonPropertyName("quoc_gia")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("modified")]
        public string? Modified { get; set; }

        [JsonPropertyName("modified_time")]
        public string? ModifiedTime { get; set; }
    }

    public class Pagination
    {
        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_items")]
        public int TotalItems { get; set; }

        public int Limit { get; set; }
    }

    // Movie detail response
    public class MovieDetailResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("tmdb_type")]
        public string? TmdbType { get; set; }

        [JsonPropertyName("tmdb_id")]
        public string? TmdbId { get; set; }

        [JsonPropertyName("tmdb_season")]
        public string? TmdbSeason { get; set; }

        [JsonPropertyName("tmdb_vote_average")]
        public double TmdbVoteAverage { get; set; }

        [JsonPropertyName("tmdb_vote_count")]
        public int TmdbVoteCount { get; set; }

        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("movie_id")]
        public string? MovieId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("original_name")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("origin_name")]
        public string? OriginName { get; set; }

        public string? Description { get; set; }
        public string? Content { get; set; }

        [JsonPropertyName("thumb_url")]
        public string? ThumbUrl { get; set; }

        [JsonPropertyName("poster_url")]
        public string? PosterUrl { get; set; }

        public string? Created { get; set; }

        [JsonPropertyName("created_time")]
        public string? CreatedTime { get; set; }

        public string? Modified { get; set; }

        [JsonPropertyName("modified_time")]
        public string? ModifiedTime { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("total_episodes")]
        public int? TotalEpisodes { get; set; }

        [JsonPropertyName("episode_total")]
        public string? EpisodeTotal { get; set; }

        [JsonPropertyName("current_episode")]
        public string? CurrentEpisode { get; set; }

        [JsonPropertyName("episode_current")]
        public string? EpisodeCurrent { get; set; }

        public string? Time { get; set; }
        public string? Quality { get; set; }
        public string? Language { get; set; }
        public string? Lang { get; set; }
        public string? Director { get; set; }
        public string? Directors { get; set; }
        public string? Casts { get; set; }
        public string? Actors { get; set; }
        public string? Format { get; set; }
        public string? Genres { get; set; }
        public string? Categories { get; set; }
        public string? Year { get; set; }
        public string? Countries { get; set; }
        public string? Platforms { get; set; }

        public List<EpisodeServer> Episodes { get; set; } = new List<EpisodeServer>();

        [JsonPropertyName("sub_thumb")]
        public string? SubThumb { get; set; }

        [JsonPropertyName("sub_poster")]
        public string? SubPoster { get; set; }

        public string? Type { get; set; }
        public string? Status { get; set; }

        [JsonPropertyName("trailer_url")]
        public string? TrailerUrl { get; set; }

        public string? Notify { get; set; }
        public string? Showtimes { get; set; }
        public int View { get; set; }

        [JsonPropertyName("is_copyright")]
        public int IsCopyright { get; set; }

        [JsonPropertyName("sub_docquyen")]
        public int SubDocquyen { get; set; }

        public int Chieurap { get; set; }

        [JsonPropertyName("film_type")]
        public string? FilmType { get; set; }
    }

    public class EpisodeServer
    {
        [JsonPropertyName("server_name")]
        public string ServerName { get; set; } = string.Empty;

        public List<EpisodeItem> Items { get; set; } = new List<EpisodeItem>();
    }

    public class EpisodeItem
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Embed { get; set; } = string.Empty;
        public string M3u8 { get; set; } = string.Empty;
    }

    // TMDB response
    public class TmdbResponse
    {
        public string Status { get; set; } = string.Empty;
        public TmdbData? Data { get; set; }
    }

    public class TmdbData
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Tmdb { get; set; } = string.Empty;
        public string Imdb { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("trailer_url")]
        public string? TrailerUrl { get; set; }

        [JsonPropertyName("quoc_gia")]
        public string? Country { get; set; }
    }

    // Actor response
    public class ActorResponse
    {
        public bool Success { get; set; }
        public List<Actor> Actors { get; set; } = new List<Actor>();
        public List<EpisodeInfo> Episodes { get; set; } = new List<EpisodeInfo>();

        [JsonPropertyName("related_images")]
        public List<string> RelatedImages { get; set; } = new List<string>();

        public Rating? Rating { get; set; }

        [JsonPropertyName("age_rating")]
        public string? AgeRating { get; set; }

        [JsonPropertyName("debug_info")]
        public string? DebugInfo { get; set; }
    }

    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Character { get; set; } = string.Empty;
    }

    public class EpisodeInfo
    {
        [JsonPropertyName("episode_number")]
        public int EpisodeNumber { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    public class Rating
    {
        public double Average { get; set; }
        public int Count { get; set; }
    }

    // Production company response
    public class ProductionResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("production_companies")]
        public List<ProductionCompany> ProductionCompanies { get; set; } = new List<ProductionCompany>();

        [JsonPropertyName("streaming_platforms")]
        public List<StreamingPlatform> StreamingPlatforms { get; set; } = new List<StreamingPlatform>();

        [JsonPropertyName("debug_info")]
        public string? DebugInfo { get; set; }
    }

    public class ProductionCompany
    {
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }

        [JsonPropertyName("origin_country")]
        public string? OriginCountry { get; set; }
    }

    public class StreamingPlatform
    {
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }
    }

    // Image response
    public class ImageResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("sub_thumb")]
        public string? SubThumb { get; set; }

        [JsonPropertyName("sub_poster")]
        public string? SubPoster { get; set; }
    }
}