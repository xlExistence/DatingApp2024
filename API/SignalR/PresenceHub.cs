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

        await presenceTracker.UserConnected(Context.User.GetUserName(), Context.ConnectionId);
        await Clients.Others.SendAsync("UserIsOnline", Context.User?.GetUserName());

        await GetOnlineUsersAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User == null)
        {
            throw new HubException("Cannot get the current user claim");
        }

        await presenceTracker.UserDisconnected(Context.User.GetUserName(), Context.ConnectionId);

        await Clients.Others.SendAsync("UserIsOffline", Context.User?.GetUserName());

        await GetOnlineUsersAsync();

        await base.OnDisconnectedAsync(exception);
    }

    private async Task GetOnlineUsersAsync()
    {
        var onlineUsers = await presenceTracker.GetOnlineUsers();
        await Clients.All.SendAsync("GetOnlineUsers", onlineUsers);
    }
}
