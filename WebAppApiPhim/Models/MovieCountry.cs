namespace WebAppApiPhim.Models
{
    public class MovieCountry
    {
        public string MovieSlug { get; set; }
        public int CountryId { get; set; }

        public virtual Movie Movie { get; set; }
        public virtual Country Country { get; set; }
    }
}
