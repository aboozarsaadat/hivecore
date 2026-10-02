using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a comment on a post. Supports nested replies (one level deep recommended).
/// </summary>
public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    // ============================================================
    // Relationships
    // ============================================================
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    // Nested comments
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    // ============================================================
    // Counters
    // ============================================================
    public int LikesCount { get; set; } = 0;
    public int RepliesCount { get; set; } = 0;

    // ============================================================
    // Moderation
    // ============================================================
    public bool IsDeleted { get; set; } = false;
    public bool IsHidden { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ============================================================
    // Navigation
    // ============================================================
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}