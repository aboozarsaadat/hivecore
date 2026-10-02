using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a music track attached to a user's profile.
/// Users can have multiple tracks in their playlist.
/// </summary>
public class MusicTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // ============================================================
    // Track info
    // ============================================================
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Artist { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Album { get; set; }

    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    [MaxLength(500)]
    public string? AudioUrl { get; set; }

    // Provider (e.g., Spotify, SoundCloud, direct)
    [MaxLength(50)]
    public string Provider { get; set; } = "direct";

    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    public TimeSpan? Duration { get; set; }

    // ============================================================
    // Display
    // ============================================================
    public int DisplayOrder { get; set; } = 0;
    public bool IsPinned { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}