using HiveCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HiveCore.Hubs;

/// <summary>
/// Real-time notification hub. Sends typed events to each user's personal group.
/// The group name is the user's Guid (string form).
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId.Value));
        _logger.LogDebug("User {UserId} connected to NotificationHub ({ConnId})",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId.Value));
            _logger.LogDebug("User {UserId} disconnected from NotificationHub ({ConnId})",
                userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Marks a notification as read (client → server).</summary>
    public async Task AcknowledgeNotification(Guid notificationId)
    {
        // Actual DB work is done via HTTP; this just broadcasts to other tabs
        var userId = GetUserId();
        if (userId == null) return;

        await Clients.OthersInGroup(GroupName(userId.Value))
            .NotificationRead(notificationId);
    }

    /// <summary>Used by the client to (re)request its unread count.</summary>
    public async Task RequestUnreadCount()
    {
        // Server-side count is available via HTTP; this is a lightweight ping
        var userId = GetUserId();
        if (userId == null) return;

        // Client will call the HTTP endpoint for the actual count
        await Task.CompletedTask;
    }

    public static string GroupName(Guid userId) => $"user:{userId}";

    private Guid? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

/// <summary>Strongly-typed client contract for the NotificationHub.</summary>
public interface INotificationClient
{
    Task ReceiveNotification(object payload);
    Task NotificationRead(Guid notificationId);
    Task UnreadCountUpdated(int count);
    Task ReceiveToast(string message, string? url);
}