using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAppApiPhim.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thêm HttpClient Factory
builder.Services.AddHttpClient();

// Thêm Memory Cache
builder.Services.AddMemoryCache();

// Đăng ký MovieApiService
builder.Services.AddScoped<IMovieApiService, MovieApiService>();

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Cấu hình HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Sử dụng CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();