using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace WebAppApiPhim.Middleware.Enhanced
{
    public class EnhancedPerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedPerformanceMiddleware> _logger;
        private readonly DiagnosticSource _diagnosticSource;

        public EnhancedPerformanceMiddleware(
            RequestDelegate next,
            ILogger<EnhancedPerformanceMiddleware> logger,
            DiagnosticSource diagnosticSource)
        {
            _next = next;
            _logger = logger;
            _diagnosticSource = diagnosticSource;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestPath = context.Request.Path.Value;
            var requestMethod = context.Request.Method;
            var requestId = context.TraceIdentifier;

            // Add performance headers
            context.Response.OnStarting(() =>
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";
                    context.Response.Headers["X-Request-ID"] = requestId;
                    context.Response.Headers["X-Server-Time"] = DateTimeOffset.UtcNow.ToString("O");
                }
                return Task.CompletedTask;
            });

            // Start activity for distributed tracing
            using var activity = Activity.Current?.Source.StartActivity($"{requestMethod} {requestPath}");
            activity?.SetTag("http.method", requestMethod);
            activity?.SetTag("http.url", context.Request.GetDisplayUrl());
            activity?.SetTag("request.id", requestId);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log performance metrics
                LogPerformanceMetrics(requestMethod, requestPath, elapsedMs,
                    context.Response.StatusCode, requestId);

                // Send telemetry
                if (_diagnosticSource.IsEnabled("WebAppApiPhim.Performance"))
                {
                    _diagnosticSource.Write("WebAppApiPhim.Performance", new
                    {
                        RequestId = requestId,
                        Method = requestMethod,
                        Path = requestPath,
                        StatusCode = context.Response.StatusCode,
                        ElapsedMilliseconds = elapsedMs,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }

                activity?.SetTag("http.status_code", context.Response.StatusCode.ToString());
                activity?.SetTag("duration_ms", elapsedMs.ToString());
            }
        }

        private void LogPerformanceMetrics(string method, string path, long elapsedMs,
            int statusCode, string requestId)
        {
            var logLevel = GetLogLevel(elapsedMs, statusCode);
            var category = GetPerformanceCategory(elapsedMs);

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["Method"] = method,
                ["Path"] = path,
                ["StatusCode"] = statusCode,
                ["ElapsedMs"] = elapsedMs,
                ["Category"] = category
            });

            _logger.Log(logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms [{Category}]",
                method, path, statusCode, elapsedMs, category);

            // Log slow requests with additional context
            if (elapsedMs > 5000)
            {
                _logger.LogWarning(
                    "SLOW REQUEST DETECTED: {Method} {Path} took {ElapsedMs}ms - RequestId: {RequestId}",
                    method, path, elapsedMs, requestId);
            }
        }

        private LogLevel GetLogLevel(long elapsedMs, int statusCode)
        {
            if (statusCode >= 500) return LogLevel.Error;
            if (statusCode >= 400) return LogLevel.Warning;
            if (elapsedMs > 10000) return LogLevel.Error;
            if (elapsedMs > 5000) return LogLevel.Warning;
            if (elapsedMs > 2000) return LogLevel.Information;
            return LogLevel.Debug;
        }

        private string GetPerformanceCategory(long elapsedMs) => elapsedMs switch
        {
            <= 100 => "Excellent",
            <= 500 => "Good",
            <= 1000 => "Acceptable",
            <= 2000 => "Slow",
            <= 5000 => "Very Slow",
            _ => "Critical"
        };
    }
}
