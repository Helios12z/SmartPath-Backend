using System;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Xunit;
using SmartPathBackend.Services;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Tests.Controllers
{
    public class AddUserTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly UserService _service;

        public AddUserTests()
        {
            _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _service = new UserService(
                _uowMock.Object,
                _mapperMock.Object
            );
        }

        private static UserRequestDto ValidRequest(Action<UserRequestDto>? modify = null)
        {
            var req = new UserRequestDto
            {
                Email = "test@gmail.com",
                Username = "testuser",
                Password = "123456",
                Role = Role.Student
            };
            modify?.Invoke(req);
            return req;
        }

        /* ========================= UTCUSR01 ========================= */
        [Fact]
        public async Task UTCUSR01_ValidInput_ReturnUser()
        {
            var request = ValidRequest();

            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<UserResponseDto>(It.IsAny<User>()))
                .Returns(new UserResponseDto());

            var result = await _service.CreateAsync(request);

            Assert.NotNull(result);
            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        /* ========================= UTCUSR02 ========================= */
        [Fact]
        public async Task UTCUSR02_EmailEmpty_ThrowException()
        {
            var request = ValidRequest(r => r.Email = " ");

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
        }

        /* ========================= UTCUSR03 ========================= */
        [Fact]
        public async Task UTCUSR03_UsernameEmpty_ThrowException()
        {
            var request = ValidRequest(r => r.Username = "");

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
        }

        /* ========================= UTCUSR04 ========================= */
        [Fact]
        public async Task UTCUSR04_PasswordEmpty_ThrowException()
        {
            var request = ValidRequest(r => r.Password = " ");

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(request));
        }

        /* ========================= UTCUSR05 ========================= */
        [Fact]
        public async Task UTCUSR05_EmailDuplicate_ThrowException()
        {
            var request = ValidRequest();

            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new User());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(request));
        }

        /* ========================= UTCUSR06 ========================= */
        [Fact]
        public async Task UTCUSR06_UsernameDuplicate_ThrowException()
        {
            var request = ValidRequest();

            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(new User());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(request));
        }

        /* ========================= UTCUSR07 ========================= */
        [Fact]
        public async Task UTCUSR07_RoleProvided_AssignRole()
        {
            var request = ValidRequest(r => r.Role = Role.Admin);

            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            User? capturedUser = null;
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .Returns(Task.CompletedTask);

            await _service.CreateAsync(request);

            Assert.NotNull(capturedUser);
            Assert.Equal(Role.Admin, capturedUser!.Role);
        }

        /* ========================= UTCUSR08 ========================= */
        [Fact]
        public async Task UTCUSR08_RoleNotProvided_DefaultStudent()
        {
            var request = ValidRequest(r => r.Role = null);

            _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            User? capturedUser = null;
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .Returns(Task.CompletedTask);

            await _service.CreateAsync(request);

            Assert.NotNull(capturedUser);
            Assert.Equal(Role.Student, capturedUser!.Role);
        }
    }
}
