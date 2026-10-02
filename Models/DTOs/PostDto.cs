using HiveCore.Models.Entities;

namespace HiveCore.Models.DTOs;

public class PostDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;

    public UserSummaryDto Author { get; set; } = null!;

    public Guid? ParentPostId { get; set; }
    public PostDto? ParentPost { get; set; }

    public bool IsRepost { get; set; }
    public bool IsQuote { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsDeleted { get; set; }

    public PostVisibility Visibility { get; set; }

    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int RepostsCount { get; set; }
    public int BookmarksCount { get; set; }
    public int ViewsCount { get; set; }

    public List<MediaDto> Media { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Contextual flags for the current viewer
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsBookmarkedByCurrentUser { get; set; }
    public bool IsRepostedByCurrentUser { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public string? RelativeTime { get; set; }
}

public class MediaDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? AltText { get; set; }
}