namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a user's saved/bookmarked post.
/// </summary>
public class Bookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}