using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System.Security.Claims;

namespace SmartPathBackend.Tests.Controllers
{
    public class EditCommentTests
    {
        private readonly Mock<ICommentService> _commentServiceMock;
        private readonly Mock<ISystemLogService> _logServiceMock;
        private readonly CommentController _controller;
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _commentId = Guid.NewGuid();

        public EditCommentTests()
        {
            _commentServiceMock = new Mock<ICommentService>();
            _logServiceMock = new Mock<ISystemLogService>();

            _controller = new CommentController(
                _commentServiceMock.Object,
                _logServiceMock.Object
            );

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
            }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // UTC BADUPD01 – Success
        [Fact]
        public async Task Update_ValidRequest_ReturnsOk()
        {
            var req = new CommentRequestDto
            {
                Content = "Updated content"
            };

            _commentServiceMock
                .Setup(s => s.UpdateAsync(_commentId, req))
                .ReturnsAsync(new CommentResponseDto { Content = req.Content });

            var result = await _controller.Update(_commentId, req);

            Assert.IsType<OkObjectResult>(result);
        }

        // UTC BADUPD03 – Content empty
        [Fact]
        public async Task Update_EmptyContent_ThrowsArgumentException()
        {
            var req = new CommentRequestDto { Content = "" };

            _commentServiceMock
                .Setup(s => s.UpdateAsync(_commentId, req))
                .ThrowsAsync(new ArgumentException("Content is required"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _controller.Update(_commentId, req));
        }

        // UTC BADUPD04 – Comment not found
        [Fact]
        public async Task Update_CommentNotFound_ReturnsNotFound()
        {
            var req = new CommentRequestDto { Content = "Update" };

            _commentServiceMock
                .Setup(s => s.UpdateAsync(_commentId, req))
                .ReturnsAsync((CommentResponseDto?)null);

            var result = await _controller.Update(_commentId, req);

            Assert.IsType<NotFoundResult>(result);
        }

        // UTC BADUPD05 – Not author
        [Fact]
        public async Task Update_NotAuthor_ThrowsUnauthorized()
        {
            var req = new CommentRequestDto { Content = "Update" };

            _commentServiceMock
                .Setup(s => s.UpdateAsync(_commentId, req))
                .ThrowsAsync(new UnauthorizedAccessException("Not comment author"));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.Update(_commentId, req));
        }
    }
}
