using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebAppApiPhim.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; } = "default-avatar.jpg";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<WatchHistory> WatchHistories { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<EpisodeProgress> EpisodeProgresses { get; set; }
        public virtual ICollection<UserActivity> UserActivities { get; set; }
    }

    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        public string MovieName { get; set; }

        public string MoviePosterUrl { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public class WatchHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        public string MovieName { get; set; }

        public string MoviePosterUrl { get; set; }

        public string EpisodeSlug { get; set; }

        public string EpisodeName { get; set; }

        public string ServerName { get; set; }

        public double WatchedPercentage { get; set; }

        public double CurrentTime { get; set; }

        public double Duration { get; set; }

        public DateTime WatchedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public class Rating
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        [Range(1, 10)]
        public int Value { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public class EpisodeProgress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string MovieSlug { get; set; }

        [Required]
        public string EpisodeSlug { get; set; }

        public double CurrentTime { get; set; }

        public double Duration { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public class UserActivity
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string ActivityType { get; set; } // "login", "register", "watch", "favorite", "comment", "rate"

        public string EntityId { get; set; } // ID của đối tượng liên quan (phim, tập phim, bình luận, v.v.)

        public string Details { get; set; } // Chi tiết bổ sung (JSON)

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}