using Microsoft.EntityFrameworkCore.Storage;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;

namespace SmartPathBackend.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SmartPathDbContext _context;
        private IDbContextTransaction? _transaction;

        public IUserRepository Users { get; }
        public IPostRepository Posts { get; }
        public ICommentRepository Comments { get; }
        public IReactionRepository Reactions { get; }
        public IReportRepository Reports { get; }
        public IFriendshipRepository Friendships { get; }
        public IChatRepository Chats { get; }
        public IMessageRepository Messages { get; }
        public INotificationRepository Notifications { get; }
        public ISystemLogRepository SystemLogs { get; }

        public UnitOfWork(
            SmartPathDbContext context,
            IUserRepository users,
            IPostRepository posts,
            ICommentRepository comments,
            IReactionRepository reactions,
            IReportRepository reports,
            IFriendshipRepository friendships,
            IChatRepository chats,
            IMessageRepository messages,
            INotificationRepository notifications,
            ISystemLogRepository systemLogs
        )
        {
            _context = context;

            Users = users;
            Posts = posts;
            Comments = comments;
            Reactions = reactions;
            Reports = reports;
            Friendships = friendships;
            Chats = chats;
            Messages = messages;
            Notifications = notifications;
            SystemLogs = systemLogs;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                return;
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
