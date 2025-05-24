using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();

                if (healthReport.Status == HealthStatus.Healthy)
                {
                    return Ok(healthReport);
                }
                else
                {
                    return StatusCode(503, healthReport);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                return StatusCode(500, "Internal server error during health check");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { Status = "OK", Time = DateTime.UtcNow });
        }
    }
}
