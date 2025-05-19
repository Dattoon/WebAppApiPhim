namespace WebAppApiPhim.Models
{
    public class MovieType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiValue { get; set; } // Value used in API requests
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Movie> Movies { get; set; }
    }
}
