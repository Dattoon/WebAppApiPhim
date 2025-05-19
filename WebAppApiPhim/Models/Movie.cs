namespace WebAppApiPhim.Models
{
    public class Movie
    {
        public string Slug { get; set; } // Primary Key
        public string Name { get; set; }
        public string PosterUrl { get; set; }
        public string ThumbUrl { get; set; }
        public string Year { get; set; }
        public string Type { get; set; } // Phim lẻ, Phim bộ, etc.
        public string Country { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<WatchHistory> WatchHistories { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
    }
}
