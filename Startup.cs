using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAppApiPhim.Services;

namespace WebAppApiPhim
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Thêm Memory Cache
            services.AddMemoryCache();

            // Cấu hình HttpClient
            services.AddHttpClient<IMovieApiService, MovieApiService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // Đăng ký các dịch vụ
            services.AddScoped<IMovieApiService, MovieApiService>();

            // Thêm Response Compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Sử dụng Response Compression
            app.UseResponseCompression();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
