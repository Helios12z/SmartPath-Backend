using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System;

public class AddCommentTests
{
    private CommentController CreateController(
        Mock<ICommentService> serviceMock,
        Guid? userId = null)
    {
        var logMock = new Mock<ISystemLogService>();
        var controller = new CommentController(serviceMock.Object, logMock.Object);

        if (userId != null)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }

        return controller;
    }

    // UTCCMT01 - Tạo comment thành công
    [Fact]
    public async Task AddComment_Valid_ReturnsOk()
    {
        var serviceMock = new Mock<ICommentService>();
        var userId = Guid.NewGuid();

        serviceMock.Setup(s =>
            s.CreateAsync(userId, It.IsAny<CommentRequestDto>()))
            .ReturnsAsync(new CommentResponseDto());

        var controller = CreateController(serviceMock, userId);

        var req = new CommentRequestDto
        {
            PostId = Guid.NewGuid(),
            Content = "Hello"
        };

        var result = await controller.Create(req);

        Assert.IsType<OkObjectResult>(result);
    }

    // UTCCMT02 - Reply hợp lệ
    [Fact]
    public async Task AddComment_ValidReply_ReturnsOk()
    {
        var serviceMock = new Mock<ICommentService>();
        var userId = Guid.NewGuid();

        serviceMock.Setup(s =>
            s.CreateAsync(userId, It.IsAny<CommentRequestDto>()))
            .ReturnsAsync(new CommentResponseDto());

        var controller = CreateController(serviceMock, userId);

        var req = new CommentRequestDto
        {
            PostId = Guid.NewGuid(),
            ParentCommentId = Guid.NewGuid(),
            Content = "Reply"
        };

        var result = await controller.Create(req);

        Assert.IsType<OkObjectResult>(result);
    }

    // UTCCMT03 - Content rỗng
    [Fact]
    public async Task AddComment_EmptyContent_ThrowsArgumentException()
    {
        var serviceMock = new Mock<ICommentService>();
        var userId = Guid.NewGuid();

        serviceMock.Setup(s =>
            s.CreateAsync(userId, It.IsAny<CommentRequestDto>()))
            .ThrowsAsync(new ArgumentException("Content is required"));

        var controller = CreateController(serviceMock, userId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            controller.Create(new CommentRequestDto
            {
                PostId = Guid.NewGuid(),
                Content = ""
            }));
    }

    // UTCCMT04 - Parent comment không tồn tại
    [Fact]
    public async Task AddComment_ParentNotFound_ThrowsKeyNotFound()
    {
        var serviceMock = new Mock<ICommentService>();
        var userId = Guid.NewGuid();

        serviceMock.Setup(s =>
            s.CreateAsync(userId, It.IsAny<CommentRequestDto>()))
            .ThrowsAsync(new KeyNotFoundException("Parent comment not found"));

        var controller = CreateController(serviceMock, userId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            controller.Create(new CommentRequestDto
            {
                PostId = Guid.NewGuid(),
                ParentCommentId = Guid.NewGuid(),
                Content = "Reply"
            }));
    }

    // UTCCMT05 - Tự reply chính mình
    [Fact]
    public async Task AddComment_SelfReply_ThrowsArgumentException()
    {
        var serviceMock = new Mock<ICommentService>();
        var userId = Guid.NewGuid();

        serviceMock.Setup(s =>
            s.CreateAsync(userId, It.IsAny<CommentRequestDto>()))
            .ThrowsAsync(new ArgumentException("Cannot reply to your own comment"));

        var controller = CreateController(serviceMock, userId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            controller.Create(new CommentRequestDto
            {
                PostId = Guid.NewGuid(),
                ParentCommentId = Guid.NewGuid(),
                Content = "Invalid"
            }));
    }

   // UTCCMT06 - Chưa đăng nhập
[Fact]
public async Task AddComment_NotAuthenticated_ThrowsArgumentNullException()
{
    var serviceMock = new Mock<ICommentService>();
    var logMock = new Mock<ISystemLogService>();

    var controller = new CommentController(
        serviceMock.Object,
        logMock.Object
    );

    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        controller.Create(new CommentRequestDto
        {
            PostId = Guid.NewGuid(),
            Content = "Hello"
        }));
}

}
