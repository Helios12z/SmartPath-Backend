using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Interfaces.Services
{
    public record PostAiReviewResult(bool categoryMatch, bool isAppropriate, double confidence, string reason, Guid? suggestedCategoryId);

    public interface IPostAiReviewer
    {
        Task<PostAiReviewResult> ReviewAsync(Post post, IEnumerable<Category> categories, CancellationToken ct);
    }
}