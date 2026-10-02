namespace HiveCore.Models.DTOs;

/// <summary>
/// Full user DTO used on profile pages and API responses.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? Pronouns { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public bool IsVerified { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsSuspended { get; set; }
    public bool IsDeleted { get; set; }

    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostsCount { get; set; }
    public int LikesReceivedCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public UserThemeDto? Theme { get; set; }
    public List<MusicTrackDto> MusicTracks { get; set; } = new();
}

/// <summary>
/// Compact user DTO used inside posts/comments/notifications.
/// </summary>
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
}

public class UserThemeDto
{
    public string PrimaryColor { get; set; } = "#FF6B35";
    public string SecondaryColor { get; set; } = "#F7C59F";
    public string AccentColor { get; set; } = "#EFEFD0";
    public string BackgroundColor { get; set; } = "#004E89";
    public string TextColor { get; set; } = "#FFFFFF";
    public string? BackgroundGradient { get; set; }
    public string? HeaderGradient { get; set; }
    public string? FontFamily { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string BackgroundMode { get; set; } = "solid";
    public string LayoutStyle { get; set; } = "default";
    public bool ShowBadges { get; set; } = true;
    public bool ShowMusicPlayer { get; set; } = true;
}

public class MusicTrackDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Album { get; set; }
    public string? CoverUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string Provider { get; set; } = "direct";
    public string? ExternalUrl { get; set; }
    public TimeSpan? Duration { get; set; }
    public int DisplayOrder { get; set; }
}