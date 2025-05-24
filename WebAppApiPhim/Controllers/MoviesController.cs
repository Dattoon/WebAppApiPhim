using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieApiService _movieApiService;
        private readonly IStreamingService _streamingService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(
            IMovieApiService movieApiService,
            IStreamingService streamingService,
            ILogger<MoviesController> logger)
        {
            _movieApiService = movieApiService;
            _streamingService = streamingService;
            _logger = logger;
        }

        [HttpGet("latest")]
        public async Task<ActionResult<ApiResponse<MovieListResponse>>> GetLatestMovies(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string version = null)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Page must be greater than 0"
                    });
                }
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 50"
                    });
                }

                var result = await _movieApiService.GetLatestMoviesAsync(page, limit, version);

                return Ok(new ApiResponse<MovieListResponse>
                {
                    Success = true,
                    Message = "Latest movies retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest movies");
                return StatusCode(500, new ApiResponse<MovieListResponse>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<ApiResponse<MovieDetailViewModel>>> GetMovieDetail(
            string slug,
            [FromQuery] string version = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return BadRequest(new ApiResponse<MovieDetailViewModel>
                    {
                        Success = false,
                        Message = "Movie slug is required"
                    });
                }

                var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug, version);

                if (movieDetail == null)
                {
                    return NotFound(new ApiResponse<MovieDetailViewModel>
                    {
                        Success = false,
                        Message = "Movie not found"
                    });
                }

                var cachedMovie = await _streamingService.GetCachedMovieAsync(slug);
                var servers = await _streamingService.GetEpisodesAsync(slug);

                var viewModel = new MovieDetailViewModel
                {
                    Slug = movieDetail.Slug,
                    Name = movieDetail.Name,
                    OriginalName = movieDetail.OriginalName,
                    Description = movieDetail.Description,
                    Year = movieDetail.Year,
                    ThumbnailUrl = movieDetail.Thumb_url ?? movieDetail.Sub_thumb ?? "/placeholder.svg?height=450&width=300",
                    PosterUrl = movieDetail.Poster_url ?? movieDetail.Sub_poster ?? "/placeholder.svg?height=450&width=300",
                    Type = movieDetail.Format,
                    Country = movieDetail.Countries,
                    Genres = movieDetail.Genres,
                    Director = movieDetail.Director ?? movieDetail.Directors,
                    Actors = movieDetail.Casts ?? movieDetail.Actors,
                    Duration = movieDetail.Time,
                    Quality = movieDetail.Quality,
                    Language = movieDetail.Language,
                    ViewCount = cachedMovie?.ViewCount ?? movieDetail.View,
                    AverageRating = cachedMovie?.Statistic?.AverageRating ?? 0,
                    RatingCount = cachedMovie?.Statistic?.RatingCount ?? 0,
                    IsFavorite = false,
                    UserRating = null,
                    Servers = servers
                };

                // Increment view count synchronously to ensure it executes
                try
                {
                    await _streamingService.IncrementViewCountAsync(slug);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error incrementing view count for {slug}");
                }

                return Ok(new ApiResponse<MovieDetailViewModel>
                {
                    Success = true,
                    Message = "Movie detail retrieved successfully",
                    Data = viewModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting movie detail for slug: {slug}");
                return StatusCode(500, new ApiResponse<MovieDetailViewModel>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<MovieListResponse>>> SearchMovies(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Search query is required"
                    });
                }

                if (page < 1)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Page must be greater than 0"
                    });
                }
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 50"
                    });
                }

                var result = await _movieApiService.SearchMoviesAsync(query, page, limit);

                return Ok(new ApiResponse<MovieListResponse>
                {
                    Success = true,
                    Message = "Search completed successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching movies with query: {query}");
                return StatusCode(500, new ApiResponse<MovieListResponse>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        [HttpGet("filter")]
        public async Task<ActionResult<ApiResponse<MovieListResponse>>> FilterMovies(
            [FromQuery] string type = null,
            [FromQuery] string genre = null,
            [FromQuery] string country = null,
            [FromQuery] string year = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Page must be greater than 0"
                    });
                }
                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 50"
                    });
                }

                var result = await _movieApiService.FilterMoviesAsync(type, genre, country, year, page, limit);

                return Ok(new ApiResponse<MovieListResponse>
                {
                    Success = true,
                    Message = "Movies filtered successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering movies");
                return StatusCode(500, new ApiResponse<MovieListResponse>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        [HttpGet("{slug}/related")]
        public async Task<ActionResult<ApiResponse<MovieListResponse>>> GetRelatedMovies(
            string slug,
            [FromQuery] int limit = 6,
            [FromQuery] string version = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Movie slug is required"
                    });
                }

                if (limit < 1 || limit > 20)
                {
                    return BadRequest(new ApiResponse<MovieListResponse>
                    {
                        Success = false,
                        Message = "Limit must be between 1 and 20"
                    });
                }

                var result = await _movieApiService.GetRelatedMoviesAsync(slug, limit, version);

                return Ok(new ApiResponse<MovieListResponse>
                {
                    Success = true,
                    Message = "Related movies retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting related movies for slug: {slug}");
                return StatusCode(500, new ApiResponse<MovieListResponse>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }
    }
}