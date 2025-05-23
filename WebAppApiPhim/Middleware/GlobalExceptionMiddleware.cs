using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started, cannot modify headers. Exception: {Exception}", exception);
                return;
            }

            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Data = null
            };

            switch (exception)
            {
                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid argument provided";
                    response.Errors = argEx.Message;
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    break;

                case TimeoutException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timeout";
                    break;

                case HttpRequestException httpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                    response.Message = "External service error";
                    response.Errors = "Unable to connect to external service";
                    break;

                case TaskCanceledException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request was cancelled or timed out";
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An internal server error occurred";

                    // Only include detailed error information in development
                    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                    {
                        response.Errors = new
                        {
                            Message = exception.Message,
                            StackTrace = exception.StackTrace,
                            InnerException = exception.InnerException?.Message
                        };
                    }
                    else
                    {
                        response.Errors = "Please contact support if the problem persists";
                    }
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
