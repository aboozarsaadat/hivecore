using HiveCore.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace HiveCore.Models.ViewModels;

public class ProfileViewModel
{
    public UserDto User { get; set; } = null!;
    public List<PostDto> Posts { get; set; } = new();

    public int PostsCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    public bool IsFollowing { get; set; }
    public bool IsFollowPending { get; set; }
    public bool IsOwnProfile { get; set; }
    public bool CanViewContent { get; set; } = true;

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;

    /// <summary>Active tab: "posts" | "media" | "likes".</summary>
    public string Tab { get; set; } = "posts";
}

public class EditProfileViewModel
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(80, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Bio { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string? Location { get; set; }

    [System.ComponentModel.DataAnnotations.Url]
    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string? Website { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? Pronouns { get; set; }

    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public bool IsPrivate { get; set; }
    public bool ShowOnlineStatus { get; set; } = true;

    [System.ComponentModel.DataAnnotations.Display(Name = "Avatar")]
    public IFormFile? AvatarFile { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Cover Image")]
    public IFormFile? CoverFile { get; set; }

    public string? CurrentAvatarUrl { get; set; }
    public string? CurrentCoverUrl { get; set; }
}

public class ProfileSettingsViewModel
{
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool ShowOnlineStatus { get; set; } = true;

    [System.ComponentModel.DataAnnotations.StringLength(10)]
    public string PreferredLanguage { get; set; } = "en";

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string TimeZone { get; set; } = "UTC";

    public bool IsPrivate { get; set; }
}