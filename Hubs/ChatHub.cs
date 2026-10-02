using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HiveCore.Hubs;

/// <summary>
/// Real-time direct-message hub.
/// Messages are in-memory and ephemeral (no persistence) — see IMessageStore.
/// </summary>
[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IMessageStore _store;

    public ChatHub(ILogger<ChatHub> logger, IMessageStore store)
    {
        _logger = logger;
        _store = store;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        _logger.LogDebug("User {UserId} connected to ChatHub", userId);

        // Deliver any queued messages while user was offline
        var pending = _store.Drain(userId.Value);
        foreach (var msg in pending)
        {
            await Clients.Caller.ReceiveMessage(msg);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId.Value));

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Sends a direct message to another user.</summary>
    public async Task SendMessage(Guid recipientId, string content)
    {
        var senderId = GetUserId();
        if (senderId == null) return;

        content = (content ?? string.Empty).Trim();
        if (content.Length == 0) return;
        if (content.Length > 2000) content = content[..2000];

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId.Value,
            RecipientId = recipientId,
            Content = content,
            SentAtUtc = DateTime.UtcNow
        };

        // Send to recipient (if online) or queue
        await Clients.Group(UserGroup(recipientId)).ReceiveMessage(message);

        // Echo back to sender for confirmation
        await Clients.Caller.MessageSent(message);

        _logger.LogDebug("Message {MessageId} from {Sender} to {Recipient}",
            message.Id, senderId, recipientId);
    }

    /// <summary>Typing indicator.</summary>
    public async Task SendTyping(Guid recipientId, bool isTyping)
    {
        var senderId = GetUserId();
        if (senderId == null) return;

        await Clients.Group(UserGroup(recipientId))
            .UserTyping(senderId.Value, isTyping);
    }

    /// <summary>Marks a conversation as read.</summary>
    public async Task MarkAsRead(Guid otherUserId)
    {
        var me = GetUserId();
        if (me == null) return;

        await Clients.Group(UserGroup(otherUserId))
            .MessagesRead(me.Value);
    }

    public static string UserGroup(Guid userId) => $"chat:{userId}";

    private Guid? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

/// <summary>Strongly-typed client contract for the ChatHub.</summary>
public interface IChatClient
{
    Task ReceiveMessage(ChatMessage message);
    Task MessageSent(ChatMessage message);
    Task UserTyping(Guid userId, bool isTyping);
    Task MessagesRead(Guid userId);
}

/// <summary>Ephemeral chat message DTO.</summary>
public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}

/// <summary>Very small in-memory store for queued (offline) chat messages.</summary>
public interface IMessageStore
{
    void Enqueue(Guid userId, ChatMessage message);
    IReadOnlyList<ChatMessage> Drain(Guid userId);
}

/// <summary>In-memory implementation of IMessageStore (singleton).</summary>
public class InMemoryMessageStore : IMessageStore
{
    private readonly Dictionary<Guid, List<ChatMessage>> _pending = new();
    private readonly object _lock = new();

    public void Enqueue(Guid userId, ChatMessage message)
    {
        lock (_lock)
        {
            if (!_pending.TryGetValue(userId, out var list))
            {
                list = new List<ChatMessage>();
                _pending[userId] = list;
            }
            list.Add(message);

            // Cap to avoid unbounded growth
            if (list.Count > 100)
                list.RemoveRange(0, list.Count - 100);
        }
    }

    public IReadOnlyList<ChatMessage> Drain(Guid userId)
    {
        lock (_lock)
        {
            if (!_pending.TryGetValue(userId, out var list))
                return Array.Empty<ChatMessage>();

            var copy = list.ToList();
            list.Clear();
            return copy;
        }
    }
}