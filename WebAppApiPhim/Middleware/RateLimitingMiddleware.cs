using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAppApiPhim.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // Rate limiting configuration
        private readonly int _requestLimit = 100; // requests per window
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1); // 1 minute window
        private readonly int _burstLimit = 10; // burst requests per 10 seconds
        private readonly TimeSpan _burstWindow = TimeSpan.FromSeconds(10);

        public RateLimitingMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);

            // Check rate limits
            if (await IsRateLimitExceededAsync(clientId))
            {
                await HandleRateLimitExceeded(context);
                return;
            }

            // Update request count
            await UpdateRequestCountAsync(clientId);

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Try to get user ID first (for authenticated users)
            var userId = context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();

            if (!string.IsNullOrEmpty(forwardedFor))
            {
                ipAddress = forwardedFor.Split(',')[0].Trim();
            }

            return $"ip:{ipAddress}";
        }

        private async Task<bool> IsRateLimitExceededAsync(string clientId)
        {
            var normalKey = $"rate_limit:{clientId}";
            var burstKey = $"burst_limit:{clientId}";

            // Check burst limit (short term)
            var burstCount = _cache.Get<int?>(burstKey) ?? 0;
            if (burstCount >= _burstLimit)
            {
                _logger.LogWarning($"Burst rate limit exceeded for client: {clientId}");
                return true;
            }

            // Check normal rate limit (longer term)
            var normalCount = _cache.Get<int?>(normalKey) ?? 0;
            if (normalCount >= _requestLimit)
            {
                _logger.LogWarning($"Rate limit exceeded for client: {clientId}");
                return true;
            }

            return false;
        }

        private async Task UpdateRequestCountAsync(string clientId)
        {
            var normalKey = $"rate_limit:{clientId}";
            var burstKey = $"burst_limit:{clientId}";

            // Update normal rate limit counter with size
            var normalCount = _cache.Get<int?>(normalKey) ?? 0;
            var normalOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_timeWindow)
                .SetSize(1); // Set size for the cache entry
            _cache.Set(normalKey, normalCount + 1, normalOptions);

            // Update burst rate limit counter with size
            var burstCount = _cache.Get<int?>(burstKey) ?? 0;
            var burstOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_burstWindow)
                .SetSize(1); // Set size for the cache entry
            _cache.Set(burstKey, burstCount + 1, burstOptions);
        }

        private async Task HandleRateLimitExceeded(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Rate limit exceeded",
                message = "Too many requests. Please try again later.",
                retryAfter = _timeWindow.TotalSeconds
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
