using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class Genre : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Slug { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public int MovieCount { get; set; } = 0;
    }

    public class Country : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Slug { get; set; }

        [StringLength(10)]
        public string Code { get; set; }

        public int MovieCount { get; set; } = 0;
    }

    public class MovieType : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Slug { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public int MovieCount { get; set; } = 0;
    }
}
