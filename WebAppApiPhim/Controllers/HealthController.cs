using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Threading.Tasks;

namespace WebAppApiPhim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Get application health status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = healthReport.Status.ToString(),
                totalDuration = healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    exception = entry.Value.Exception?.Message
                })
            };

            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => 200,
                HealthStatus.Degraded => 200,
                HealthStatus.Unhealthy => 503,
                _ => 500
            };

            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// Simple health check endpoint for load balancers
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "healthy", timestamp = System.DateTime.UtcNow });
        }
    }
}
