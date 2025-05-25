using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.OpenApi.Models;
using WebAppApiPhim.Data;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;
using WebAppApiPhim.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using FluentValidation.AspNetCore;
using System.Threading.RateLimiting;
using HealthChecks.Redis;
using WebAppApiPhim.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var elasticsearchUrl = builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/movieapi-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "movieapi-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    })
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Configure DbContext with retry-on-failure
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: new List<int> { 1205 })));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()

    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new ArgumentException("JWT Secret is not configured.");
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourproductiondomain.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add HttpClient with Resilience
builder.Services.AddHttpClient<IMovieApiService, MovieApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.dulieuphim.ink/");
})
.AddResilienceHandler("MovieApiResilience", config =>
{
    config.AddRetry(new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential
    })
    .AddCircuitBreaker(new()
    {
        FailureRatio = 0.5,
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(30)
    });
});

// Add Caching
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? throw new ArgumentException("Redis connection string is not configured.");
    options.InstanceName = "MovieApi_";
});

// Register Services
builder.Services.AddScoped<IMetadataService, MetadataService>(); // ✅ Required

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStreamingService, StreamingService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<MovieCacheService>();

// Register Hosted Services
builder.Services.AddHostedService<SyncMoviesService>();
builder.Services.AddHostedService<MovieCacheService>();
builder.Services.AddHostedService<CacheCleanupService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "sqlserver", failureStatus: HealthStatus.Degraded, tags: new[] { "db" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis"), "redis_cache", failureStatus: HealthStatus.Degraded, tags: new[] { "cache" })
    .AddUrlGroup(new Uri(builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200"), "elasticsearch", failureStatus: HealthStatus.Degraded, tags: new[] { "search" });

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Movie API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] {}
        }
    });
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("default", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:DefaultLimit", 100),
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("search", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:SearchLimit", 30),
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movie API v1"));
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.UseRateLimiter();

app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

// Load thêm file appsettings.enhanced.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("appsettings.enhanced.json", optional: true)
    .AddEnvironmentVariables(); // dùng cho docker hoặc môi trường cài biến ENV

app.MapControllers();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await context.Database.MigrateAsync();
        await DbSeeder.Initialize(services, context, userManager, roleManager); // Sửa thành DbSeeder
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();