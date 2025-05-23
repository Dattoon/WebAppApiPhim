using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetadataController : ControllerBase
    {
        private readonly IMovieApiService _movieApiService;
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(
            IMovieApiService movieApiService,
            ILogger<MetadataController> logger)
        {
            _movieApiService = movieApiService;
            _logger = logger;
        }

        /// <summary>
        /// Get all genres
        /// </summary>
        [HttpGet("genres")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetGenres()
        {
            try
            {
                var genres = await _movieApiService.GetGenresAsync();

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Genres retrieved successfully",
                    Data = genres
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genres");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all countries
        /// </summary>
        [HttpGet("countries")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetCountries()
        {
            try
            {
                var countries = await _movieApiService.GetCountriesAsync();

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Countries retrieved successfully",
                    Data = countries
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all years
        /// </summary>
        [HttpGet("years")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetYears()
        {
            try
            {
                var years = await _movieApiService.GetYearsAsync();

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Years retrieved successfully",
                    Data = years
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting years");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all movie types
        /// </summary>
        [HttpGet("types")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetMovieTypes()
        {
            try
            {
                var types = await _movieApiService.GetMovieTypesAsync();

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Movie types retrieved successfully",
                    Data = types
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movie types");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all metadata in one request
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<MetadataResponse>>> GetAllMetadata()
        {
            try
            {
                var genres = await _movieApiService.GetGenresAsync();
                var countries = await _movieApiService.GetCountriesAsync();
                var years = await _movieApiService.GetYearsAsync();
                var types = await _movieApiService.GetMovieTypesAsync();

                var metadata = new MetadataResponse
                {
                    Genres = genres,
                    Countries = countries,
                    Years = years,
                    Types = types
                };

                return Ok(new ApiResponse<MetadataResponse>
                {
                    Success = true,
                    Message = "All metadata retrieved successfully",
                    Data = metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all metadata");
                return StatusCode(500, new ApiResponse<MetadataResponse>
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = ex.Message
                });
            }
        }
    }

    public class MetadataResponse
    {
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Countries { get; set; } = new List<string>();
        public List<string> Years { get; set; } = new List<string>();
        public List<string> Types { get; set; } = new List<string>();
    }
}
