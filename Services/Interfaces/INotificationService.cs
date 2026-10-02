using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;

namespace HiveCore.Services.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateAsync(
        Guid recipientId,
        NotificationType type,
        Guid? actorId = null,
        Guid? postId = null,
        Guid? commentId = null,
        Guid? followId = null,
        string? message = null,
        string? url = null,
        CancellationToken ct = default);

    Task NotifyLikeAsync(Guid postAuthorId, Guid actorId, Guid postId, CancellationToken ct = default);

    Task NotifyCommentAsync(
        Guid postAuthorId, Guid actorId, Guid postId, Guid commentId, CancellationToken ct = default);

    Task NotifyReplyAsync(
        Guid parentAuthorId, Guid actorId, Guid postId, Guid commentId, CancellationToken ct = default);

    Task NotifyFollowAsync(Guid targetUserId, Guid actorId, Guid followId, CancellationToken ct = default);

    Task NotifyFollowRequestAsync(
        Guid targetUserId, Guid actorId, Guid followId, CancellationToken ct = default);

    Task NotifyFollowAcceptedAsync(
        Guid requesterId, Guid actorId, Guid followId, CancellationToken ct = default);

    Task NotifyMentionAsync(
        Guid mentionedUserId, Guid actorId, Guid postId, CancellationToken ct = default);

    Task NotifyRepostAsync(
        Guid postAuthorId, Guid actorId, Guid postId, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetRecentAsync(
        Guid userId, int count, CancellationToken ct = default);

    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}