using HiveCore.Hubs;
using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Interfaces;
using HiveCore.Utilities;
using Microsoft.Extensions.Logging;

namespace HiveCore.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IGenericRepository<Notification> _notificationRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRealTimeNotifier _realtime;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IGenericRepository<Notification> notificationRepo,
        IUserRepository userRepo,
        IRealTimeNotifier realtime,
        ILogger<NotificationService> logger)
    {
        _notificationRepo = notificationRepo;
        _userRepo = userRepo;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Notification> CreateAsync(
        Guid recipientId,
        NotificationType type,
        Guid? actorId = null,
        Guid? postId = null,
        Guid? commentId = null,
        Guid? followId = null,
        string? message = null,
        string? url = null,
        CancellationToken ct = default)
    {
        if (recipientId == actorId) return null!;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            PostId = postId,
            CommentId = commentId,
            FollowId = followId,
            Message = message,
            Url = url,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await _notificationRepo.AddAsync(notification, ct);
        await _notificationRepo.SaveChangesAsync(ct);

        // ============================================================
        // Real-time push
        // ============================================================
        try
        {
            var dto = await BuildDtoAsync(notification, ct);
            await _realtime.NotifyNotificationAsync(recipientId, dto, ct);

            var unread = await GetUnreadCountAsync(recipientId, ct);
            await _realtime.NotifyUnreadCountAsync(recipientId, unread, ct);
        }
        catch (Exception ex)
        {
            // Never fail the notification because of realtime issues
            _logger.LogWarning(ex, "Real-time push of notification {Id} failed", notification.Id);
        }

        _logger.LogDebug("Notification {Id} ({Type}) sent to {Recipient}",
            notification.Id, type, recipientId);

        return notification;
    }

    private async Task<NotificationDto> BuildDtoAsync(Notification n, CancellationToken ct)
    {
        UserSummaryDto? actorDto = null;
        if (n.ActorId.HasValue)
        {
            var actor = await _userRepo.GetByIdAsync(n.ActorId.Value, ct);
            if (actor != null)
            {
                actorDto = new UserSummaryDto
                {
                    Id = actor.Id,
                    Username = actor.UserName ?? string.Empty,
                    DisplayName = actor.DisplayName,
                    AvatarUrl = actor.AvatarUrl,
                    IsVerified = actor.IsVerified
                };
            }
        }

        return new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Actor = actorDto,
            Message = n.Message,
            Url = n.Url,
            ImageUrl = n.ImageUrl,
            PostId = n.PostId,
            CommentId = n.CommentId,
            FollowId = n.FollowId,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt,
            RelativeTime = DateHelper.ToRelative(n.CreatedAt)
        };
    }

    // ============================================================
    // Rest of the class is unchanged from the previous version.
    // Copy the NotifyLikeAsync, NotifyCommentAsync, etc. from before.
    // ============================================================
    public Task NotifyLikeAsync(Guid postAuthorId, Guid actorId, Guid postId, CancellationToken ct = default)
        => CreateAsync(postAuthorId, NotificationType.Like, actorId, postId: postId, url: $"/Post/{postId}", ct: ct);

    public Task NotifyCommentAsync(Guid postAuthorId, Guid actorId, Guid postId, Guid commentId, CancellationToken ct = default)
        => CreateAsync(postAuthorId, NotificationType.Comment, actorId,
            postId: postId, commentId: commentId, url: $"/Post/{postId}#comment-{commentId}", ct: ct);

    public Task NotifyReplyAsync(Guid parentAuthorId, Guid actorId, Guid postId, Guid commentId, CancellationToken ct = default)
        => CreateAsync(parentAuthorId, NotificationType.Reply, actorId,
            postId: postId, commentId: commentId, url: $"/Post/{postId}#comment-{commentId}", ct: ct);

    public Task NotifyFollowAsync(Guid targetUserId, Guid actorId, Guid followId, CancellationToken ct = default)
        => CreateAsync(targetUserId, NotificationType.Follow, actorId, followId: followId, ct: ct);

    public Task NotifyFollowRequestAsync(Guid targetUserId, Guid actorId, Guid followId, CancellationToken ct = default)
        => CreateAsync(targetUserId, NotificationType.FollowRequest, actorId, followId: followId, ct: ct);

    public Task NotifyFollowAcceptedAsync(Guid requesterId, Guid actorId, Guid followId, CancellationToken ct = default)
        => CreateAsync(requesterId, NotificationType.FollowAccepted, actorId, followId: followId, ct: ct);

    public Task NotifyMentionAsync(Guid mentionedUserId, Guid actorId, Guid postId, CancellationToken ct = default)
        => CreateAsync(mentionedUserId, NotificationType.Mention, actorId,
            postId: postId, url: $"/Post/{postId}", ct: ct);

    public Task NotifyRepostAsync(Guid postAuthorId, Guid actorId, Guid postId, CancellationToken ct = default)
        => CreateAsync(postAuthorId, NotificationType.Repost, actorId, postId: postId, ct: ct);

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId, int count, CancellationToken ct = default)
    {
        count = count < 1 ? 20 : count;

        var list = await _notificationRepo.FindAsync(
            n => n.RecipientId == userId && !n.IsDeleted, ct);

        var dtos = new List<NotificationDto>();
        foreach (var n in list.OrderByDescending(x => x.CreatedAt).Take(count))
            dtos.Add(await BuildDtoAsync(n, ct));

        return dtos;
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId, ct);
        if (notification == null || notification.RecipientId != userId) return;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _notificationRepo.Update(notification);
            await _notificationRepo.SaveChangesAsync(ct);

            try
            {
                var unread = await GetUnreadCountAsync(userId, ct);
                await _realtime.NotifyUnreadCountAsync(userId, unread, ct);
            }
            catch { /* ignore */ }
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _notificationRepo.FindAsync(
            n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted, ct);

        if (unread.Count == 0) return;

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        _notificationRepo.UpdateRange(unread);
        await _notificationRepo.SaveChangesAsync(ct);

        try
        {
            await _realtime.NotifyUnreadCountAsync(userId, 0, ct);
        }
        catch { /* ignore */ }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _notificationRepo.CountAsync(
            n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted, ct);
    }
}