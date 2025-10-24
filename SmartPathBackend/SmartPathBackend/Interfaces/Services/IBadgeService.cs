using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IBadgeService
    {
        Task<IEnumerable<BadgeResponseDTO>> GetAllAsync();
        Task<BadgeResponseDTO?> GetByIdAsync(Guid id);
        Task<BadgeResponseDTO?> GetByPointAsync(int point);
        Task<BadgeResponseDTO?> GetByNameAsync(string name);
        Task<BadgeResponseDTO> CreateAsync(BadgeRequestDTO request);
        Task<BadgeResponseDTO?> UpdateAsync(Guid id, BadgeRequestDTO request);
        Task<bool> DeleteAsync(Guid id);
    }
}
