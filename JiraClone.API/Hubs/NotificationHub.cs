using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace JiraClone.API.Hubs;

/// <summary>
/// SignalR Hub for real-time notification delivery
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();

        if (userId > 0)
        {
            // Add the connection to a user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            _logger.LogInformation("User {UserId} connected to NotificationHub with ConnectionId {ConnectionId}",
                userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("Connection attempt without valid user ID. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        if (userId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            _logger.LogInformation("User {UserId} disconnected from NotificationHub. ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "Error during disconnection for ConnectionId {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    private ulong GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ulong.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get the group name for a specific user
    /// </summary>
    public static string GetUserGroupName(ulong userId)
    {
        return $"user_{userId}";
    }
}
