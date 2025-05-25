using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    // Services/IMetadataService.cs
    public interface IMetadataService
    {
        Task<bool> AddGenreAsync(string name, string slug);
        Task<bool> AddCountryAsync(string name, string code, string slug);
        Task<bool> AddMovieTypeAsync(string name, string slug);
        Task UpdateMetadataAsync(MovieDetailResponse movie, string slug);
        Task<string> GetMetadataAsync(string key);
    }
}
