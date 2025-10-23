using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
        Task<CategoryResponseDto?> GetByIdAsync(Guid id);
        Task<CategoryResponseDto> CreateAsync(CategoryRequestDto request);
        Task<CategoryResponseDto?> UpdateAsync(Guid id, CategoryRequestDto request);
        Task<bool> DeleteAsync(Guid id);
    }
}
