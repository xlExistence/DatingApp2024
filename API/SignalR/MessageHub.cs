namespace API.SignalR;

using System.Globalization;
using API.Data;
using API.DataEntities;
using API.DTOs;
using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

public class MessageHub(IMessageRepository messagesRepository, IUserRepository userRepository, IMapper mapper) : Hub
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
        var messages = await messagesRepository.GetThreadAsync(Context.User.GetUserName(), otherUser!);
        await Clients.Group(groupName).SendAsync("ReceiveMessageThread", messages);
    }

    public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);

    public async Task SendMessage(MessageRequest messageRequest)
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

        messagesRepository.Add(message);

        if (await messagesRepository.SaveAllAsync())
        {
            var group = GetGroupName(sender.UserName, recipient.UserName);
            await Clients.Group(group).SendAsync("NewMessage", mapper.Map<MessageResponse>(message));
        }
    }

    private string GetGroupName(string caller, string? other)
    {
        var stringCompare = string.CompareOrdinal(caller, other) < 0;
        return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
    }
}
