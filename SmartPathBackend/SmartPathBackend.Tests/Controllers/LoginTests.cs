using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPathBackend.Tests.Controllers
{
    public class LoginTests
    {
        // UTL01 - Thiếu trường (service ném ArgumentNullException)
        [Fact]
        public async Task Login_Should_Throw_ArgumentNull_When_Missing_Fields()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new LoginRequest
            {
                EmailOrUsername = "",   // thiếu
                Password = ""           // thiếu
            };

            mockAuth.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(new ArgumentNullException("EmailOrUsername"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => controller.Login(req));

            mockAuth.Verify(a => a.LoginAsync("", ""), Times.Once);
        }

        [Fact]
        public async Task Login_Should_Return_Unauthorized_When_UserNotFound()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new LoginRequest
            {
                EmailOrUsername = "nouser",
                Password = "123456"
            };

            mockAuth.Setup(a => a.LoginAsync(req.EmailOrUsername, req.Password))
                    .ReturnsAsync((AuthResponse?)null);

            var action = await controller.Login(req);

            var unauthorized = action.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorized.Value.Should().Be("Invalid credentials");

            mockAuth.Verify(a => a.LoginAsync(req.EmailOrUsername, req.Password), Times.Once);
        }

        [Fact]
        public async Task Login_Should_Return_Unauthorized_When_PasswordNotMatch()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new LoginRequest
            {
                EmailOrUsername = "existuser",
                Password = "wrongpass"
            };

            mockAuth.Setup(a => a.LoginAsync(req.EmailOrUsername, req.Password))
                    .ReturnsAsync((AuthResponse?)null);

            var action = await controller.Login(req);

            var unauthorized = action.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorized.Value.Should().Be("Invalid credentials");

            mockAuth.Verify(a => a.LoginAsync(req.EmailOrUsername, req.Password), Times.Once);
        }

        // UTL04 - Đăng nhập thành công -> Ok({ accessToken, refreshToken, currentUserId })
        [Fact]
        public async Task Login_Should_Return_Ok_With_Tokens_When_Success()
        {
            var mockAuth = new Mock<IAuthService>();
            var mockUser = new Mock<IUserService>();
            var controller = new AuthController(mockAuth.Object, mockUser.Object);

            var req = new LoginRequest
            {
                EmailOrUsername = "admin",
                Password = "correctpass"
            };

            var expected = new AuthResponse
            {
                CurrentUserId = Guid.NewGuid(),
                AccessToken = "access-token-xxx",
                RefreshToken = "refresh-token-yyy"
            };

            mockAuth.Setup(a => a.LoginAsync(req.EmailOrUsername, req.Password))
                    .ReturnsAsync(expected);

            var action = await controller.Login(req);

            var ok = action.Should().BeOfType<OkObjectResult>().Subject;

            var anon = ok.Value!;
            var type = anon.GetType();
            var accessToken = (string)type.GetProperty("accessToken")!.GetValue(anon)!;
            var refreshToken = (string)type.GetProperty("refreshToken")!.GetValue(anon)!;
            var currentUserId = (Guid)type.GetProperty("currentUserId")!.GetValue(anon)!;

            accessToken.Should().NotBeNullOrWhiteSpace();
            refreshToken.Should().NotBeNullOrWhiteSpace();
            currentUserId.Should().Be(expected.CurrentUserId);

            mockAuth.Verify(a => a.LoginAsync(req.EmailOrUsername, req.Password), Times.Once);
        }
    }
}
