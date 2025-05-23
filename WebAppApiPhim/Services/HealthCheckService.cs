using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebAppApiPhim.Data;

namespace WebAppApiPhim.Services
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(ApplicationDbContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple database connectivity check
                await _context.Database.CanConnectAsync(cancellationToken);

                // Check if we can query the database
                var movieCount = await _context.CachedMovies.CountAsync(cancellationToken);

                return HealthCheckResult.Healthy($"Database is healthy. Movies in cache: {movieCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
            }
        }
    }

    public class ExternalApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiHealthCheck> _logger;
        private readonly string _apiUrl = "https://api.dulieuphim.ink";

        public ExternalApiHealthCheck(HttpClient httpClient, ILogger<ExternalApiHealthCheck> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout

                var response = await _httpClient.GetAsync($"{_apiUrl}/phim-moi/v1?page=1&limit=1", cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("External API is healthy");
                }
                else
                {
                    return HealthCheckResult.Degraded($"External API returned status code: {response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("External API health check timed out");
                return HealthCheckResult.Degraded("External API health check timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External API health check failed");
                return HealthCheckResult.Unhealthy("External API is unhealthy", ex);
            }
        }
    }

    public class MemoryCacheHealthCheck : IHealthCheck
    {
        private readonly ILogger<MemoryCacheHealthCheck> _logger;

        public MemoryCacheHealthCheck(ILogger<MemoryCacheHealthCheck> logger)
        {
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get memory usage
                var workingSet = GC.GetTotalMemory(false);
                var workingSetMB = workingSet / 1024 / 1024;

                if (workingSetMB > 1000) // > 1GB
                {
                    return Task.FromResult(HealthCheckResult.Degraded($"High memory usage: {workingSetMB}MB"));
                }

                return Task.FromResult(HealthCheckResult.Healthy($"Memory usage is normal: {workingSetMB}MB"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory cache health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy("Memory cache health check failed", ex));
            }
        }
    }
}
