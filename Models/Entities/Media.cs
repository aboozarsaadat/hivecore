using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Represents an uploaded media file (image, video, audio, or gif).
/// May belong to a post, a user avatar/cover, or a music track.
/// </summary>
public class Media
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ============================================================
    // Ownership
    // ============================================================
    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;

    public Guid? PostId { get; set; }
    public Post? Post { get; set; }

    public Guid? MusicTrackId { get; set; }
    public MusicTrack? MusicTrack { get; set; }

    // ============================================================
    // File info
    // ============================================================
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Extension { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    // ============================================================
    // Dimensions (for images/videos)
    // ============================================================
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }

    // ============================================================
    // Metadata
    // ============================================================
    public MediaType Type { get; set; } = MediaType.Image;

    [MaxLength(500)]
    public string? AltText { get; set; }

    public bool IsDeleted { get; set; } = false;

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MediaType
{
    Image = 0,
    Video = 1,
    Gif = 2,
    Audio = 3,
    Document = 4
}