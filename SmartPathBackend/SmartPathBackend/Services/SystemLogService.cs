using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class SystemLogService : ISystemLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SystemLogService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SystemLogResponseDto>> GetRecentAsync(int limit = 50)
        {
            var logs = await _unitOfWork.SystemLogs.GetRecentAsync(limit);
            return _mapper.Map<IEnumerable<SystemLogResponseDto>>(logs);
        }

        public async Task<IEnumerable<SystemLogResponseDto>> GetByUserAsync(Guid userId)
        {
            var logs = await _unitOfWork.SystemLogs.GetByUserAsync(userId);
            return _mapper.Map<IEnumerable<SystemLogResponseDto>>(logs);
        }

        public async Task CreateAsync(Guid? userId, string action, string targetType, string? url = null)
        {
            var log = new SystemLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                TargetType = targetType,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SystemLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
