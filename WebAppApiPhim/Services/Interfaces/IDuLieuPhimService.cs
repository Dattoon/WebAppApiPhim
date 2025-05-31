using WebAppApiPhim.Models.DuLieuPhim;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IDuLieuPhimService
    {
        // Lấy danh sách phim mới nhất
        Task<MovieListResponse> GetLatestMoviesAsync(int page = 1, int limit = 10, string version = "v1");

        // Lấy chi tiết phim theo slug
        Task<MovieDetailResponse> GetMovieDetailBySlugAsync(string slug, string version = "v1");

        // Lấy thông tin TMDB theo slug
        Task<TmdbResponse> GetTmdbBySlugAsync(string slug);

        // Lấy thông tin diễn viên theo slug
        Task<ActorResponse> GetActorsBySlugAsync(string slug);

        // Lấy thông tin nhà sản xuất và nền tảng phát hành theo slug
        Task<ProductionResponse> GetProductionBySlugAsync(string slug);

        // Lấy ảnh theo slug
        Task<ImageResponse> GetImagesBySlugAsync(string slug, string version = "v1");

        // Lọc phim theo các tiêu chí
        Task<MovieListResponse> FilterMoviesAsync(
            string? name = null,
            string? type = null,
            string? genre = null,
            string? country = null,
            string? year = null,
            int page = 1,
            int limit = 10);
    }
}