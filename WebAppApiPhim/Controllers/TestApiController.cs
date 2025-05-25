using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestApiController : ControllerBase
    {
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<TestApiController> _logger;

        public TestApiController(IMovieApiService movieApiService, ILogger<TestApiController> logger)
        {
            _movieApiService = movieApiService;
            _logger = logger;
        }

        [HttpGet("test-api-connection")]
        public async Task<IActionResult> TestApiConnection()
        {
            try
            {
                _logger.LogInformation("Testing API connection...");

                var result = await _movieApiService.GetLatestMoviesAsync(1, 5, "v1");

                return Ok(new
                {
                    Success = true,
                    Message = "API connection successful",
                    MoviesCount = result.Data.Count,
                    Movies = result.Data.Take(3).Select(m => new { m.Slug, m.Title, m.PosterUrl }),
                    Pagination = result.Pagination
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API connection test failed");
                return BadRequest(new
                {
                    Success = false,
                    Message = "API connection failed",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("raw-api-response")]
        public async Task<IActionResult> GetRawApiResponse([FromQuery] string version = "v1")
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api.dulieuphim.ink/");

                var endpoint = $"phim-moi/{version}?page=1&limit=5";
                var response = await httpClient.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    Endpoint = endpoint,
                    StatusCode = response.StatusCode,
                    Content = content
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("test-movie-detail/{slug}")]
        public async Task<IActionResult> TestMovieDetail(string slug)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api.dulieuphim.ink/");

                var testEndpoints = new[]
                {
                    $"phim/{slug}",
                    $"movie/{slug}",
                    $"detail/{slug}",
                    $"v1/phim/{slug}",
                    $"v2/phim/{slug}",
                    $"v3/phim/{slug}"
                };

                var results = new List<object>();

                foreach (var endpoint in testEndpoints)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(endpoint);
                        var content = await response.Content.ReadAsStringAsync();

                        results.Add(new
                        {
                            Endpoint = endpoint,
                            StatusCode = response.StatusCode,
                            ContentLength = content.Length,
                            ContentPreview = content.Length > 200 ? content.Substring(0, 200) + "..." : content
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            Endpoint = endpoint,
                            Error = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    Slug = slug,
                    TestResults = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
