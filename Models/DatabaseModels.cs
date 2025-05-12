using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApiPhim.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string Salt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string Role { get; set; } = "User";
    }

    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [StringLength(255)]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(255)]
        public string MovieName { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class WatchHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [StringLength(255)]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(255)]
        public string MovieName { get; set; }

        public double WatchedPercentage { get; set; }

        public DateTime WatchedAt { get; set; } = DateTime.Now;

        public string EpisodeSlug { get; set; }
    }

    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [StringLength(255)]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}