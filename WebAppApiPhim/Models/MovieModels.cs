using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebAppApiPhim.Models
{
    // Base movie information shared across different models
    public class MovieBase
    {
        [Required]
        [StringLength(500)]
        public string Slug { get; set; }

        [Required]
        [StringLength(500)]
        public string Name { get; set; }

        [StringLength(500)]
        public string OriginalName { get; set; }

        [StringLength(10)]
        public string Year { get; set; }

        [StringLength(1000)]
        public string ThumbUrl { get; set; }

        [StringLength(1000)]
        public string PosterUrl { get; set; }
    }

    // API response models từ external API
    public class MovieListResponse
    {
        public List<MovieItem> Data { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
    }

    public class Pagination
    {
        public int Current_page { get; set; }
        public int Total_pages { get; set; }
        public int Total_items { get; set; }
        public int Limit { get; set; }
    }

    public class MovieItem : MovieBase
    {
        public string Id { get; set; }
        public string Loai_phim { get; set; }
        public string Quoc_gia { get; set; }
        public string Modified { get; set; }
        public string Modified_time { get; set; }
        public string Tmdb_id { get; set; }
    }

    public class MovieDetailResponse : MovieBase
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Thumb_url { get; set; }
        public string Poster_url { get; set; }
        public string Time { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
        public string Director { get; set; }
        public string Directors { get; set; }
        public string Casts { get; set; }
        public string Actors { get; set; }
        public string Genres { get; set; }
        public string Countries { get; set; }
        public List<object> Episodes { get; set; }
        public string Sub_thumb { get; set; }
        public string Sub_poster { get; set; }
        public string Type { get; set; }
        public string Format { get; set; }
        public int View { get; set; }

        // Additional TMDB fields
        public string Tmdb_type { get; set; }
        public string Tmdb_id { get; set; }
        public string Tmdb_season { get; set; }
        public double Tmdb_vote_average { get; set; }
        public int Tmdb_vote_count { get; set; }
        public string Imdb_id { get; set; }

        // Episode information
        public string Total_episodes { get; set; }
        public string Episode_total { get; set; }
        public string Current_episode { get; set; }
        public string Episode_current { get; set; }
    }

    // Database models - Lưu trong DB của chúng ta
    public class CachedMovie : MovieBase
    {
        [Key]
        public new int Id { get; set; }

        [Column(TypeName = "ntext")]
        public string Description { get; set; }

        [StringLength(100)]
        public string Type { get; set; }

        [StringLength(200)]
        public string Country { get; set; }

        [StringLength(500)]
        public string Genres { get; set; }

        [StringLength(500)]
        public string Director { get; set; }

        [StringLength(1000)]
        public string Actors { get; set; }

        [StringLength(50)]
        public string Duration { get; set; }

        [StringLength(50)]
        public string Quality { get; set; }

        [StringLength(50)]
        public string Language { get; set; }

        public int ViewCount { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Store the raw JSON for future use if needed
        [Column(TypeName = "ntext")]
        public string RawData { get; set; }

        // Navigation properties
        public virtual ICollection<CachedEpisode> Episodes { get; set; } = new List<CachedEpisode>();
        public virtual MovieStatistic Statistic { get; set; }
    }

    public class CachedEpisode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string MovieSlug { get; set; }

        [StringLength(200)]
        public string ServerName { get; set; }

        [StringLength(200)]
        public string EpisodeName { get; set; }

        [Required]
        [StringLength(500)]
        public string EpisodeSlug { get; set; }

        [StringLength(1000)]
        public string EmbedUrl { get; set; }

        [StringLength(1000)]
        public string M3u8Url { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Navigation property
        public virtual CachedMovie Movie { get; set; }
    }

    public class MovieStatistic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string MovieSlug { get; set; }

        public int ViewCount { get; set; } = 0;

        public int FavoriteCount { get; set; } = 0;

        public int CommentCount { get; set; } = 0;

        public double AverageRating { get; set; } = 0;

        public int RatingCount { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Navigation property
        public virtual CachedMovie Movie { get; set; }
    }
}
