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

        /// <summary>
        /// Hàm tổng quát để gọi API với phiên bản fallback (v3 -> v2 -> v1)
        /// </summary>
        /// <param name="endpoint">Endpoint cơ bản (ví dụ: /phim-moi)</param>
        /// <param name="queryParams">Tham số query (ví dụ: ?page=1&limit=10)</param>
        /// <param name="startVersion">Phiên bản bắt đầu (mặc định: v3)</param>
        /// <returns>Kết quả hoặc default nếu thất bại</returns>
        private async Task<T> CallApiWithVersionFallbackAsync<T>(string endpoint, string queryParams, string startVersion = "v3")
        {
            var versions = new List<string> { "v3", "v2", "v1" };
            int startIndex = versions.IndexOf(startVersion);
            if (startIndex == -1)
            {
                Debug.WriteLine($"Invalid start version: {startVersion}. Defaulting to v3.");
                startIndex = 0; // Nếu phiên bản không hợp lệ, bắt đầu từ v3
            }

            T result = default;
            string responseContent = null;
            bool isValidResponse = false;

            for (int i = startIndex; i < versions.Count; i++)
            {
                string version = versions[i];
                string requestUrl = $"{_baseUrl}/{endpoint}/{version}{queryParams}";
                Debug.WriteLine($"Requesting: {requestUrl}");

                try
                {
                    var response = await _httpClient.GetAsync(requestUrl);
                    Debug.WriteLine($"Response status: {response.StatusCode}");

                    responseContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Response content ({version}): {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...");

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            result = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                            if (result != null)
                            {
                                // Kiểm tra đặc biệt cho ImageResponse
                                if (result is ImageResponse imageResponse)
                                {
                                    if (imageResponse.Success && (!string.IsNullOrEmpty(imageResponse.SubThumb) || !string.IsNullOrEmpty(imageResponse.SubPoster)))
                                    {
                                        isValidResponse = true;
                                        Debug.WriteLine($"Successfully deserialized and validated response from {version}");
                                        break;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"ImageResponse from {version} is not valid (Success=false or both URLs empty).");
                                    }
                                }
                                else
                                {
                                    // Đối với các loại khác (như MovieListResponse), chỉ cần result không null
                                    isValidResponse = true;
                                    Debug.WriteLine($"Successfully deserialized response from {version}");
                                    break;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Debug.WriteLine($"Deserialization failed for {version}: {ex.Message}, Raw response: {responseContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calling {version} endpoint: {ex.Message}");
                }

                Debug.WriteLine($"Failed to get valid data from {version}. Trying next version...");
            }

            if (!isValidResponse)
            {
                Debug.WriteLine("All API versions failed to return valid data.");
                return default;
            }

            return result;
        }

        public async Task<MovieListResponse> GetNewMoviesAsync(int page = 1, int limit = 10)
        {
            try
            {
                string queryParams = $"?page={page}&limit={limit}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>("phim-moi", queryParams, "v3");

                if (result?.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        Debug.WriteLine($"Movie {movie.Slug}: Initial PosterUrl={movie.PosterUrl}, ThumbUrl={movie.ThumbUrl}");
                        if (string.IsNullOrEmpty(movie.PosterUrl) || string.IsNullOrEmpty(movie.ThumbUrl))
                        {
                            try
                            {
                                var imageData = await GetMovieImagesAsync(movie.Slug);
                                Debug.WriteLine($"Images for {movie.Slug}: ThumbUrl={imageData.ThumbUrl}, PosterUrl={imageData.PosterUrl}");
                                if (!string.IsNullOrEmpty(imageData.PosterUrl))
                                    movie.PosterUrl = imageData.PosterUrl;

                                if (!string.IsNullOrEmpty(imageData.ThumbUrl))
                                    movie.ThumbUrl = imageData.ThumbUrl;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error getting images for {movie.Slug}: {ex.Message}");
                            }
                        }
                    }
                }

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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

                MovieDetailResponse result = null;

                // Try v3, v2, v1 for phim-chi-tiet
                result = await CallApiWithVersionFallbackAsync<MovieDetailResponse>($"phim-chi-tiet", $"?slug={slug}", "v3");

                // If no data, try TMDB endpoint
                if (result == null)
                {
                    var tmdbData = await CallApiWithVersionFallbackAsync<MovieDetailResponse>($"get_tmdb", $"?slug={slug}", "v3");
                    if (tmdbData != null && !string.IsNullOrEmpty(tmdbData.Id))
                    {
                        result = await CallApiWithVersionFallbackAsync<MovieDetailResponse>($"phim/{slug}", "", "v3");
                    }
                }

                if (result == null)
                {
                    Debug.WriteLine("All API endpoints failed to return movie details");
                    return CreateEmptyMovieDetailResponse(slug);
                }

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

                if (result.Movie != null && string.IsNullOrEmpty(result.Movie.Slug))
                {
                    result.Movie.Slug = slug;
                }

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

        private List<string> ParseGenres(string categories)
        {
            if (string.IsNullOrEmpty(categories))
                return new List<string>();

            return categories.Split(',').Select(c => c.Trim()).ToList();
        }

        private List<Episode> ParseEpisodesFromResponse(MovieDetailResponse response)
        {
            var episodes = new List<Episode>();

            if (response.Episodes != null && response.Episodes.Any())
            {
                foreach (var episodeObj in response.Episodes)
                {
                    if (episodeObj is JsonElement jsonElement)
                    {
                        try
                        {
                            if (jsonElement.TryGetProperty("server_name", out var _) &&
                                jsonElement.TryGetProperty("server_data", out var serverData))
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
                            else if (jsonElement.TryGetProperty("items", out var items))
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
                string queryParams = $"?keyword={Uri.EscapeDataString(keyword)}&page={page}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>("search", queryParams, "v3");

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

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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
                string queryParams = $"?page={page}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>($"the-loai/{category}", queryParams, "v3");

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

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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
                string queryParams = $"?page={page}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>($"quoc-gia/{country}", queryParams, "v3");

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

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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
                string queryParams = $"?page={page}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>($"danh-sach/{type}", queryParams, "v3");

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

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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
                string queryParams = $"?limit={limit}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>("phim-moi", queryParams, "v3");

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

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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
                string queryParams = $"?page={page}&limit={limit}";
                var result = await CallApiWithVersionFallbackAsync<MovieListResponse>("phim-moi", queryParams, "v3");

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

                return result ?? new MovieListResponse { Data = new List<MovieItem>(), Pagination = new Pagination() };
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
                var result = await CallApiWithVersionFallbackAsync<ImageResponse>("get-img", $"?slug={slug}", "v3");

                if (result != null && result.Success)
                {
                    string thumbUrl = string.IsNullOrEmpty(result.SubThumb) ? "/placeholder.svg?height=150&width=100" : result.SubThumb;
                    string posterUrl = string.IsNullOrEmpty(result.SubPoster) ? "/placeholder.svg?height=450&width=300" : result.SubPoster;
                    return (thumbUrl, posterUrl);
                }

                Debug.WriteLine($"No valid images found for slug: {slug}. Using placeholders.");
                return ("/placeholder.svg?height=150&width=100", "/placeholder.svg?height=450&width=300");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetMovieImagesAsync: {ex.Message}");
                return ("/placeholder.svg?height=150&width=100", "/placeholder.svg?height=450&width=300");
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