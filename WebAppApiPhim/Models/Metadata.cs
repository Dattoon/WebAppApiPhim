using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class MovieType
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ApiValue { get; set; } // Value used in API requests

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Movie> Movies { get; set; }
    }

    public class Genre
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ApiValue { get; set; } // Value used in API requests

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MovieGenre> MovieGenres { get; set; }
    }

    public class Country
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ApiValue { get; set; } // Value used in API requests

        public string Code { get; set; } // ISO country code

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MovieCountry> MovieCountries { get; set; }
    }

    public class MovieGenre
    {
        [Required]
        public string MovieSlug { get; set; }

        [Required]
        public int GenreId { get; set; }

        // Navigation properties
        public virtual Movie Movie { get; set; }
        public virtual Genre Genre { get; set; }
    }

    public class MovieCountry
    {
        [Required]
        public string MovieSlug { get; set; }

        [Required]
        public int CountryId { get; set; }

        // Navigation properties
        public virtual Movie Movie { get; set; }
        public virtual Country Country { get; set; }
    }
}