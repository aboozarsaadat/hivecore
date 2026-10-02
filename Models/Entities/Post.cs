using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a post (microblog entry) created by a user.
/// Supports text, media attachments, reposts, quotes, and polls (via extensions).
/// </summary>
public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    // ============================================================
    // Relationships
    // ============================================================
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    // Repost / Quote
    public Guid? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }
    public ICollection<Post> Replies { get; set; } = new List<Post>();

    public bool IsRepost { get; set; } = false;
    public bool IsQuote { get; set; } = false;

    // ============================================================
    // Visibility
    // ============================================================
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;

    // ============================================================
    // Moderation
    // ============================================================
    public bool IsDeleted { get; set; } = false;
    public bool IsHidden { get; set; } = false;
    public bool IsSensitive { get; set; } = false;

    [MaxLength(500)]
    public string? ModerationNote { get; set; }

    public DateTime? DeletedAt { get; set; }

    // ============================================================
    // Counters (denormalized)
    // ============================================================
    public int LikesCount { get; set; } = 0;
    public int CommentsCount { get; set; } = 0;
    public int RepostsCount { get; set; } = 0;
    public int BookmarksCount { get; set; } = 0;
    public int ViewsCount { get; set; } = 0;

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ============================================================
    // Navigation properties
    // ============================================================
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
}

public enum PostVisibility
{
    Public = 0,
    FollowersOnly = 1,
    Private = 2,
    Unlisted = 3
}