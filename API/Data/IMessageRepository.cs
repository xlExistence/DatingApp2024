namespace API.Data;

using API.DataEntities;
using API.DTOs;
using API.Helpers;

public interface IMessageRepository
{
    public void Add(Message message);
    public void AddGroup(MessageGroup group);
    public Task<Message?> GetAsync(int id);
    public Task<Connection?> GetConnectionAsync(string connectionId);
    public Task<PagedList<MessageResponse>> GetForUserAsync(MessageParams messageParams);
    public Task<MessageGroup?> GetMessageGroupForConnectionAsync(string connectionId);
    public Task<MessageGroup?> GetMessageGroupAsync(string groupName);
    public Task<IEnumerable<MessageResponse>> GetThreadAsync(string currentUsername, string recipientUsername);
    public void Remove(Message message);
    public void RemoveConnection(Connection connection);
}
