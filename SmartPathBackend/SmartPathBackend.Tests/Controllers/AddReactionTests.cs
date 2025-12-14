using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System;
using System.Threading.Tasks;

public class AddReactionTests
{
    private ReactionController CreateController(Guid userId, Mock<IReactionService> mockService)
    {
        var controller = new ReactionController(mockService.Object);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }

    // UTCREACT01 - Like Post successfully
    [Fact]
    public async Task UTCREACT01_ValidPostReaction_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IReactionService>();
        mockService.Setup(s => s.ReactAsync(userId, It.IsAny<ReactionRequestDto>()))
                   .ReturnsAsync(new ReactionResponseDto());

        var controller = CreateController(userId, mockService);

        var req = new ReactionRequestDto { PostId = Guid.NewGuid(), IsPositive = true };
        var result = await controller.React(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // UTCREACT02 - Like Comment
    [Fact]
    public async Task UTCREACT02_ValidCommentReaction_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IReactionService>();
        mockService.Setup(s => s.ReactAsync(userId, It.IsAny<ReactionRequestDto>()))
                   .ReturnsAsync(new ReactionResponseDto());

        var controller = CreateController(userId, mockService);

        var req = new ReactionRequestDto { CommentId = Guid.NewGuid(), IsPositive = true };
        var result = await controller.React(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // UTCREACT03 - Only PostId provided
    [Fact]
    public async Task UTCREACT03_OnlyPostId_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IReactionService>();
        mockService.Setup(s => s.ReactAsync(userId, It.IsAny<ReactionRequestDto>()))
                   .ReturnsAsync(new ReactionResponseDto());

        var controller = CreateController(userId, mockService);

        var req = new ReactionRequestDto { PostId = Guid.NewGuid(), IsPositive = true };
        var result = await controller.React(req);

        Assert.IsType<OkObjectResult>(result);
    }

    // UTCREACT04 - Reaction exists, cannot create new, return Ok (controller allows overwrite)
    [Fact]
    public async Task UTCREACT04_ExistingReaction_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IReactionService>();
        mockService.Setup(s => s.ReactAsync(userId, It.IsAny<ReactionRequestDto>()))
                   .ReturnsAsync(new ReactionResponseDto()); // Simulate existing

        var controller = CreateController(userId, mockService);

        var req = new ReactionRequestDto { PostId = Guid.NewGuid(), IsPositive = false };
        var result = await controller.React(req);

        Assert.IsType<OkObjectResult>(result);
    }

    // UTCREACT05 - Cannot react on own post
    [Fact]
    public async Task UTCREACT05_SelfReaction_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IReactionService>();
        mockService.Setup(s => s.ReactAsync(userId, It.IsAny<ReactionRequestDto>()))
                   .ThrowsAsync(new InvalidOperationException("Cannot react on own content"));

        var controller = CreateController(userId, mockService);

        var req = new ReactionRequestDto { PostId = Guid.NewGuid(), IsPositive = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.React(req));
    }

    // UTCREACT06 - Invalid request (no target)
    [Fact]
    public async Task UTCREACT06_NoTargetProvided_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var mockService = new Mock<IReactionService>();
        mockService.Setup(s => s.ReactAsync(userId, It.IsAny<ReactionRequestDto>()))
                   .ThrowsAsync(new ArgumentException("PostId or CommentId must be provided"));

        var controller = CreateController(userId, mockService);

        var req = new ReactionRequestDto(); // No PostId, no CommentId
        await Assert.ThrowsAsync<ArgumentException>(() => controller.React(req));
    }
}
