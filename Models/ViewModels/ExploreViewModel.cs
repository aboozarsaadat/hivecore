using HiveCore.Models.DTOs;

namespace HiveCore.Models.ViewModels;

public class ExploreViewModel
{
    public List<PostDto> TrendingPosts { get; set; } = new();

    // Fixed: use UserSummaryDto
    public List<UserSummaryDto> SuggestedUsers { get; set; } = new();

    public List<TrendingHashtagViewModel> TrendingHashtags { get; set; } = new();

    public string? SearchQuery { get; set; }
    public string? ActiveTab { get; set; } = "trending";

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public class TrendingHashtagViewModel
{
    public string Tag { get; set; } = string.Empty;
    public int PostsCount { get; set; }
}