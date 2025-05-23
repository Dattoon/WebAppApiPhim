using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get user profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserProfileViewModel>>> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<UserProfileViewModel>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var favoriteCount = await _context.UserFavorites.CountAsync(f => f.UserId == userId);
                var ratingCount = await _context.UserRatings.CountAsync(r => r.UserId == userId);
                var commentCount = await _context.UserComments.CountAsync(c => c.UserId == userId);

                var profile = new UserProfileViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    FavoriteCount = favoriteCount,
                    RatingCount = ratingCount,
                    CommentCount = commentCount
                };

                return Ok(new ApiResponse<UserProfileViewModel>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = profile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new ApiResponse<UserProfileViewModel>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user favorites
        /// </summary>
        [HttpGet("favorites")]
        public async Task<ActionResult<ApiResponse<List<MovieListItemViewModel>>>> GetFavorites(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (page < 1) page = 1;
                if (limit < 1 || limit > 50) limit = 20;

                var favorites = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Join(_context.CachedMovies,
                        f => f.MovieSlug,
                        m => m.Slug,
                        (f, m) => new MovieListItemViewModel
                        {
                            Slug = m.Slug,
                            Name = m.Name,
                            OriginalName = m.OriginalName,
                            Year = m.Year,
                            ThumbUrl = m.ThumbUrl,
                            PosterUrl = m.PosterUrl,
                            Type = m.Type,
                            Quality = m.Quality,
                            ViewCount = m.ViewCount,
                            AverageRating = m.Statistic != null ? m.Statistic.AverageRating : 0,
                            IsFavorite = true,
                            WatchedPercentage = null
                        })
                    .OrderByDescending(f => f.ViewCount)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new ApiResponse<List<MovieListItemViewModel>>
                {
                    Success = true,
                    Message = "Favorites retrieved successfully",
                    Data = favorites
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user favorites");
                return StatusCode(500, new ApiResponse<List<MovieListItemViewModel>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Add movie to favorites
        /// </summary>
        [HttpPost("favorites/{slug}")]
        public async Task<ActionResult<ApiResponse<object>>> AddToFavorites(string slug)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == slug);

                if (existingFavorite != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Movie is already in favorites"
                    });
                }

                var favorite = new UserFavorite
                {
                    UserId = userId,
                    MovieSlug = slug,
                    CreatedAt = DateTime.Now
                };

                _context.UserFavorites.Add(favorite);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Movie added to favorites successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding movie {slug} to favorites");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Remove movie from favorites
        /// </summary>
        [HttpDelete("favorites/{slug}")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveFromFavorites(string slug)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieSlug == slug);

                if (favorite == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Movie not found in favorites"
                    });
                }

                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Movie removed from favorites successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing movie {slug} from favorites");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Rate a movie
        /// </summary>
        [HttpPost("ratings/{slug}")]
        public async Task<ActionResult<ApiResponse<object>>> RateMovie(
            string slug,
            [FromBody] RateMovieRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (request.Rating < 1 || request.Rating > 10)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Rating must be between 1 and 10"
                    });
                }

                var existingRating = await _context.UserRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieSlug == slug);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Rating = request.Rating;
                    existingRating.Comment = request.Comment;
                    existingRating.UpdatedAt = DateTime.Now;
                    _context.UserRatings.Update(existingRating);
                }
                else
                {
                    // Create new rating
                    var rating = new UserRating
                    {
                        UserId = userId,
                        MovieSlug = slug,
                        Rating = request.Rating,
                        Comment = request.Comment,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.UserRatings.Add(rating);
                }

                await _context.SaveChangesAsync();

                // Update movie statistics asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateMovieRatingStatisticsAsync(slug);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error updating rating statistics for movie {slug}");
                    }
                });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Movie rated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rating movie {slug}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get watch history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<EpisodeProgress>>>> GetWatchHistory(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (page < 1) page = 1;
                if (limit < 1 || limit > 50) limit = 20;

                var history = await _context.EpisodeProgresses
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.UpdatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new ApiResponse<List<EpisodeProgress>>
                {
                    Success = true,
                    Message = "Watch history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watch history");
                return StatusCode(500, new ApiResponse<List<EpisodeProgress>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        private async Task UpdateMovieRatingStatisticsAsync(string movieSlug)
        {
            var ratings = await _context.UserRatings
                .Where(r => r.MovieSlug == movieSlug)
                .ToListAsync();

            if (ratings.Any())
            {
                var averageRating = ratings.Average(r => r.Rating);
                var ratingCount = ratings.Count;

                var statistic = await _context.MovieStatistics
                    .FirstOrDefaultAsync(s => s.MovieSlug == movieSlug);

                if (statistic != null)
                {
                    statistic.AverageRating = averageRating;
                    statistic.RatingCount = ratingCount;
                    statistic.LastUpdated = DateTime.Now;
                    _context.MovieStatistics.Update(statistic);
                }
                else
                {
                    statistic = new MovieStatistic
                    {
                        MovieSlug = movieSlug,
                        ViewCount = 0,
                        FavoriteCount = 0,
                        CommentCount = 0,
                        AverageRating = averageRating,
                        RatingCount = ratingCount,
                        LastUpdated = DateTime.Now
                    };
                    _context.MovieStatistics.Add(statistic);
                }

                await _context.SaveChangesAsync();
            }
        }
    }

    // Request models
    public class RateMovieRequest
    {
        public double Rating { get; set; }
        public string Comment { get; set; }
    }
}
