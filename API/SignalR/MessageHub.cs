namespace API.SignalR;

using System.Globalization;
using API.Data;
using API.DataEntities;
using API.DTOs;
using API.Extensions;
using API.UnitOfWork;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

public class MessageHub(
    IUnitOfWork unitOfWork,
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

        var messages = await unitOfWork.MessageRepository.GetThreadAsync(Context.User.GetUserName(), otherUser!);

        if (unitOfWork.HasChanges())
        {
            await unitOfWork.Complete();
        }

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

        var sender = await unitOfWork.UserRepository.GetByUsernameAsync(username);
        var recipient = await unitOfWork.UserRepository.GetByUsernameAsync(messageRequest.RecipientUsername);

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
        var group = await unitOfWork.MessageRepository.GetMessageGroupAsync(groupName);

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

        unitOfWork.MessageRepository.Add(message);

        if (await unitOfWork.Complete())
        {
            await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageResponse>(message));
        }
    }

    private async Task<MessageGroup> AddToMessageGroupAsync(string groupName)
    {
        var username = Context.User?.GetUserName() ?? throw new ArgumentException("Cannot get username");
        var group = await unitOfWork.MessageRepository.GetMessageGroupAsync(groupName);
        var connection = new Connection
        {
            ConnectionId = Context.ConnectionId,
            Username = username
        };

        if (group == null)
        {
            group = new MessageGroup { Name = groupName };
            unitOfWork.MessageRepository.AddGroup(group);
        }

        group.Connections.Add(connection);

        if (await unitOfWork.Complete())
        {
            return group;
        }

        throw new HubException("Failed to join message group");
    }

    private async Task<MessageGroup> RemoveFromMessageGroupAsync()
    {
        var messageGroup = await unitOfWork.MessageRepository.GetMessageGroupForConnectionAsync(Context.ConnectionId);
        var connection = messageGroup?.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
        if (connection != null && messageGroup != null)
        {
            unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await unitOfWork.Complete())
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
