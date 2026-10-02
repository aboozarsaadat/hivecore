namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a like on a post or a comment.
/// Exactly one of PostId or CommentId must be set.
/// </summary>
public class Like
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? PostId { get; set; }
    public Post? Post { get; set; }

    public Guid? CommentId { get; set; }
    public Comment? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}