using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using WebAppApiPhim.Models;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text.Json.Serialization;

namespace WebAppApiPhim.Services
{
    public class MovieApiService : IMovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.dulieuphim.ink";
        private readonly JsonSerializerOptions _jsonOptions;

        public MovieApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Increase timeout

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        public async Task<MovieListResponse> GetNewMoviesAsync(int page = 1, int limit = 10)
        {
            try
            {
                // Try v3 endpoint first (most recent)
                string requestUrl = $"{_baseUrl}/phim-moi/v3?page={page}&limit={limit}";
                Debug.WriteLine($"Requesting: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to v2 endpoint
                    requestUrl = $"{_baseUrl}/phim-moi/v2?page={page}&limit={limit}";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Fallback to v1 endpoint
                        requestUrl = $"{_baseUrl}/phim-moi/v1?page={page}&limit={limit}";
                        Debug.WriteLine($"Fallback to: {requestUrl}");
                        response = await _httpClient.GetAsync(requestUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine($"All API endpoints failed: {response.StatusCode}");
                            return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                        }
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Ensure all movies have poster and thumbnail URLs
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        // If poster or thumb is missing, try to get them from the API
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetNewMoviesAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug)
        {
            try
            {
                if (string.IsNullOrEmpty(slug))
                {
                    Debug.WriteLine("Slug is null or empty");
                    return CreateEmptyMovieDetailResponse("invalid-slug");
                }

                // Try different API versions for movie details
                MovieDetailResponse result = null;

                // Try v3 endpoint first
                result = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/phim-chi-tiet/v3?slug={slug}");

                // If v3 fails, try v2
                if (result == null)
                {
                    result = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/phim-chi-tiet/v2?slug={slug}");

                    // If v2 fails, try v1
                    if (result == null)
                    {
                        result = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/phim-chi-tiet/v1?slug={slug}");
                    }
                }

                // If all API endpoints fail, try the TMDB endpoint
                if (result == null)
                {
                    var tmdbData = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/get_tmdb/{slug}");

                    if (tmdbData != null && !string.IsNullOrEmpty(tmdbData.Id))
                    {
                        // We got TMDB data, now try to get full details
                        result = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/phim/{slug}/v3");

                        if (result == null)
                        {
                            result = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/phim/{slug}/v2");

                            if (result == null)
                            {
                                result = await TryGetMovieDetailFromEndpoint($"{_baseUrl}/phim/{slug}/v1");
                            }
                        }
                    }
                }

                // If we still don't have data, create an empty response
                if (result == null)
                {
                    Debug.WriteLine("All API endpoints failed to return movie details");
                    return CreateEmptyMovieDetailResponse(slug);
                }

                // Ensure we have a valid Movie object
                if (result.Movie == null && !string.IsNullOrEmpty(result.Name))
                {
                    Debug.WriteLine("Converting root-level properties to Movie object");
                    result.Movie = new MovieDetail
                    {
                        Id = result.Id,
                        Name = result.Name,
                        OriginalName = result.OriginalName,
                        Slug = result.Slug ?? slug,
                        Year = result.Year,
                        Description = result.Description ?? result.Content,
                        Type = result.Type,
                        Status = result.Status,
                        Genres = result.Genres != null ? result.Genres : ParseGenres(result.Categories),
                        Country = result.Country ?? result.Countries,
                        PosterUrl = result.PosterUrl ?? result.ThumbUrl,
                        BackdropUrl = result.BackdropUrl,
                        Rating = result.Rating
                    };
                }

                // Ensure the movie has a slug
                if (result.Movie != null && string.IsNullOrEmpty(result.Movie.Slug))
                {
                    result.Movie.Slug = slug;
                }

                // Try to get images if they're missing
                if (result.Movie != null && (string.IsNullOrEmpty(result.Movie.PosterUrl) || string.IsNullOrEmpty(result.Movie.BackdropUrl)))
                {
                    try
                    {
                        var imageData = await GetMovieImagesAsync(slug);
                        if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                        {
                            if (string.IsNullOrEmpty(result.Movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                result.Movie.PosterUrl = imageData.PosterUrl;

                            if (string.IsNullOrEmpty(result.Movie.BackdropUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                result.Movie.BackdropUrl = imageData.ThumbUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting images: {ex.Message}");
                    }
                }

                // Parse episodes if they're in a different format
                if (result.Episodes == null || !result.Episodes.Any())
                {
                    var parsedEpisodes = ParseEpisodesFromResponse(result);
                    result.Episodes = parsedEpisodes.Cast<object>().ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetMovieDetailBySlugAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return CreateEmptyMovieDetailResponse(slug);
            }
        }

        private async Task<MovieDetailResponse> TryGetMovieDetailFromEndpoint(string endpoint)
        {
            try
            {
                Debug.WriteLine($"Trying endpoint: {endpoint}");
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MovieDetailResponse>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with endpoint {endpoint}: {ex.Message}");
                return null;
            }
        }

        private List<string> ParseGenres(string categories)
        {
            if (string.IsNullOrEmpty(categories))
                return new List<string>();

            return categories.Split(',').Select(c => c.Trim()).ToList();
        }

        private List<Episode> ParseEpisodesFromResponse(MovieDetailResponse response)
        {
            var episodes = new List<Episode>();

            // Check if episodes are in the format shown in the example
            if (response.Episodes != null && response.Episodes.Any())
            {
                foreach (var episodeObj in response.Episodes)
                {
                    if (episodeObj is JsonElement jsonElement1)
                    {
                        try
                        {
                            if (jsonElement1.TryGetProperty("server_name", out var _) &&
                                jsonElement1.TryGetProperty("server_data", out var serverData))
                            {
                                foreach (var episode in serverData.EnumerateArray())
                                {
                                    episodes.Add(new Episode
                                    {
                                        Name = episode.GetProperty("name").GetString(),
                                        Slug = episode.GetProperty("slug").GetString(),
                                        Filename = episode.GetProperty("filename").GetString(),
                                        Link = episode.GetProperty("link_embed").GetString() ?? episode.GetProperty("link_m3u8").GetString()
                                    });
                                }
                            }
                            else if (jsonElement1.TryGetProperty("items", out var items))
                            {
                                foreach (var item in items.EnumerateArray())
                                {
                                    try
                                    {
                                        episodes.Add(new Episode
                                        {
                                            Name = item.GetProperty("name").GetString(),
                                            Slug = item.GetProperty("slug").GetString(),
                                            Filename = item.GetProperty("name").GetString(),
                                            Link = item.GetProperty("embed").GetString() ?? item.GetProperty("m3u8").GetString()
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error parsing episode item: {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error parsing episodes from JsonElement: {ex.Message}");
                        }
                    }
                }
            }

            // If we still don't have episodes, try to parse from the raw content
            if (episodes.Count == 0 && !string.IsNullOrEmpty(response.Content))
            {
                var matches = Regex.Matches(response.Content, @"Tập\s+(\d+)\|(.+?)(?=Tập|$)");
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var episodeName = match.Groups[1].Value.Trim();
                        var episodeLink = match.Groups[2].Value.Trim();

                        episodes.Add(new Episode
                        {
                            Name = episodeName,
                            Slug = $"tap-{episodeName}",
                            Filename = $"Tập {episodeName}",
                            Link = episodeLink
                        });
                    }
                }
            }

            return episodes;
        }

        public async Task<MovieListResponse> SearchMoviesAsync(string keyword, int page = 1)
        {
            try
            {
                string requestUrl = $"{_baseUrl}/search/v1?keyword={Uri.EscapeDataString(keyword)}&page={page}";
                Debug.WriteLine($"Requesting: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // Try phim-data endpoint as fallback
                    requestUrl = $"{_baseUrl}/phim-data/v1?name={Uri.EscapeDataString(keyword)}&page={page}&limit=10";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Add images to movies
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in SearchMoviesAsync: {ex.Message}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieListResponse> GetMoviesByCategoryAsync(string category, int page = 1)
        {
            try
            {
                string requestUrl = $"{_baseUrl}/the-loai/{category}/v1?page={page}";
                Debug.WriteLine($"Requesting: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // Try phim-data endpoint as fallback
                    requestUrl = $"{_baseUrl}/phim-data/v1?the_loai={Uri.EscapeDataString(category)}&page={page}&limit=10";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Add images to movies
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetMoviesByCategoryAsync: {ex.Message}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieListResponse> GetMoviesByCountryAsync(string country, int page = 1)
        {
            try
            {
                string requestUrl = $"{_baseUrl}/quoc-gia/{country}/v1?page={page}";
                Debug.WriteLine($"Requesting: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // Try phim-data endpoint as fallback
                    requestUrl = $"{_baseUrl}/phim-data/v1?quoc_gia={Uri.EscapeDataString(country)}&page={page}&limit=10";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Add images to movies
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetMoviesByCountryAsync: {ex.Message}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieListResponse> GetMoviesByTypeAsync(string type, int page = 1)
        {
            try
            {
                string requestUrl = $"{_baseUrl}/danh-sach/{type}/v1?page={page}";
                Debug.WriteLine($"Requesting: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // Try phim-data endpoint as fallback
                    requestUrl = $"{_baseUrl}/phim-data/v1?loai_phim={Uri.EscapeDataString(type)}&page={page}&limit=10";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Add images to movies
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetMoviesByTypeAsync: {ex.Message}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieListResponse> GetRelatedMoviesAsync(string slug, int limit = 6)
        {
            try
            {
                // Since the API might not have a direct endpoint for related movies,
                // we'll try to get movies from the same category or by the same director
                string requestUrl = $"{_baseUrl}/phim-moi/v3?limit={limit}";
                Debug.WriteLine($"Requesting related movies: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    requestUrl = $"{_baseUrl}/phim-moi/v2?limit={limit}";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        requestUrl = $"{_baseUrl}/phim-moi/v1?limit={limit}";
                        Debug.WriteLine($"Fallback to: {requestUrl}");
                        response = await _httpClient.GetAsync(requestUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                            return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                        }
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Add images to movies
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetRelatedMoviesAsync: {ex.Message}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        public async Task<MovieListResponse> GetTrendingMoviesAsync(int page = 1, int limit = 10)
        {
            try
            {
                // Since the API might not have a direct endpoint for trending movies,
                // we'll use the new movies endpoint as a fallback
                string requestUrl = $"{_baseUrl}/phim-moi/v3?page={page}&limit={limit}";
                Debug.WriteLine($"Requesting trending movies: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    requestUrl = $"{_baseUrl}/phim-moi/v2?page={page}&limit={limit}";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        requestUrl = $"{_baseUrl}/phim-moi/v1?page={page}&limit={limit}";
                        Debug.WriteLine($"Fallback to: {requestUrl}");
                        response = await _httpClient.GetAsync(requestUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                            return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
                        }
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MovieListResponse>(content, _jsonOptions);

                // Add images to movies
                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                if (imageData.ThumbUrl != null || imageData.PosterUrl != null)
                                {
                                    if (string.IsNullOrEmpty(movie.PosterUrl) && !string.IsNullOrEmpty(imageData.PosterUrl))
                                        movie.PosterUrl = imageData.PosterUrl;

                                    if (string.IsNullOrEmpty(movie.ThumbUrl) && !string.IsNullOrEmpty(imageData.ThumbUrl))
                                        movie.ThumbUrl = imageData.ThumbUrl;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetTrendingMoviesAsync: {ex.Message}");
                return new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
            }
        }

        private async Task<(string ThumbUrl, string PosterUrl)> GetMovieImagesAsync(string slug)
        {
            try
            {
                // Try v3 endpoint first
                string requestUrl = $"{_baseUrl}/get-img/v3?slug={slug}";
                Debug.WriteLine($"Requesting images: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to v2 endpoint
                    requestUrl = $"{_baseUrl}/get-img/v2?slug={slug}";
                    Debug.WriteLine($"Fallback to: {requestUrl}");
                    response = await _httpClient.GetAsync(requestUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Fallback to v1 endpoint
                        requestUrl = $"{_baseUrl}/get-img/v1?slug={slug}";
                        Debug.WriteLine($"Fallback to: {requestUrl}");
                        response = await _httpClient.GetAsync(requestUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine($"All image API endpoints failed: {response.StatusCode}");
                            return (null, null);
                        }
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ImageResponse>(content, _jsonOptions);

                if (result?.Success == true)
                {
                    return (result.SubThumb, result.SubPoster);
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetMovieImagesAsync: {ex.Message}");
                return (null, null);
            }
        }

        private MovieDetailResponse CreateEmptyMovieDetailResponse(string slug)
        {
            return new MovieDetailResponse
            {
                Movie = new MovieDetail
                {
                    Name = "Movie Not Found",
                    Slug = slug,
                    Description = "Unable to load movie details. Please try again later.",
                    PosterUrl = "/placeholder.svg?height=450&width=300"
                },
                Episodes = new List<object>()
            };
        }
    }

    public class ImageResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("sub_thumb")]
        public string SubThumb { get; set; }

        [JsonPropertyName("sub_poster")]
        public string SubPoster { get; set; }
    }
}