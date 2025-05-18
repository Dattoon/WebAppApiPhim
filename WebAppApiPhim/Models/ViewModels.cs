using System.Collections.Generic;

namespace WebAppApiPhim.Models
{
    public class HomeViewModel
    {
        public List<MovieItem> LatestMovies { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
    }

    public class SearchViewModel
    {
        public string Query { get; set; }
        public List<MovieItem> Movies { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
        public int CurrentPage { get; set; } = 1;
    }

    public class FilterViewModel
    {
        public string Type { get; set; }
        public string Genre { get; set; }
        public string Country { get; set; }
        public string Year { get; set; }
        public List<MovieItem> Movies { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
        public int CurrentPage { get; set; } = 1;

        // Danh sách các bộ lọc
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Countries { get; set; } = new List<string>();
        public List<string> Years { get; set; } = new List<string>();
        public List<string> MovieTypes { get; set; } = new List<string>();
    }

    
}
