namespace WebAppApiPhim.DTOs
{
    public class EpisodeServerDto
    {
        public string ServerName { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;
    }

    public class EpisodeDto
    {
        public string Id { get; set; } = string.Empty;
        public int EpisodeNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<EpisodeServerDto> Servers { get; set; } = new List<EpisodeServerDto>();
    }

    public class MovieStatisticsDto
    {
        public int Views { get; set; }
        public double AverageRating { get; set; }
        public int FavoriteCount { get; set; }
    }

    public class CompleteMovieResponseDto
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string ThumbUrl { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string TmdbId { get; set; } = string.Empty;
        public double? Rating { get; set; }
        public string TrailerUrl { get; set; } = string.Empty;
        public long Views { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Countries { get; set; } = new List<string>();
        public List<EpisodeDto> Episodes { get; set; } = new List<EpisodeDto>();
        public MovieStatisticsDto? Statistics { get; set; }
    }
}
