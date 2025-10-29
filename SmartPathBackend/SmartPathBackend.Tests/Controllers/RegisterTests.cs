using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Tests.Controllers
{
    public class RegisterTests
    {
        [Fact]
        public async Task Register_Should_Return_BadRequest_When_Missing_Fields()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new RegisterRequest
            {
                Email = "",
                Username = "abc",
                Password = "123456",
                FullName = ""
            };

            mockAuth
                .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new ArgumentException("Missing required fields"));

            var result = await controller.Register(req);

            result.Should().BeOfType<BadRequestObjectResult>();
            mockAuth.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Once);
        }

        // UTCREG02 - Password too short
        [Fact]
        public async Task Register_Should_Return_BadRequest_When_PasswordTooShort()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new RegisterRequest
            {
                Email = "user@mail.com",
                Username = "abc",
                Password = "123",  
                FullName = "John Doe"
            };

            mockAuth
                .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new ArgumentException("Password must be at least 6 characters"));

            var result = await controller.Register(req);

            result.Should().BeOfType<BadRequestObjectResult>();
            mockAuth.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Once);
        }

        // UTCREG03 - Email already exists
        [Fact]
        public async Task Register_Should_Return_Conflict_When_EmailAlreadyExists()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new RegisterRequest
            {
                Email = "exist@mail.com",
                Username = "newuser",
                Password = "123456",
                FullName = "John Doe"
            };

            mockAuth
                .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new InvalidOperationException("Email already in use"));

            var result = await controller.Register(req);

            result.Should().BeOfType<ConflictObjectResult>();
            mockAuth.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Once);
        }

        // UTCREG04 - Username already exists
        [Fact]
        public async Task Register_Should_Return_Conflict_When_UsernameAlreadyExists()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new RegisterRequest
            {
                Email = "user@mail.com",
                Username = "admin1",
                Password = "123456",
                FullName = "John Doe"
            };

            mockAuth
                .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new InvalidOperationException("Username already in use"));

            var result = await controller.Register(req);

            result.Should().BeOfType<ConflictObjectResult>();
            mockAuth.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Once);
        }

        // UTCREG05 - Successful registration
        [Fact]
        public async Task Register_Should_Return_Ok_With_User_When_Success()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new RegisterRequest
            {
                Email = "user@mail.com",
                Username = "newuser",
                Password = "123456",
                FullName = "John Doe",
                Role = Role.Student
            };

            var expectedUser = new UserResponseDto
            {
                Id = Guid.NewGuid(),
                Email = "user@mail.com",
                Username = "newuser",
                FullName = "John Doe",
            };

            mockAuth.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>()))
                    .ReturnsAsync(expectedUser);

            var action = await controller.Register(req);

            var ok = action.Should().BeOfType<OkObjectResult>().Subject;

            var anon = ok.Value!;
            var userProp = anon.GetType().GetProperties()
                .FirstOrDefault(p => typeof(UserResponseDto).IsAssignableFrom(p.PropertyType));
            userProp.Should().NotBeNull("response wrapper phải chứa UserResponseDto");
            var payload = (UserResponseDto)userProp!.GetValue(anon)!;

            payload.Id.Should().NotBeEmpty();
            payload.Email.Should().Be("user@mail.com");
            payload.Username.Should().Be("newuser");

            mockAuth.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Once);
        }
    }
}
