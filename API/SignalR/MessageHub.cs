namespace API.SignalR;

using System.Globalization;
using API.Data;
using API.DataEntities;
using API.DTOs;
using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

public class MessageHub(
    IMessageRepository messagesRepository,
    IUserRepository userRepository,
    IMapper mapper,
    IHubContext<PresenceHub> presenceHub) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var otherUser = httpContext?.Request.Query["user"];

        if (Context.User == null || string.IsNullOrEmpty(otherUser))
        {
            throw new HubException("Cannot join group");
        }
        var groupName = GetGroupName(Context.User.GetUserName(), otherUser);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        var messageGroup = await AddToMessageGroupAsync(groupName);

        await Clients.Group(groupName).SendAsync("UpdatedGroup", messageGroup);

        var messages = await messagesRepository.GetThreadAsync(Context.User.GetUserName(), otherUser!);

        await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var messageGroup = await RemoveFromMessageGroupAsync();
        await Clients.Group(messageGroup.Name).SendAsync("UpdatedGroup", messageGroup);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessageAsync(MessageRequest messageRequest)
    {
        var username = Context.User?.GetUserName() ?? throw new ArgumentException("Could not get user");

        if (username == messageRequest.RecipientUsername.ToLower(CultureInfo.InvariantCulture))
        {
            throw new HubException("You cannnot message yourself");
        }

        var sender = await userRepository.GetByUsernameAsync(username);
        var recipient = await userRepository.GetByUsernameAsync(messageRequest.RecipientUsername);

        if (recipient == null || sender == null || sender.UserName == null || recipient.UserName == null)
        {
            throw new HubException("The message can't be sent right now");
        }

        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUsername = sender.UserName,
            RecipientUsername = recipient.UserName,
            Content = messageRequest.Content
        };

        var groupName = GetGroupName(sender.UserName, recipient.UserName);
        var group = await messagesRepository.GetMessageGroupAsync(groupName);

        if (group != null && group.Connections.Any(x => x.Username == recipient.UserName))
        {
            message.DateRead = DateTime.UtcNow;
        }
        else
        {
            var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
            if (connections != null && connections?.Count != null)
            {
                await presenceHub.Clients.Clients(connections)
                    .SendAsync("NewMessageReceived", new { username = sender.UserName, knownAs = sender.KnownAs });
            }
        }

        messagesRepository.Add(message);

        if (await messagesRepository.SaveAllAsync())
        {
            await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageResponse>(message));
        }
    }

    private async Task<MessageGroup> AddToMessageGroupAsync(string groupName)
    {
        var username = Context.User?.GetUserName() ?? throw new ArgumentException("Cannot get username");
        var group = await messagesRepository.GetMessageGroupAsync(groupName);
        var connection = new Connection
        {
            ConnectionId = Context.ConnectionId,
            Username = username
        };

        if (group == null)
        {
            group = new MessageGroup { Name = groupName };
            messagesRepository.AddGroup(group);
        }

        group.Connections.Add(connection);

        if (await messagesRepository.SaveAllAsync())
        {
            return group;
        }

        throw new HubException("Failed to join message group");
    }

    private async Task<MessageGroup> RemoveFromMessageGroupAsync()
    {
        var messageGroup = await messagesRepository.GetMessageGroupForConnectionAsync(Context.ConnectionId);
        var connection = messageGroup?.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
        if (connection != null && messageGroup != null)
        {
            messagesRepository.RemoveConnection(connection);
            if (await messagesRepository.SaveAllAsync())
            {
                return messageGroup;
            }
        }

        throw new HubException("Failed to remove from message group");
    }

    private static string GetGroupName(string caller, string? other)
    {
        var stringCompare = string.CompareOrdinal(caller, other) < 0;
        return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
    }
}
