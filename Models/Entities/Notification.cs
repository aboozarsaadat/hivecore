using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a notification delivered to a user.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ============================================================
    // Recipient and Actor
    // ============================================================
    public Guid RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }

    // ============================================================
    // Type and Payload
    // ============================================================
    public NotificationType Type { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }

    // Referenced entity ids
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? FollowId { get; set; }

    // Deep link
    [MaxLength(500)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    // ============================================================
    // State
    // ============================================================
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    Like = 0,
    Comment = 1,
    Reply = 2,
    Follow = 3,
    FollowRequest = 4,
    FollowAccepted = 5,
    Mention = 6,
    Repost = 7,
    Quote = 8,
    Bookmark = 9,
    System = 10,
    Welcome = 11,
    PasswordChanged = 12,
    LoginAlert = 13
}