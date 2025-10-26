using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReportResponseDto>> GetPendingAsync()
        {
            var reports = await _unitOfWork.Reports.GetPendingReportsAsync();
            return _mapper.Map<IEnumerable<ReportResponseDto>>(reports);
        }

        public async Task<IEnumerable<ReportResponseDto>> GetByReporterAsync(Guid reporterId)
        {
            var reports = await _unitOfWork.Reports.GetReportsByUserAsync(reporterId);
            return _mapper.Map<IEnumerable<ReportResponseDto>>(reports);
        }

        public async Task<ReportResponseDto> CreateAsync(Guid reporterId, ReportRequestDto request)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = reporterId,
                PostId = request.PostId,
                CommentId = request.CommentId,
                ReportedUserId = request.ReportedUserId,
                Reason = request.Reason,
                Status = Status.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ReportResponseDto>(report);
        }

        public async Task<bool> UpdateStatusAsync(Guid reportId, Status status)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report is null) return false;

            if (!Enum.IsDefined(typeof(Status), status)) return false;

            if (report.Status == status) return true;

            report.Status = status;
            _unitOfWork.Reports.Update(report);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
