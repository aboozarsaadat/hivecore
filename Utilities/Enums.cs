using System.ComponentModel;

namespace HiveCore.Utilities;

/// <summary>
/// Centralized enums used across the application.
/// Some enums (PostVisibility, NotificationType, MediaType)
/// are already defined next to their entities; those are not duplicated here.
/// </summary>

public enum FeedType
{
    [Description("For You")]
    ForYou = 0,

    [Description("Following")]
    Following = 1,

    [Description("Latest")]
    Latest = 2,

    [Description("Trending")]
    Trending = 3
}

public enum SortOrder
{
    Newest = 0,
    Oldest = 1,
    MostLiked = 2,
    MostCommented = 3,
    Trending = 4
}

public enum ReportReason
{
    [Description("Spam")]
    Spam = 0,

    [Description("Harassment")]
    Harassment = 1,

    [Description("Hate speech")]
    HateSpeech = 2,

    [Description("Violence")]
    Violence = 3,

    [Description("Nudity")]
    Nudity = 4,

    [Description("Misinformation")]
    Misinformation = 5,

    [Description("Impersonation")]
    Impersonation = 6,

    [Description("Self-harm")]
    SelfHarm = 7,

    [Description("Other")]
    Other = 99
}

public enum ReportStatus
{
    Pending = 0,
    UnderReview = 1,
    Resolved = 2,
    Dismissed = 3
}

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public enum MediaUploadKind
{
    Avatar = 0,
    Cover = 1,
    PostImage = 2,
    PostVideo = 3,
    Music = 4
}

public enum AccountStatus
{
    Active = 0,
    Pending = 1,
    Suspended = 2,
    Deactivated = 3,
    Banned = 4
}

public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Push = 2,
    Sms = 3
}

public enum FollowStatus
{
    NotFollowing = 0,
    Pending = 1,
    Following = 2,
    Blocked = 3
}