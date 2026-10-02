using HiveCore.Hubs;
using HiveCore.Models.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace HiveCore.Hubs;

/// <summary>
/// Abstraction over SignalR hub contexts, used by services to push real-time events
/// without directly depending on IHubContext.
/// </summary>
public interface IRealTimeNotifier
{
    Task NotifyPostCreatedAsync(Guid authorId, PostDto post, CancellationToken ct = default);
    Task NotifyCommentCreatedAsync(Guid postId, CommentDto comment, CancellationToken ct = default);
    Task NotifyPostUpdatedAsync(Guid postId, PostDto post, CancellationToken ct = default);
    Task NotifyPostDeletedAsync(Guid postId, CancellationToken ct = default);
    Task NotifyCountsUpdatedAsync(Guid postId, int likes, int comments, int reposts, int bookmarks, CancellationToken ct = default);
    Task NotifyNotificationAsync(Guid userId, NotificationDto notification, CancellationToken ct = default);
    Task NotifyUnreadCountAsync(Guid userId, int count, CancellationToken ct = default);
}

public class RealTimeNotifier : IRealTimeNotifier
{
    private readonly IHubContext<FeedHub, IFeedClient> _feed;
    private readonly IHubContext<NotificationHub, INotificationClient> _notif;

    public RealTimeNotifier(
        IHubContext<FeedHub, IFeedClient> feed,
        IHubContext<NotificationHub, INotificationClient> notif)
    {
        _feed = feed;
        _notif = notif;
    }

    public async Task NotifyPostCreatedAsync(Guid authorId, PostDto post, CancellationToken ct = default)
    {
        // Push to author's personal feed
        await _feed.Clients
            .Group(FeedHub.PersonalGroup(authorId))
            .NewPost(post);

        // Push to global "latest" feed
        await _feed.Clients
            .Group(FeedHub.LatestGroup)
            .NewPost(post);
    }

    public async Task NotifyCommentCreatedAsync(Guid postId, CommentDto comment, CancellationToken ct = default)
    {
        await _feed.Clients
            .Group(FeedHub.PostGroup(postId))
            .NewComment(postId, comment);
    }

    public async Task NotifyPostUpdatedAsync(Guid postId, PostDto post, CancellationToken ct = default)
    {
        await _feed.Clients
            .Group(FeedHub.PostGroup(postId))
            .PostUpdated(postId, post);
    }

    public async Task NotifyPostDeletedAsync(Guid postId, CancellationToken ct = default)
    {
        await _feed.Clients
            .Group(FeedHub.PostGroup(postId))
            .PostDeleted(postId);
    }

    public async Task NotifyCountsUpdatedAsync(
        Guid postId, int likes, int comments, int reposts, int bookmarks,
        CancellationToken ct = default)
    {
        await _feed.Clients
            .Group(FeedHub.PostGroup(postId))
            .CountsUpdated(postId, likes, comments, reposts, bookmarks);
    }

    public async Task NotifyNotificationAsync(
        Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        await _notif.Clients
            .Group(NotificationHub.GroupName(userId))
            .ReceiveNotification(notification);
    }

    public async Task NotifyUnreadCountAsync(Guid userId, int count, CancellationToken ct = default)
    {
        await _notif.Clients
            .Group(NotificationHub.GroupName(userId))
            .UnreadCountUpdated(count);
    }
}