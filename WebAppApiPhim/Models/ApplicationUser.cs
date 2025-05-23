using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(200)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; } = string.Empty; // Set default value

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLoginAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public virtual ICollection<UserRating> Ratings { get; set; } = new List<UserRating>();
        public virtual ICollection<UserComment> Comments { get; set; } = new List<UserComment>();
        public virtual ICollection<EpisodeProgress> WatchProgress { get; set; } = new List<EpisodeProgress>();
    }
}
