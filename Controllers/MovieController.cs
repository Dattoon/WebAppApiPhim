using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebAppApiPhim.Models;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieApiService _movieApiService;

        public MovieController(IMovieApiService movieApiService)
        {
            _movieApiService = movieApiService;
        }

        public async Task<IActionResult> Detail(string slug)
        {
            try
            {
                if (string.IsNullOrEmpty(slug))
                {
                    return NotFound();
                }

                var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);

                if (movieDetail == null)
                {
                    return NotFound();
                }

                // Lấy phim liên quan
                var relatedMovies = await _movieApiService.GetRelatedMoviesAsync(slug, 6);

                // Tạo view model
                var viewModel = new MovieDetailViewModel
                {
                    Movie = new MovieDetail
                    {
                        Id = movieDetail.Id,
                        Name = movieDetail.Name,
                        OriginalName = movieDetail.OriginalName ?? movieDetail.OriginName,
                        Slug = movieDetail.Slug ?? slug,
                        Year = movieDetail.Year,
                        Description = movieDetail.Description ?? movieDetail.Content,
                        Type = movieDetail.Type,
                        Status = movieDetail.Status,
                        Genres = GetGenresFromMovieDetail(movieDetail),
                        Country = movieDetail.Country ?? movieDetail.Countries,
                        PosterUrl = GetPosterUrl(movieDetail),
                        BackdropUrl = GetBackdropUrl(movieDetail),
                        Rating = movieDetail.Tmdb_vote_average,
                        // Các trường không có trong MovieDetailResponse, sử dụng giá trị mặc định
                        Director = movieDetail.Director ?? movieDetail.Directors ?? "",
                        Actors = movieDetail.Casts ?? movieDetail.Actors ?? "",
                        Duration = movieDetail.Time ?? "",
                        Quality = movieDetail.Quality ?? "",
                        Language = movieDetail.Language ?? movieDetail.Lang ?? ""
                    },
                    RelatedMovies = relatedMovies?.Data ?? new List<MovieItem>(),
                    Comments = new List<CommentViewModel>()
                };

                // Xử lý episodes
                if (movieDetail.Episodes != null && movieDetail.Episodes.Any())
                {
                    viewModel.Episodes = ParseEpisodes(movieDetail.Episodes);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { RequestId = ex.Message });
            }
        }

        public async Task<IActionResult> Watch(string slug, string episode = null)
        {
            try
            {
                if (string.IsNullOrEmpty(slug))
                {
                    return NotFound();
                }

                var movieDetail = await _movieApiService.GetMovieDetailBySlugAsync(slug);

                if (movieDetail == null)
                {
                    return NotFound();
                }

                // Tạo view model
                var viewModel = new MovieDetailViewModel
                {
                    Movie = new MovieDetail
                    {
                        Id = movieDetail.Id,
                        Name = movieDetail.Name,
                        OriginalName = movieDetail.OriginalName ?? movieDetail.OriginName,
                        Slug = movieDetail.Slug ?? slug,
                        Year = movieDetail.Year,
                        Description = movieDetail.Description ?? movieDetail.Content,
                        Type = movieDetail.Type,
                        Status = movieDetail.Status,
                        Genres = GetGenresFromMovieDetail(movieDetail),
                        Country = movieDetail.Country ?? movieDetail.Countries,
                        PosterUrl = GetPosterUrl(movieDetail),
                        BackdropUrl = GetBackdropUrl(movieDetail),
                        Rating = movieDetail.Tmdb_vote_average,
                        // Các trường không có trong MovieDetailResponse, sử dụng giá trị mặc định
                        Director = movieDetail.Director ?? movieDetail.Directors ?? "",
                        Actors = movieDetail.Casts ?? movieDetail.Actors ?? "",
                        Duration = movieDetail.Time ?? "",
                        Quality = movieDetail.Quality ?? "",
                        Language = movieDetail.Language ?? movieDetail.Lang ?? ""
                    },
                    Comments = new List<CommentViewModel>()
                };

                // Xử lý episodes
                if (movieDetail.Episodes != null && movieDetail.Episodes.Any())
                {
                    viewModel.Episodes = ParseEpisodes(movieDetail.Episodes);

                    // Nếu không có episode được chỉ định, mặc định là tập đầu tiên
                    if (string.IsNullOrEmpty(episode) && viewModel.Episodes.Any())
                    {
                        episode = viewModel.Episodes.First().Slug;
                    }
                }

                ViewBag.CurrentEpisode = episode;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { RequestId = ex.Message });
            }
        }

        private List<string> GetGenresFromMovieDetail(MovieDetailResponse movieDetail)
        {
            // Nếu có Genres dạng string, chuyển thành List<string>
            if (!string.IsNullOrEmpty(movieDetail.Genres))
            {
                return movieDetail.Genres.Split(',').Select(g => g.Trim()).ToList();
            }

            // Nếu có Categories, chuyển thành List<string>
            if (!string.IsNullOrEmpty(movieDetail.Categories))
            {
                return movieDetail.Categories.Split(',').Select(c => c.Trim()).ToList();
            }

            // Trả về danh sách rỗng nếu không có thông tin
            return new List<string>();
        }

        private string GetPosterUrl(MovieDetailResponse movieDetail)
        {
            // Thứ tự ưu tiên: Poster_url > Thumb_url > Sub_poster
            if (!string.IsNullOrEmpty(movieDetail.Poster_url))
                return movieDetail.Poster_url;

            if (!string.IsNullOrEmpty(movieDetail.Thumb_url))
                return movieDetail.Thumb_url;

            if (!string.IsNullOrEmpty(movieDetail.Sub_poster))
                return movieDetail.Sub_poster;

            return "/placeholder.svg?height=450&width=300";
        }

        private string GetBackdropUrl(MovieDetailResponse movieDetail)
        {
            // Thứ tự ưu tiên: Backdrop > Thumb_url > Sub_thumb
            // Backdrop không có trong model, nên dùng Thumb_url hoặc Sub_thumb
            if (!string.IsNullOrEmpty(movieDetail.Thumb_url))
                return movieDetail.Thumb_url;

            if (!string.IsNullOrEmpty(movieDetail.Sub_thumb))
                return movieDetail.Sub_thumb;

            return "/placeholder.svg?height=500&width=1200";
        }

        private List<Episode> ParseEpisodes(List<object> episodes)
        {
            var result = new List<Episode>();

            foreach (var episodeObj in episodes)
            {
                if (episodeObj is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("server_name", out var _) &&
                        jsonElement.TryGetProperty("items", out var items))
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            try
                            {
                                string name = item.GetProperty("name").GetString();
                                string slug = item.GetProperty("slug").GetString();
                                string embed = item.TryGetProperty("embed", out var embedProp) ? embedProp.GetString() : null;
                                string m3u8 = item.TryGetProperty("m3u8", out var m3u8Prop) ? m3u8Prop.GetString() : null;

                                result.Add(new Episode
                                {
                                    Name = name,
                                    Slug = slug,
                                    Filename = $"Tập {name}",
                                    Link = embed ?? m3u8
                                });
                            }
                            catch (Exception)
                            {
                                // Bỏ qua nếu không thể parse
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
