using System.ComponentModel.DataAnnotations;

namespace WebAppApiPhim.Models
{
    public class CommentWithRepliesViewModel
    {
        public UserComment Comment { get; set; }
        public List<UserComment> Replies { get; set; } = new List<UserComment>();
    }

    public class CommentCreateRequest
    {
        [Required]
        [StringLength(100)]
        public string MovieSlug { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }
    }

    public class CommentReplyRequest
    {
        [Required]
        [StringLength(50)]
        public string ParentCommentId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }
    }

    public class CommentUpdateRequest
    {
        [Required]
        [StringLength(1000)]
        public string Content { get; set; }
    }

    public class CommentViewModel
    {
        public string Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string MovieSlug { get; set; }
        public string Content { get; set; }
        public string DisplayContent { get; set; } // Content without reply formatting
        public DateTime CreatedAt { get; set; }
        public bool IsReply { get; set; }
        public string ParentCommentId { get; set; }
        public string ReplyToUserName { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}