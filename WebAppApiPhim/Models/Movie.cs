using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApiPhim.Models
{
    public class Movie
    {
        [Key]
        public string Slug { get; set; }

        [Required]
        public string Name { get; set; }

        public string OriginalName { get; set; }

        public string Description { get; set; }

        public string PosterUrl { get; set; }

        public string ThumbUrl { get; set; }

        public string Year { get; set; }

        public int? MovieTypeId { get; set; }

        public string Director { get; set; }

        public string Actors { get; set; }

        public string Duration { get; set; }

        public string Quality { get; set; }

        public string Language { get; set; }

        public int ViewCount { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MovieTypeId")]
        public virtual MovieType MovieType { get; set; }

        public virtual ICollection<MovieGenre> MovieGenres { get; set; }

        public virtual ICollection<MovieCountry> MovieCountries { get; set; }

        public virtual ICollection<Favorite> Favorites { get; set; }

        public virtual ICollection<WatchHistory> WatchHistories { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }

        public virtual ICollection<Rating> Ratings { get; set; }
    }

    public class CachedMovie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Slug { get; set; }

        public string Name { get; set; }

        public string OriginalName { get; set; }

        public string Description { get; set; }

        public string PosterUrl { get; set; }

        public string ThumbUrl { get; set; }

        public string Year { get; set; }

        public string Type { get; set; }

        public string Country { get; set; }

        public string Genres { get; set; }

        public string Director { get; set; }

        public string Actors { get; set; }

        public string Duration { get; set; }

        public string Quality { get; set; }

        public string Language { get; set; }

        public int ViewCount { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public string RawData { get; set; } // JSON data from API
    }

    public class CachedEpisode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        public string ServerName { get; set; }

        public string EpisodeName { get; set; }

        [Required]
        public string EpisodeSlug { get; set; }

        public string EmbedUrl { get; set; }

        public string M3u8Url { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class FeaturedMovie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        public int DisplayOrder { get; set; }

        [Required]
        public string Category { get; set; } // "home", "trending", "recommended", etc.

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class MovieStatistic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        public int ViewCount { get; set; }

        public int FavoriteCount { get; set; }

        public int CommentCount { get; set; }

        public double AverageRating { get; set; }

        public int RatingCount { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}