using System.Text.RegularExpressions;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Extensions
{
    public static class CommentExtensions
    {
        public static CommentViewModel ToViewModel(this UserComment comment, string currentUserId = null)
        {
            var viewModel = new CommentViewModel
            {
                Id = comment.Id,
                UserId = comment.UserId,
                UserName = comment.User?.UserName ?? "Unknown User",
                MovieSlug = comment.MovieSlug,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                CanEdit = currentUserId != null && comment.UserId.ToString() == currentUserId,
                CanDelete = currentUserId != null && comment.UserId.ToString() == currentUserId
            };

            // Check if this is a reply
            if (comment.Content.Contains("[REPLY_TO:"))
            {
                var replyMatch = Regex.Match(comment.Content, @"@([^:]+): (.+) \[REPLY_TO:([^\]]+)\]");
                if (replyMatch.Success)
                {
                    viewModel.IsReply = true;
                    viewModel.ReplyToUserName = replyMatch.Groups[1].Value;
                    viewModel.DisplayContent = replyMatch.Groups[2].Value;
                    viewModel.ParentCommentId = replyMatch.Groups[3].Value;
                }
                else
                {
                    viewModel.DisplayContent = comment.Content;
                }
            }
            else
            {
                viewModel.IsReply = false;
                viewModel.DisplayContent = comment.Content;
            }

            return viewModel;
        }

        public static List<CommentViewModel> ToViewModelList(this IEnumerable<UserComment> comments, string currentUserId = null)
        {
            return comments.Select(c => c.ToViewModel(currentUserId)).ToList();
        }
    }
}