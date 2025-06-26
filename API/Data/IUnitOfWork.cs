namespace API.UnitOfWork;

using API.Data;

public interface IUnitOfWork
{
    public IMessageRepository MessageRepository { get; }
    public ILikesRepository LikesRepository { get; }
    public IUserRepository UserRepository { get; }
    public Task<bool> Complete();
    public bool HasChanges();
}
