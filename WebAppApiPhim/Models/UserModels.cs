using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class EpisodeProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(500)]
        public string EpisodeSlug { get; set; }

        public double CurrentTime { get; set; } = 0;

        public double Duration { get; set; } = 0;

        public double Percentage => Duration > 0 ? (CurrentTime / Duration) * 100 : 0;

        public bool IsCompleted => Percentage >= 90;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class UserFavorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string MovieSlug { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class UserRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string MovieSlug { get; set; }

        [Range(1, 10)]
        public double Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    public class UserComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        public int ParentId { get; set; } = 0;

        public int Likes { get; set; } = 0;

        public int Dislikes { get; set; } = 0;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}
