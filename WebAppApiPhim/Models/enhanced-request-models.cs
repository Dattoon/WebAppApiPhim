using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Controllers.Enhanced
{
    public class MovieSearchRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public string Query { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int Limit { get; set; } = 20;

        public string Type { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Country { get; set; }
    }
}
