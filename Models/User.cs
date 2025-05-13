using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace WebAppApiPhim.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<WatchHistory> WatchHistories { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
    }

    public class Favorite
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public string MoviePosterUrl { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class WatchHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public string MovieName { get; set; }
        public string MoviePosterUrl { get; set; }
        public string EpisodeSlug { get; set; }
        public string EpisodeName { get; set; }
        public double WatchedPercentage { get; set; }
        public DateTime WatchedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class Rating
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string MovieSlug { get; set; }
        public int Value { get; set; } // 1-10
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}
