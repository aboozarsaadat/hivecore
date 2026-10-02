using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HiveCore.Hubs;

/// <summary>
/// Real-time feed hub. Broadcasts live post events to interested clients
/// (own feed, follow-target feed, global latest feed).
/// </summary>
[Authorize]
public class FeedHub : Hub<IFeedClient>
{
    private readonly ILogger<FeedHub> _logger;

    public FeedHub(ILogger<FeedHub> logger)
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

        // Personal feed group
        await Groups.AddToGroupAsync(Context.ConnectionId, PersonalGroup(userId.Value));
        // Global "latest" feed
        await Groups.AddToGroupAsync(Context.ConnectionId, LatestGroup);
        // Public post page subscribers (explicit opt-in)
        _logger.LogDebug("User {UserId} connected to FeedHub ({ConnId})",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PersonalGroup(userId.Value));
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, LatestGroup);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Subscribe to live updates for a specific post's comment thread.</summary>
    public async Task JoinPostGroup(Guid postId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, PostGroup(postId));

    public async Task LeavePostGroup(Guid postId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, PostGroup(postId));

    public static string PersonalGroup(Guid userId) => $"feed:{userId}";
    public const string LatestGroup = "feed:latest";
    public static string PostGroup(Guid postId) => $"post:{postId}";

    private Guid? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

/// <summary>Strongly-typed client contract for the FeedHub.</summary>
public interface IFeedClient
{
    Task NewPost(object postDto);
    Task NewComment(Guid postId, object commentDto);
    Task PostUpdated(Guid postId, object postDto);
    Task PostDeleted(Guid postId);
    Task CountsUpdated(Guid postId, int likes, int comments, int reposts, int bookmarks);
}