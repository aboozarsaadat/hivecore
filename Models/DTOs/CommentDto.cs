namespace HiveCore.Models.DTOs;

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;

    public Guid PostId { get; set; }
    public UserSummaryDto Author { get; set; } = null!;

    public Guid? ParentCommentId { get; set; }
    public List<CommentDto> Replies { get; set; } = new();

    public int LikesCount { get; set; }
    public int RepliesCount { get; set; }

    public bool IsDeleted { get; set; }
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsLikedByCurrentUser { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public string? RelativeTime { get; set; }
}