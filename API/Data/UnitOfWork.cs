namespace API.Data;

using System.Threading.Tasks;
using API.UnitOfWork;

public class UnitOfWork(
    DataContext context,
    IUserRepository userRepository,
    ILikesRepository likesRepository,
    IMessageRepository messageRepository) : IUnitOfWork
{
    public IMessageRepository MessageRepository => messageRepository;

    public ILikesRepository LikesRepository => likesRepository;

    public IUserRepository UserRepository => userRepository;

    public async Task<bool> Complete() => await context.SaveChangesAsync() > 0;

    public bool HasChanges() => context.ChangeTracker.HasChanges();
}
