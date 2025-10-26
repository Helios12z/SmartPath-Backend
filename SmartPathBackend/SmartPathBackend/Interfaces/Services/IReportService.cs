using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Interfaces.Services
{
    public interface IReportService
    {
        Task<IEnumerable<ReportResponseDto>> GetPendingAsync();
        Task<IEnumerable<ReportResponseDto>> GetByReporterAsync(Guid reporterId);
        Task<ReportResponseDto> CreateAsync(Guid reporterId, ReportRequestDto request);
        Task<bool> UpdateStatusAsync(Guid reportId, Status status);     
    }
}
