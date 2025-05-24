using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace WebAppApiPhim.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IDistributedCache distributedCache, ILogger<RedisHealthCheck> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var testKey = "health_check_test";
                var testValue = DateTime.UtcNow.ToString();

                // Test write
                await _distributedCache.SetStringAsync(testKey, testValue, cancellationToken);

                // Test read
                var retrievedValue = await _distributedCache.GetStringAsync(testKey, cancellationToken);

                if (retrievedValue == testValue)
                {
                    // Cleanup
                    await _distributedCache.RemoveAsync(testKey, cancellationToken);
                    return HealthCheckResult.Healthy("Redis is healthy");
                }
                else
                {
                    return HealthCheckResult.Degraded("Redis read/write test failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
            }
        }
    }
}
