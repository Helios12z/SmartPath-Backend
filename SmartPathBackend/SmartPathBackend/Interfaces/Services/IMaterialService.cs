using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IMaterialService
    {
        Task<MaterialResponse> UploadImageAsync(Guid uploaderId, MaterialCreateRequest meta, IFormFile file);

        Task<IReadOnlyList<MaterialResponse>> UploadDocumentsAsync(Guid uploaderId, MaterialCreateRequest meta, IFormFile[] files);

        Task<IReadOnlyList<MaterialResponse>> GetByPostAsync(Guid postId);
        Task<IReadOnlyList<MaterialResponse>> GetByCommentAsync(Guid commentId);
        Task<IReadOnlyList<MaterialResponse>> GetByMessageAsync(Guid messageId);

        Task<bool> DeleteAsync(Guid id, Guid requesterId);
    }
}
