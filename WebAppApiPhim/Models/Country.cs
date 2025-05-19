namespace WebAppApiPhim.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiValue { get; set; } // Value used in API requests
        public string Code { get; set; } // ISO country code
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MovieCountry> MovieCountries { get; set; }
    }
}
