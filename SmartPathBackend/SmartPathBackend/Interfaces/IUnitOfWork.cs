using SmartPathBackend.Interfaces.Repositories;

namespace SmartPathBackend.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IPostRepository Posts { get; }
        ICommentRepository Comments { get; }
        IReactionRepository Reactions { get; }
        IReportRepository Reports { get; }
        IFriendshipRepository Friendships { get; }
        IChatRepository Chats { get; }
        IMessageRepository Messages { get; }
        INotificationRepository Notifications { get; }
        ISystemLogRepository SystemLogs { get; }

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        Task<int> SaveChangesAsync();
    }
}
