using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAppApiPhim.Services;
using System;
using WebAppApiPhim.Repositories;
using WebAppApiPhim.Data; // namespace chứa ApplicationDbContext

using Microsoft.EntityFrameworkCore;
using WebAppApiPhim; // nếu dùng EF Core

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thêm HttpClient Factory
builder.Services.AddHttpClient();

// Thêm Memory Cache
builder.Services.AddMemoryCache();

// Đăng ký dịch vụ
builder.Services.AddScoped<IMovieApiService, MovieApiService>();
builder.Services.AddScoped<IMetadataRepository, MetadataRepository>();

// Thêm DbContext (ví dụ sử dụng SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("http://localhost:3000") // Frontend URL
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // If you need cookies or credentials
    });
});


var app = builder.Build();

// Seed dữ liệu sau khi app được build
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(context);
}

// Cấu hình HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowFrontend"); // Replace "AllowAll" with "AllowFrontend"

app.UseAuthorization();

app.MapControllers();

app.Run();
