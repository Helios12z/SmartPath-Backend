using AutoMapper;
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
    }
}
