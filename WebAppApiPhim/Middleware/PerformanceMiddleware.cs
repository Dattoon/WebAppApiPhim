using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WebAppApiPhim.Middleware
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;

        public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestPath = context.Request.Path.Value;
            var requestMethod = context.Request.Method;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                if (!context.Response.HasStarted)
                {
                    context.Response.Headers["X-Response-Time"] = $"{elapsedMs}ms";
                }


                // Log performance metrics
                LogPerformanceMetrics(requestMethod, requestPath, elapsedMs, context.Response.StatusCode);
            }
        }

        private void LogPerformanceMetrics(string method, string path, long elapsedMs, int statusCode)
        {
            var logLevel = GetLogLevel(elapsedMs, statusCode);

            _logger.Log(logLevel,
                "Performance Metric - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, " +
                "ElapsedMs: {ElapsedMs}, Category: {Category}",
                method, path, statusCode, elapsedMs, GetPerformanceCategory(elapsedMs));
        }

        private LogLevel GetLogLevel(long elapsedMs, int statusCode)
        {
            // Error responses
            if (statusCode >= 500)
                return LogLevel.Error;

            if (statusCode >= 400)
                return LogLevel.Warning;

            // Performance-based log levels
            if (elapsedMs > 10000) // > 10 seconds
                return LogLevel.Error;

            if (elapsedMs > 5000) // > 5 seconds
                return LogLevel.Warning;

            if (elapsedMs > 2000) // > 2 seconds
                return LogLevel.Information;

            return LogLevel.Debug;
        }

        private string GetPerformanceCategory(long elapsedMs)
        {
            if (elapsedMs <= 100)
                return "Excellent";

            if (elapsedMs <= 500)
                return "Good";

            if (elapsedMs <= 1000)
                return "Acceptable";

            if (elapsedMs <= 2000)
                return "Slow";

            if (elapsedMs <= 5000)
                return "Very Slow";

            return "Critical";
        }
    }
}
