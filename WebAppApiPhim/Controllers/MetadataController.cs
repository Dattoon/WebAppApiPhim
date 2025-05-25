using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebAppApiPhim.Services.Interfaces;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Restrict to admins
    public class MetadataController : ControllerBase
    {
        private readonly IMetadataService _metadataService;
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(IMetadataService metadataService, ILogger<MetadataController> logger)
        {
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("genres")]
        public async Task<IActionResult> AddGenre([FromBody] GenreRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
                {
                    return BadRequest(new { Success = false, Message = "Name and slug are required" });
                }

                var success = await _metadataService.AddGenreAsync(request.Name, request.Slug);
                if (!success)
                {
                    return StatusCode(500, new { Success = false, Message = "Failed to add genre" });
                }

                return Ok(new { Success = true, Message = "Genre added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding genre: {Name}", request.Name);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpPost("countries")]
        public async Task<IActionResult> AddCountry([FromBody] CountryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Slug))
                {
                    return BadRequest(new { Success = false, Message = "Name, code, and slug are required" });
                }

                var success = await _metadataService.AddCountryAsync(request.Name, request.Code, request.Slug);
                if (!success)
                {
                    return StatusCode(500, new { Success = false, Message = "Failed to add country" });
                }

                return Ok(new { Success = true, Message = "Country added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding country: {Name}", request.Name);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        [HttpPost("movie-types")]
        public async Task<IActionResult> AddMovieType([FromBody] MovieTypeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
                {
                    return BadRequest(new { Success = false, Message = "Name and slug are required" });
                }

                var success = await _metadataService.AddMovieTypeAsync(request.Name, request.Slug);
                if (!success)
                {
                    return StatusCode(500, new { Success = false, Message = "Failed to add movie type" });
                }

                return Ok(new { Success = true, Message = "Movie type added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie type: {Name}", request.Name);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
    }

    public class GenreRequest
    {
        public string Name { get; set; }
        public string Slug { get; set; }
    }

    public class CountryRequest
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Slug { get; set; }
    }

    public class MovieTypeRequest
    {
        public string Name { get; set; }
        public string Slug { get; set; }
    }
}