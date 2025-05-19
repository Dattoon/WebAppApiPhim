using WebAppApiPhim.Models;

namespace WebAppApiPhim.Data
{
    // Data/DbSeeder.cs
    public class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Seed movie types if empty
            if (!context.MovieTypes.Any())
            {
                var types = new List<MovieType>
            {
                new MovieType { Name = "Phim lẻ", ApiValue = "Phim lẻ" },
                new MovieType { Name = "Phim bộ", ApiValue = "Phim bộ" },
                new MovieType { Name = "Phim chiếu rạp", ApiValue = "Phim chiếu rạp" },
                new MovieType { Name = "Phim đang chiếu", ApiValue = "Phim đang chiếu" },
                new MovieType { Name = "TV shows", ApiValue = "TV shows" },
                new MovieType { Name = "Hoạt hình", ApiValue = "Hoạt hình" }
            };

                context.MovieTypes.AddRange(types);
                await context.SaveChangesAsync();
            }

            // Seed genres if empty
            if (!context.Genres.Any())
            {
                var genres = new List<Genre>
            {
                new Genre { Name = "Hành Động", ApiValue = "Hành Động" },
                new Genre { Name = "Tình Cảm", ApiValue = "Tình Cảm" },
                new Genre { Name = "Hài Hước", ApiValue = "Hài Hước" },
                // Add all your genres here
            };

                context.Genres.AddRange(genres);
                await context.SaveChangesAsync();
            }

            // Seed countries if empty
            if (!context.Countries.Any())
            {
                var countries = new List<Country>
            {
                new Country { Name = "Việt Nam", ApiValue = "Việt Nam", Code = "VN" },
                new Country { Name = "Trung Quốc", ApiValue = "Trung Quốc", Code = "CN" },
                new Country { Name = "Hàn Quốc", ApiValue = "Hàn Quốc", Code = "KR" },
                // Add all your countries here
            };

                context.Countries.AddRange(countries);
                await context.SaveChangesAsync();
            }
        }
    }
}
