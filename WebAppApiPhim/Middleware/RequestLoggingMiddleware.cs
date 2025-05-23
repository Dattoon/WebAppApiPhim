using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebAppApiPhim.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            // Add request ID to response headers
            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-Request-ID"] = requestId;
            }


            // Log request
            await LogRequestAsync(context, requestId);

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. RequestId: {RequestId}", requestId);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            try
            {
                var request = context.Request;
                var clientIp = GetClientIpAddress(context);
                var userAgent = request.Headers["User-Agent"].ToString();
                var userId = context.User?.Identity?.Name ?? "Anonymous";

                _logger.LogInformation(
                    "HTTP Request - RequestId: {RequestId}, Method: {Method}, Path: {Path}, QueryString: {QueryString}, " +
                    "ClientIP: {ClientIP}, UserAgent: {UserAgent}, UserId: {UserId}",
                    requestId, request.Method, request.Path, request.QueryString,
                    clientIp, userAgent, userId);

                // Log request body for POST/PUT requests (be careful with sensitive data)
                if (request.Method == "POST" || request.Method == "PUT")
                {
                    if (request.ContentLength.HasValue && request.ContentLength.Value > 0 && request.ContentLength.Value < 1024 * 10) // Max 10KB
                    {
                        request.EnableBuffering();
                        var buffer = new byte[request.ContentLength.Value];
                        await request.Body.ReadAsync(buffer, 0, buffer.Length);
                        var requestBody = Encoding.UTF8.GetString(buffer);

                        // Don't log sensitive information
                        if (!ContainsSensitiveData(requestBody))
                        {
                            _logger.LogDebug("Request Body - RequestId: {RequestId}, Body: {Body}", requestId, requestBody);
                        }

                        request.Body.Position = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request. RequestId: {RequestId}", requestId);
            }
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs)
        {
            try
            {
                var response = context.Response;

                _logger.LogInformation(
                    "HTTP Response - RequestId: {RequestId}, StatusCode: {StatusCode}, " +
                    "ContentLength: {ContentLength}, ElapsedMs: {ElapsedMs}",
                    requestId, response.StatusCode, response.ContentLength, elapsedMs);

                // Log slow requests
                if (elapsedMs > 5000) // 5 seconds
                {
                    _logger.LogWarning(
                        "Slow Request Detected - RequestId: {RequestId}, ElapsedMs: {ElapsedMs}, " +
                        "Path: {Path}, Method: {Method}",
                        requestId, elapsedMs, context.Request.Path, context.Request.Method);
                }

                // Log response body for errors (be careful with sensitive data)
                if (response.StatusCode >= 400)
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                    context.Response.Body.Seek(0, SeekOrigin.Begin);

                    if (!string.IsNullOrEmpty(responseBody) && responseBody.Length < 1024 * 5) // Max 5KB
                    {
                        _logger.LogWarning(
                            "Error Response - RequestId: {RequestId}, StatusCode: {StatusCode}, Body: {Body}",
                            requestId, response.StatusCode, responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging response. RequestId: {RequestId}", requestId);
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            var realIp = context.Request.Headers["X-Real-IP"].ToString();

            if (!string.IsNullOrEmpty(forwardedFor))
            {
                ipAddress = forwardedFor.Split(',')[0].Trim();
            }
            else if (!string.IsNullOrEmpty(realIp))
            {
                ipAddress = realIp;
            }

            return ipAddress ?? "Unknown";
        }

        private bool ContainsSensitiveData(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            var sensitiveKeywords = new[] { "password", "token", "secret", "key", "authorization" };
            var lowerContent = content.ToLower();

            foreach (var keyword in sensitiveKeywords)
            {
                if (lowerContent.Contains(keyword))
                    return true;
            }

            return false;
        }
    }
}
