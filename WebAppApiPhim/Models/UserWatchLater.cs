using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class UserWatchLater
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string MovieSlug { get; set; } = string.Empty;

        [Required]
        public DateTime AddedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual CachedMovie? Movie { get; set; }
    }
}
