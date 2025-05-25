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

                var response = new
                {
                    status = healthReport.Status.ToString(),
                    checks = healthReport.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description ?? "none",
                        error = e.Value.Exception?.Message ?? "none",
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                };

                if (healthReport.Status == HealthStatus.Healthy)
                {
                    return Ok(response);
                }

                return StatusCode(503, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                return StatusCode(500, new
                {
                    message = "Internal server error during health check",
                    exception = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }


        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { Status = "OK", Time = DateTime.UtcNow });
        }
    }
}
