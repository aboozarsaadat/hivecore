using Microsoft.AspNetCore.SignalR;

namespace HiveCore.Hubs;

/// <summary>
/// Custom SignalR user identifier provider. Uses ClaimTypes.NameIdentifier
/// (which contains the user's Guid) so that Clients.User(userId) works.
/// </summary>
public class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}