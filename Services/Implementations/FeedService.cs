using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Models.ViewModels;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Interfaces;
using HiveCore.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HiveCore.Services.Implementations;

/// <summary>
/// Feed aggregation service. Provides the home feed (for-you / following / latest),
/// the explore page (trending / latest / people), and notification listing.
/// </summary>
public class FeedService : IFeedService
{
    private readonly IPostRepository _postRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPostService _postService;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IPostRepository postRepo,
        IUserRepository userRepo,
        IPostService postService,
        IUserService userService,
        INotificationService notificationService,
        ILogger<FeedService> logger)
    {
        _postRepo = postRepo;
        _userRepo = userRepo;
        _postService = postService;
        _userService = userService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ============================================================
    // Home feed
    // ============================================================
    public async Task<HomeFeedViewModel> GetHomeFeedAsync(
        Guid userId, string feedType, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? Constants.Limits.FeedPageSize
                                : Math.Min(pageSize, Constants.Limits.MaxPageSize);

        feedType = (feedType ?? "for-you").ToLowerInvariant();

        // ============================================================
        // Fetch (items, total)
        // ============================================================
        IReadOnlyList<Post> items;
        int total;

        switch (feedType)
        {
            case "following":
                (items, total) = await _postRepo.GetHomeFeedAsync(userId, page, pageSize, ct);
                break;

            case "latest":
                (items, total) = await _postRepo.GetLatestFeedAsync(page, pageSize, ct);
                break;

            case "for-you":
            default:
                // For-You = chronological public posts (HiveCore is chronological by design)
                (items, total) = await _postRepo.GetLatestFeedAsync(page, pageSize, ct);
                feedType = "for-you";
                break;
        }

        // ============================================================
        // Bulk map (single query for viewer context)
        // ============================================================
        var posts = await _postService.MapManyAsync(items, userId, ct);

        // ============================================================
        // Sidebar widgets
        // ============================================================
        var suggested = await _userService.GetSuggestedAsync(userId, 5, ct);
        var trending = await GetTrendingHashtagsAsync(10, ct);

        return new HomeFeedViewModel
        {
            Posts = posts.ToList(),
            FeedType = feedType,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            SuggestedUsers = suggested.ToList(),
            TrendingHashtags = trending,
            NewPost = new PostViewModel()
        };
    }

    // ============================================================
    // Explore
    // ============================================================
    public async Task<ExploreViewModel> GetExploreAsync(
        Guid? currentUserId, string? searchQuery, string? tab,
        int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? Constants.Limits.DefaultPageSize
                                : Math.Min(pageSize, Constants.Limits.MaxPageSize);

        tab = (tab ?? "trending").ToLowerInvariant();

        var vm = new ExploreViewModel
        {
            SearchQuery = searchQuery,
            ActiveTab = tab,
            CurrentPage = page,
            TrendingHashtags = await GetTrendingHashtagsAsync(10, ct)
        };

        // ============================================================
        // Search mode
        // ============================================================
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var (posts, total) = await _postService.SearchAsync(
                searchQuery, currentUserId, page, pageSize, ct);

            vm.TrendingPosts = posts.ToList();
            vm.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            if (currentUserId.HasValue)
            {
                vm.SuggestedUsers = (await _userService
                    .GetSuggestedAsync(currentUserId.Value, 5, ct))
                    .ToList();
            }

            return vm;
        }

        // ============================================================
        // Tab mode
        // ============================================================
        switch (tab)
        {
            case "trending":
                {
                    var trendingPosts = await _postService.GetTrendingAsync(
                        20, 24, currentUserId, ct);

                    vm.TrendingPosts = trendingPosts.ToList();
                    break;
                }

            case "latest":
                {
                    var (items, total) = await _postRepo.GetLatestFeedAsync(page, pageSize, ct);
                    var posts = await _postService.MapManyAsync(items, currentUserId, ct);

                    vm.TrendingPosts = posts.ToList();
                    vm.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
                    break;
                }

            case "people":
            default:
                {
                    if (currentUserId.HasValue)
                    {
                        vm.SuggestedUsers = (await _userService
                            .GetSuggestedAsync(currentUserId.Value, 20, ct))
                            .ToList();
                    }
                    break;
                }
        }

        // ============================================================
        // Suggested users widget (always shown if user is logged in)
        // ============================================================
        if (currentUserId.HasValue && !vm.SuggestedUsers.Any())
        {
            vm.SuggestedUsers = (await _userService
                .GetSuggestedAsync(currentUserId.Value, 10, ct))
                .ToList();
        }

        return vm;
    }

    // ============================================================
    // Notifications
    // ============================================================
    public async Task<NotificationListViewModel> GetNotificationsAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // Fetch the latest batch (capped) then page in-memory
        var recent = await _notificationService.GetRecentAsync(userId, 200, ct);

        var total = recent.Count;

        var paged = recent
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var unread = await _notificationService.GetUnreadCountAsync(userId, ct);

        return new NotificationListViewModel
        {
            Notifications = paged,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            UnreadCount = unread
        };
    }

    public Task<int> GetUnreadNotificationCountAsync(
        Guid userId, CancellationToken ct = default)
        => _notificationService.GetUnreadCountAsync(userId, ct);

    public Task MarkNotificationReadAsync(
        Guid notificationId, Guid userId, CancellationToken ct = default)
        => _notificationService.MarkReadAsync(notificationId, userId, ct);

    public Task MarkAllNotificationsReadAsync(
        Guid userId, CancellationToken ct = default)
        => _notificationService.MarkAllReadAsync(userId, ct);

    // ============================================================
    // Trending hashtags
    // ============================================================
    private async Task<List<TrendingHashtagViewModel>> GetTrendingHashtagsAsync(
        int count, CancellationToken ct)
    {
        try
        {
            var since = DateTime.UtcNow.AddDays(-7);

            // Lightweight projection: only Content of recent posts
            var contents = await _postRepo.Query()
                .AsNoTracking()
                .Where(p => !p.IsDeleted
                         && !p.IsHidden
                         && p.Visibility == PostVisibility.Public
                         && p.CreatedAt >= since)
                .OrderByDescending(p => p.CreatedAt)
                .Take(1000)
                .Select(p => p.Content)
                .ToListAsync(ct);

            return contents
                .SelectMany(HiveCore.Extensions.StringExtensions.ExtractHashtags)
                .GroupBy(tag => tag)
                .Select(g => new TrendingHashtagViewModel
                {
                    Tag = g.Key,
                    PostsCount = g.Count()
                })
                .OrderByDescending(x => x.PostsCount)
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compute trending hashtags");
            return new List<TrendingHashtagViewModel>();
        }
    }
}