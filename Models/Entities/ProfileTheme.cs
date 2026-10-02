using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.Entities;

/// <summary>
/// Represents profile customization settings (colors, gradients, layout).
/// One-to-one relationship with User.
/// </summary>
public class ProfileTheme
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // ============================================================
    // Colors
    // ============================================================
    [MaxLength(20)]
    public string PrimaryColor { get; set; } = "#FF6B35";

    [MaxLength(20)]
    public string SecondaryColor { get; set; } = "#F7C59F";

    [MaxLength(20)]
    public string AccentColor { get; set; } = "#EFEFD0";

    [MaxLength(20)]
    public string BackgroundColor { get; set; } = "#004E89";

    [MaxLength(20)]
    public string TextColor { get; set; } = "#FFFFFF";

    // ============================================================
    // Gradients
    // ============================================================
    [MaxLength(500)]
    public string? BackgroundGradient { get; set; }

    [MaxLength(500)]
    public string? HeaderGradient { get; set; }

    // ============================================================
    // Typography
    // ============================================================
    [MaxLength(100)]
    public string? FontFamily { get; set; }

    // ============================================================
    // Background
    // ============================================================
    [MaxLength(500)]
    public string? BackgroundImageUrl { get; set; }

    [MaxLength(20)]
    public string BackgroundMode { get; set; } = "solid"; // solid | gradient | image

    // ============================================================
    // Layout
    // ============================================================
    [MaxLength(50)]
    public string LayoutStyle { get; set; } = "default";

    public bool ShowBadges { get; set; } = true;
    public bool ShowMusicPlayer { get; set; } = true;

    // ============================================================
    // Timestamps
    // ============================================================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}