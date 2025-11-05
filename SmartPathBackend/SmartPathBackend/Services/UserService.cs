using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return _mapper.Map<IEnumerable<UserResponseDto>>(users);
        }

        public async Task<UserResponseDto?> GetByIdAsync(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            return user == null ? null : _mapper.Map<UserResponseDto>(user);
        }

        public async Task<UserResponseDto?> GetByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            return user == null ? null : _mapper.Map<UserResponseDto>(user);
        }

        public async Task<UserResponseDto?> CreateAsync(UserRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.");
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Username is required.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.");

            var email = request.Email.Trim().ToLowerInvariant();
            var username = request.Username.Trim();

            if (await _unitOfWork.Users.GetByEmailAsync(email) is not null)
                throw new InvalidOperationException("Email already exists.");
            if (await _unitOfWork.Users.GetByUsernameAsync(username) is not null)
                throw new InvalidOperationException("Username already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = username,
                FullName = request.FullName?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Major = request.Major?.Trim(),
                Faculty = request.Faculty?.Trim(),
                YearOfStudy = request.YearOfStudy,
                Bio = request.Bio,
                AvatarUrl = request.AvatarUrl,
                Role = request.Role ?? Role.Student,
                CreatedAt = DateTime.UtcNow
            };

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<UserResponseDto?> UpdateAsync(Guid id, UserRequestDto request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return null;

            user.FullName = request.FullName ?? user.FullName;
            user.Bio = request.Bio ?? user.Bio;
            user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.Major = request.Major ?? user.Major;
            user.Faculty = request.Faculty ?? user.Faculty;
            user.YearOfStudy = request.YearOfStudy ?? user.YearOfStudy;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            _unitOfWork.Users.Remove(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<User?> AuthenticateAsync(string emailOrUsername, string password)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername) || string.IsNullOrWhiteSpace(password))
                return null;

            User? user = await _unitOfWork.Users.GetByEmailAsync(emailOrUsername);
            if (user is null)
            {
                user = await _unitOfWork.Users.GetByUsernameAsync(emailOrUsername);
            }

            if (user is null) return null;

            bool ok = BCrypt.Net.BCrypt.Verify(password, user.Password);
            return ok ? user : null;
        }

        public async Task<bool> BanAsync(Guid id, DateTime? until, string? reason, Guid adminId)
        {
            var u = await _unitOfWork.Users.GetByIdAsync(id);
            if (u is null) return false;
            u.IsBanned = true; u.BannedUntil = until; u.BanReason = reason;
            return true;
        }

        public async Task<bool> UnbanAsync(Guid id, Guid adminId)
        {
            var u = await _unitOfWork.Users.GetByIdAsync(id);
            if (u is null) return false;
            u.IsBanned = false; u.BannedUntil = null; u.BanReason = null;
            return true;
        }

        public async Task<UserAdminSummaryDto?> GetAdminSummaryAsync(Guid id)
        {
            var u = await _unitOfWork.Users.GetByIdAsync(id);
            if (u is null) return null;
            var dto = _mapper.Map<UserResponseDto>(u);

            var posts = await _unitOfWork.Posts.CountByAuthorAsync(id);
            var reportsAgainst = await _unitOfWork.Reports.CountAgainstUserContentAsync(id);
            var reportsFiled = await _unitOfWork.Reports.CountFiledByAsync(id);

            return new UserAdminSummaryDto
            {
                User = dto,
                Posts = posts,
                ReportsAgainst = reportsAgainst,
                ReportsFiled = reportsFiled
            };
        }

        public async Task<IReadOnlyList<DailyCountDto>> GetUsersCreatedAsync(int days)
        {
            var start = DateTime.UtcNow.Date.AddDays(-days + 1);
            return await _unitOfWork.Users.CountCreatedDailyAsync(start);
        }

        public async Task<IReadOnlyList<ActivityDailyDto>> GetActivityDailyAsync(int days)
        {
            var start = DateTime.UtcNow.Date.AddDays(-days + 1);
            var posts = await _unitOfWork.Posts.CountCreatedDailyAsync(start);
            var reports = await _unitOfWork.Reports.CountCreatedDailyAsync(start);
            var users = await _unitOfWork.Users.CountCreatedDailyAsync(start);

            var dict = new Dictionary<DateTime, ActivityDailyDto>();
            void up(List<DailyCountDto> src, Action<ActivityDailyDto, int> set)
            {
                foreach (var x in src)
                {
                    if (!dict.TryGetValue(x.Date, out var row)) dict[x.Date] = row = new ActivityDailyDto { Date = x.Date };
                    set(row, x.Count);
                }
            }
            up(posts, (r, c) => r.Posts = c);
            up(reports, (r, c) => r.Reports = c);
            up(users, (r, c) => r.NewUsers = c);
            return dict.Values.OrderBy(x => x.Date).ToList();
        }

        private static List<DailyCountDto> FillMissingDays(DateTime start, DateTime end, IDictionary<DateTime, int> map)
        {
            var list = new List<DailyCountDto>();
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                list.Add(new DailyCountDto { Date = d, Count = map.TryGetValue(d, out var c) ? c : 0 });
            }
            return list;
        }

        public async Task<IReadOnlyList<DailyCountDto>> GetUsersCreatedRangeAsync(DateTime start, DateTime end)
        {
            var raw = await _unitOfWork.Users.CountCreatedDailyAsync(start.Date);
            var dict = raw.ToDictionary(x => x.Date.Date, x => x.Count);
            return FillMissingDays(start, end, dict);
        }

        public async Task<IReadOnlyList<ActivityDailyDto>> GetActivityDailyRangeAsync(DateTime start, DateTime end)
        {
            var p = (await _unitOfWork.Posts.CountCreatedDailyAsync(start.Date)).ToDictionary(x => x.Date.Date, x => x.Count);
            var c = (await _unitOfWork.Comments.CountCreatedDailyAsync(start.Date)).ToDictionary(x => x.Date.Date, x => x.Count);
            var r = (await _unitOfWork.Reactions.CountCreatedDailyAsync(start.Date)).ToDictionary(x => x.Date.Date, x => x.Count);
            var rep = (await _unitOfWork.Reports.CountCreatedDailyAsync(start.Date)).ToDictionary(x => x.Date.Date, x => x.Count);
            var u = (await _unitOfWork.Users.CountCreatedDailyAsync(start.Date)).ToDictionary(x => x.Date.Date, x => x.Count);

            var list = new List<ActivityDailyDto>();
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                list.Add(new ActivityDailyDto
                {
                    Date = d,
                    Posts = p.TryGetValue(d, out var v1) ? v1 : 0,
                    Comments = c.TryGetValue(d, out var v2) ? v2 : 0,
                    Reactions = r.TryGetValue(d, out var v3) ? v3 : 0,
                    Reports = rep.TryGetValue(d, out var v4) ? v4 : 0,
                    NewUsers = u.TryGetValue(d, out var v5) ? v5 : 0,
                });
            }
            return list;
        }
    }
}
