using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAppApiPhim.Services;

namespace WebAppApiPhim.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly IUserService _userService;

        public CommentsController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{movieSlug}")]
        public async Task<IActionResult> GetCommentsByMovie(
            string movieSlug,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(movieSlug))
                return BadRequest("Movie slug is required");

            var comments = await _userService.GetCommentsByMovieAsync(movieSlug, page, pageSize);
            var totalItems = await _userService.GetCommentsCountByMovieAsync(movieSlug);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var commentViewModels = comments.Select(c => new
            {
                id = c.Id,
                content = c.Content,
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt,
                user = new
                {
                    id = c.User.Id,
                    username = c.User.UserName,
                    displayName = c.User.DisplayName,
                    avatarUrl = c.User.AvatarUrl
                }
            }).ToList();

            return Ok(new
            {
                items = commentViewModels,
                pagination = new
                {
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    pageSize = pageSize
                }
            });
        }
    }
}
