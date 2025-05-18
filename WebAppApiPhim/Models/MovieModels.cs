using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebAppApiPhim.Models
{
    public class MovieListResponse
    {
        public List<MovieItem> Data { get; set; } = new List<MovieItem>();
        public Pagination Pagination { get; set; }
    }

    public class Pagination
    {
        public int Current_page { get; set; }
        public int Total_pages { get; set; }
        public int Total_items { get; set; }
        public int Limit { get; set; }
    }

    public class MovieItem
    {
        [JsonConverter(typeof(StringOrIntConverter))]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Year { get; set; }
        public string Loai_phim { get; set; }
        public string Quoc_gia { get; set; }
        public string Modified { get; set; }
        public string Modified_time { get; set; }
        public string Tmdb_id { get; set; }

        // Các trường bổ sung
        public string ThumbUrl { get; set; }
        public string PosterUrl { get; set; }
    }

    public class MovieDetailResponse
    {
        [JsonConverter(typeof(StringOrIntConverter))]
        public string Id { get; set; }
        public string Version { get; set; }
        public string Tmdb_type { get; set; }
        public string Tmdb_id { get; set; }
        [JsonConverter(typeof(StringOrIntNullableConverter))]
        public string Tmdb_season { get; set; }
        public double Tmdb_vote_average { get; set; }
        public int Tmdb_vote_count { get; set; }
        public string Imdb_id { get; set; }
        public string Movie_id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string OriginalName { get; set; }
        public string OriginName { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Thumb_url { get; set; }
        public string Poster_url { get; set; }
        public string Created { get; set; }
        public string Created_time { get; set; }
        public string Modified { get; set; }
        public string Modified_time { get; set; }
        public string Created_at { get; set; }
        public string Updated_at { get; set; }
        [JsonConverter(typeof(StringOrIntNullableConverter))]
        public string Total_episodes { get; set; }
        public string Episode_total { get; set; }
        public string Current_episode { get; set; }
        public string Episode_current { get; set; }
        public string Time { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
        public string Lang { get; set; }
        public string Director { get; set; }
        public string Directors { get; set; }
        public string Casts { get; set; }
        public string Actors { get; set; }
        public string Format { get; set; }
        public string Genres { get; set; }
        public string Categories { get; set; }
        public string Year { get; set; }
        public string Countries { get; set; }
        public string Country { get; set; }
        public string Platforms { get; set; }
        public List<object> Episodes { get; set; }
        public string Sub_thumb { get; set; }
        public string Sub_poster { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Trailer_url { get; set; }
        public string Notify { get; set; }
        public string Showtimes { get; set; }
        public int View { get; set; }
        public int Is_copyright { get; set; }
        public int Sub_docquyen { get; set; }
        public int Chieurap { get; set; }
        public string Film_type { get; set; }
    }

    public class MovieDetail
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Slug { get; set; }
        public string Year { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public string Country { get; set; }
        public string PosterUrl { get; set; }
        public string BackdropUrl { get; set; }
        public double Rating { get; set; }
        public string Director { get; set; }
        public string Actors { get; set; }
        public string Duration { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
    }

    public class Episode
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Filename { get; set; }
        public string Link { get; set; }
    }

    public class CommentViewModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }
        public string Content { get; set; }
        public string CreatedAt { get; set; }
    }

    public class MovieDetailViewModel
    {
        public MovieDetail Movie { get; set; }
        public List<Episode> Episodes { get; set; } = new List<Episode>();
        public List<MovieItem> RelatedMovies { get; set; } = new List<MovieItem>();
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    }
}
