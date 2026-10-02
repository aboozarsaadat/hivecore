using HiveCore.Models.DTOs;
using HiveCore.Models.ViewModels;

namespace HiveCore.Services.Interfaces;

public interface IFeedService
{
    Task<HomeFeedViewModel> GetHomeFeedAsync(
        Guid userId, string feedType, int page, int pageSize, CancellationToken ct = default);

    Task<ExploreViewModel> GetExploreAsync(
        Guid? currentUserId, string? searchQuery, string? tab, int page, int pageSize, CancellationToken ct = default);

    Task<NotificationListViewModel> GetNotificationsAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task<int> GetUnreadNotificationCountAsync(Guid userId, CancellationToken ct = default);

    Task MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct = default);
}