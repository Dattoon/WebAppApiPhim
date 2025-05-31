using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetadataController : ControllerBase
    {
        private readonly IMetadataService _metadataService;
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(IMetadataService metadataService, ILogger<MetadataController> logger)
        {
            _metadataService = metadataService;
            _logger = logger;
        }

        /// <summary>
        /// Get all filter options for frontend
        /// </summary>
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var genres = await _metadataService.GetGenresAsync();
                var countries = await _metadataService.GetCountriesAsync();
                var types = await _metadataService.GetMovieTypesAsync();

                return Ok(new
                {
                    genres = genres.Select(g => new { name = g, slug = g.ToLower().Replace(" ", "-") }),
                    countries = countries.Select(c => new { name = c, slug = c.ToLower().Replace(" ", "-") }),
                    types = types.Select(t => new { name = t, slug = t.ToLower().Replace(" ", "-") }),
                    sortOptions = new[]
                    {
                        new { value = "latest", label = "Mới nhất" },
                        new { value = "title", label = "Tên phim" },
                        new { value = "year", label = "Năm sản xuất" },
                        new { value = "views", label = "Lượt xem" }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed metadata with full entity information
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedMetadata()
        {
            try
            {
                var genres = await _metadataService.GetAllGenresAsync();
                var countries = await _metadataService.GetAllCountriesAsync();
                var types = await _metadataService.GetAllMovieTypesAsync();

                return Ok(new
                {
                    genres = genres.Select(g => new { g.Id, g.Name, g.Slug}),
                    countries = countries.Select(c => new { c.Id, c.Name, c.Code, c.Slug }),
                    types = types.Select(t => new { t.Id, t.Name, t.Slug })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed metadata");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Add new genre
        /// </summary>
        [HttpPost("genres")]
        public async Task<IActionResult> AddGenre([FromBody] GenreRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Genre name is required" });
                }

                var success = await _metadataService.AddGenreAsync(
                    request.Name, 
                    request.Slug 
                   );

                if (success)
                {
                    return Ok(new { message = "Genre added successfully" });
                }
                else
                {
                    return Conflict(new { message = "Genre already exists" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding genre");
                return StatusCode(500, new { message = "Error adding genre", error = ex.Message });
            }
        }

        /// <summary>
        /// Add new country
        /// </summary>
        [HttpPost("countries")]
        public async Task<IActionResult> AddCountry([FromBody] CountryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Country name is required" });
                }

                var success = await _metadataService.AddCountryAsync(
                    request.Name, 
                    request.Code, 
                    request.Slug);

                if (success)
                {
                    return Ok(new { message = "Country added successfully" });
                }
                else
                {
                    return Conflict(new { message = "Country already exists" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding country");
                return StatusCode(500, new { message = "Error adding country", error = ex.Message });
            }
        }

        /// <summary>
        /// Add new movie type
        /// </summary>
        [HttpPost("movie-types")]
        public async Task<IActionResult> AddMovieType([FromBody] MovieTypeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Movie type name is required" });
                }

                var success = await _metadataService.AddMovieTypeAsync(
                    request.Name, 
                    request.Slug);

                if (success)
                {
                    return Ok(new { message = "Movie type added successfully" });
                }
                else
                {
                    return Conflict(new { message = "Movie type already exists" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie type");
                return StatusCode(500, new { message = "Error adding movie type", error = ex.Message });
            }
        }
    }

    #region Request Models
    
    public class GenreRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
       
    }

    public class CountryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Slug { get; set; }
    }

    public class MovieTypeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
       
    }
    
    #endregion
}
