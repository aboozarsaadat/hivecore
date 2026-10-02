using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Application user entity extending ASP.NET Core Identity.
/// Represents a HiveCore account with profile, social, and personalization data.
/// </summary>
public class User : IdentityUser<Guid>
{
    // ============================================================
    // Profile
    // ============================================================
    [MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(100)]
    public string? Pronouns { get; set; }

    // ============================================================
    // Account status
    // ============================================================
    public bool IsVerified { get; set; } = false;
    public bool IsPrivate { get; set; } = false;
    public bool IsSuspended { get; set; } = false;

    [MaxLength(500)]
    public string? SuspensionReason { get; set; }

    public DateTime? LastLoginAt { get; set; }

    [MaxLength(64)]
    public string? LastLoginIp { get; set; }

    // ============================================================
    // Counters (denormalized for performance)
    // ============================================================
    public int FollowersCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;
    public int PostsCount { get; set; } = 0;
    public int LikesReceivedCount { get; set; } = 0;

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // ============================================================
    // Preferences
    // ============================================================
    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "en";

    [MaxLength(100)]
    public string TimeZone { get; set; } = "UTC";

    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool ShowOnlineStatus { get; set; } = true;

    // ============================================================
    // Navigation properties
    // ============================================================
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public ICollection<Follow> Following { get; set; } = new List<Follow>();
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
    public ProfileTheme? ProfileTheme { get; set; }
    public ICollection<MusicTrack> MusicTracks { get; set; } = new List<MusicTrack>();
}