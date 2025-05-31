using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebAppApiPhim.Models.DuLieuPhim;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Services
{
    public class DuLieuPhimService : IDuLieuPhimService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DuLieuPhimService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _baseUrl = "https://api.dulieuphim.ink";

        public DuLieuPhimService(HttpClient httpClient, ILogger<DuLieuPhimService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Cấu hình HttpClient
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Cấu hình JSON serializer
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = "v1")
        {
            try
            {
                string endpoint = $"/phim-moi/{version}?page={page}&limit={limit}";
                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                return result ?? new MovieListResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest movies: {Message}", ex.Message);
                return new MovieListResponse();
            }
        }

        public async Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = "v1")
        {
            try
            {
                string endpoint = $"/phim-chi-tiet/{version}?slug={slug}";
                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieDetailResponse>(content, _jsonOptions);

                return result ?? new MovieDetailResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie detail for slug {Slug}: {Message}", slug, ex.Message);
                return new MovieDetailResponse();
            }
        }

        public async Task<TmdbResponse> GetTmdbBySlugAsync(string slug)
        {
            try
            {
                string endpoint = $"/get_tmdb/{slug}";
                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbResponse>(content, _jsonOptions);

                return result ?? new TmdbResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TMDB data for slug {Slug}: {Message}", slug, ex.Message);
                return new TmdbResponse();
            }
        }

        public async Task<ActorResponse> GetActorsBySlugAsync(string slug)
        {
            try
            {
                string endpoint = $"/get-dien-vien/{slug}";
                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ActorResponse>(content, _jsonOptions);

                return result ?? new ActorResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actors for slug {Slug}: {Message}", slug, ex.Message);
                return new ActorResponse();
            }
        }

        public async Task<ProductionResponse> GetProductionBySlugAsync(string slug)
        {
            try
            {
                string endpoint = $"/get-nha-phat-hanh/{slug}";
                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProductionResponse>(content, _jsonOptions);

                return result ?? new ProductionResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting production data for slug {Slug}: {Message}", slug, ex.Message);
                return new ProductionResponse();
            }
        }

        public async Task<ImageResponse> GetImagesBySlugAsync(string slug, string version = "v1")
        {
            try
            {
                string endpoint = $"/get-img/{version}?slug={slug}";
                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ImageResponse>(content, _jsonOptions);

                return result ?? new ImageResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting images for slug {Slug}: {Message}", slug, ex.Message);
                return new ImageResponse();
            }
        }

        public async Task<MovieListResponse> FilterMoviesAsync(
            string? name = null,
            string? type = null,
            string? genre = null,
            string? country = null,
            string? year = null,
            int page = 1,
            int limit = 10)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(name))
                    queryParams.Add($"name={Uri.EscapeDataString(name)}");

                if (!string.IsNullOrEmpty(type))
                    queryParams.Add($"loai_phim={Uri.EscapeDataString(type)}");

                if (!string.IsNullOrEmpty(genre))
                    queryParams.Add($"the_loai={Uri.EscapeDataString(genre)}");

                if (!string.IsNullOrEmpty(country))
                    queryParams.Add($"quoc_gia={Uri.EscapeDataString(country)}");

                if (!string.IsNullOrEmpty(year))
                    queryParams.Add($"year={Uri.EscapeDataString(year)}");

                queryParams.Add($"page={page}");
                queryParams.Add($"limit={limit}");

                string queryString = string.Join("&", queryParams);
                string endpoint = $"/phim-data/v1?{queryString}";

                _logger.LogInformation("Calling API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                return result ?? new MovieListResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering movies: {Message}", ex.Message);
                return new MovieListResponse();
            }
        }
    }
}