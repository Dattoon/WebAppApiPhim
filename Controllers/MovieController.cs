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
                        Genres = movieDetail.Genres ?? (string.IsNullOrEmpty(movieDetail.Categories)
                            ? new List<string>()
                            : movieDetail.Categories.Split(',').Select(c => c.Trim()).ToList()),
                        Country = movieDetail.Country ?? movieDetail.Countries,
                        PosterUrl = movieDetail.PosterUrl ?? movieDetail.ThumbUrl ?? movieDetail.SubPoster,
                        BackdropUrl = movieDetail.BackdropUrl ?? movieDetail.ThumbUrl ?? movieDetail.SubThumb,
                        Rating = movieDetail.Rating,
                        // Các trường không có trong MovieDetailResponse, sử dụng giá trị mặc định
                        Director = "",
                        Actors = "",
                        Duration = "",
                        Quality = "",
                        Language = ""
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
                        Genres = movieDetail.Genres ?? (string.IsNullOrEmpty(movieDetail.Categories)
                            ? new List<string>()
                            : movieDetail.Categories.Split(',').Select(c => c.Trim()).ToList()),
                        Country = movieDetail.Country ?? movieDetail.Countries,
                        PosterUrl = movieDetail.PosterUrl ?? movieDetail.ThumbUrl ?? movieDetail.SubPoster,
                        BackdropUrl = movieDetail.BackdropUrl ?? movieDetail.ThumbUrl ?? movieDetail.SubThumb,
                        Rating = movieDetail.Rating,
                        // Các trường không có trong MovieDetailResponse, sử dụng giá trị mặc định
                        Director = "",
                        Actors = "",
                        Duration = "",
                        Quality = "",
                        Language = ""
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
