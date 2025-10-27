namespace SmartPathBackend.Interfaces.Services
{
    public interface IReputationService
    {
        Task ApplyForPostAsync(Guid postId);
        Task ApplyForCommentAsync(Guid commentId);
    }
}
