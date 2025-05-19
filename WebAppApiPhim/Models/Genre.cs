namespace WebAppApiPhim.Models
{
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiValue { get; set; } // Value used in API requests
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MovieGenre> MovieGenres { get; set; }
    }
}
