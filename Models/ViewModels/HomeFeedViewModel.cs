using HiveCore.Models.DTOs;

namespace HiveCore.Models.ViewModels;

public class HomeFeedViewModel
{
    public List<PostDto> Posts { get; set; } = new();
    public PostViewModel NewPost { get; set; } = new();

    public string FeedType { get; set; } = "for-you"; // for-you | following | latest

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool HasMore => CurrentPage < TotalPages;

    // Fixed: use UserSummaryDto (matches IUserService.GetSuggestedAsync signature)
    public List<UserSummaryDto> SuggestedUsers { get; set; } = new();
    public List<TrendingHashtagViewModel> TrendingHashtags { get; set; } = new();
}

public class NotificationListViewModel
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int UnreadCount { get; set; }
}