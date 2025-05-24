using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System.Threading.RateLimiting;
using FluentValidation;
using Serilog;
using Microsoft.AspNetCore.ResponseCompression;
using Polly;
using Polly.Extensions.Http;
using WebAppApiPhim.Services.Enhanced;
using WebAppApiPhim.Services;
using WebAppApiPhim.Controllers.Enhanced;
using WebAppApiPhim.HealthChecks;
using WebAppApiPhim.BackgroundServices;

namespace WebAppApiPhim.Configuration
{
    public static class EnhancedStartupExtensions
    {
        public static IServiceCollection AddEnhancedServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Enhanced caching with Redis
            if (!string.IsNullOrEmpty(configuration.GetConnectionString("Redis")))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = configuration.GetConnectionString("Redis");
                    options.InstanceName = "MovieApi";
                });
            }

            // Rate limiting
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("DefaultPolicy", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 50;
                });

                options.AddFixedWindowLimiter("SearchPolicy", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 30;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                });
            });

            // Response compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            // Enhanced HTTP client with Polly
            services.AddHttpClient<IMovieApiService, MovieApiService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "MovieApi/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

            // Validation
            services.AddValidatorsFromAssemblyContaining<MovieSearchRequestValidator>();

            // Image optimization
            services.AddScoped<IImageOptimizationService, ImageOptimizationService>();

            // Enhanced logging with Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "MovieApi")
                .WriteTo.Console()
                .WriteTo.File("logs/movieapi-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddSerilog();

            // Health checks with detailed monitoring
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database")
                .AddCheck<ExternalApiHealthCheck>("external_api")
                .AddCheck<RedisHealthCheck>("redis")
                .AddCheck<DiskSpaceHealthCheck>("disk_space");

            // Background services
            services.AddHostedService<EnhancedMovieCacheService>();
            services.AddHostedService<ImageCleanupService>();
            services.AddHostedService<PerformanceMonitoringService>();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        }
    }

    // Validator example
    public class MovieSearchRequestValidator : AbstractValidator<MovieSearchRequest>
    {
        public MovieSearchRequestValidator()
        {
            RuleFor(x => x.Query)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(100)
                .Matches(@"^[a-zA-Z0-9\s\-_àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐ]+$")
                .WithMessage("Query contains invalid characters");

            RuleFor(x => x.Page)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000);

            RuleFor(x => x.Limit)
                .GreaterThan(0)
                .LessThanOrEqualTo(100);
        }
    }
}
