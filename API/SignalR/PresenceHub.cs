namespace API.SignalR;

using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class PresenceHub(PresenceTracker presenceTracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User == null)
        {
            throw new HubException("Cannot get the current user claim");
        }

        var isOnline = await presenceTracker.UserConnected(Context.User.GetUserName(), Context.ConnectionId);
        if (isOnline)
        {
            await Clients.Others.SendAsync("UserIsOnline", Context.User?.GetUserName());
        }

        var onlineUsers = await presenceTracker.GetOnlineUsers();
        await Clients.Caller.SendAsync("GetOnlineUsers", onlineUsers);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User == null)
        {
            throw new HubException("Cannot get the current user claim");
        }

        var isOffline = await presenceTracker.UserDisconnected(Context.User.GetUserName(), Context.ConnectionId);
        if (isOffline)
        {
            await Clients.Others.SendAsync("UserIsOffline", Context.User?.GetUserName());
        }

        await base.OnDisconnectedAsync(exception);
    }
}
