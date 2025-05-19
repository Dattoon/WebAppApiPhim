namespace WebAppApiPhim.Models
{
    public class MovieGenre
    {
        public string MovieSlug { get; set; }
        public int GenreId { get; set; }

        public virtual Movie Movie { get; set; }
        public virtual Genre Genre { get; set; }
    }
}
