var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
if (!Directory.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot")))
{
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot"));
}
app.UseCors("AllowAll");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.MapFallbackToFile("/index.html");

app.Run();
