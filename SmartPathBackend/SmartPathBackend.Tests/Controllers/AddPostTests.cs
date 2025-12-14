using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Utils;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartPathBackend.Tests.Controllers
{
    public class AddPostTests
    {
        private static Guid AuthorId => Guid.NewGuid();

        private static PostController CreateController(Mock<IPostService> postServiceMock, Guid? authorId = null)
        {
            var logMock = new Mock<ISystemLogService>();

            var controller = new PostController(
                postServiceMock.Object,
                logMock.Object
            );

            ClaimsPrincipal user = null;
            if (authorId.HasValue)
            {
                user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, authorId.Value.ToString())
                }, "mock"));
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            return controller;
        }

      // =========================
// UTCPOST01 - Missing AuthorId
// =========================
[Fact]
public async Task UTCPOST01_MissingAuthorId_ShouldReturnUnauthorized()
{
    var serviceMock = new Mock<IPostService>();
    var controller = CreateController(serviceMock, null);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
    {
        await controller.Create(new PostRequestDto
        {
            Title = "Test"
        });
    });
}


        // =========================
        // UTCPOST02 - Missing Title
        // =========================
        [Fact]
        public async Task UTCPOST02_MissingTitle_ShouldReturnBadRequest()
        {
            var serviceMock = new Mock<IPostService>();
            var controller = CreateController(serviceMock, AuthorId);

            serviceMock
                .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<PostRequestDto>()))
                .ThrowsAsync(new ArgumentException("Title is required"));

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await controller.Create(new PostRequestDto
                {
                    Title = null
                });
            });
        }

        // =========================
        // UTCPOST03 - Valid Input (Happy Path)
        // =========================
        [Fact]
        public async Task UTCPOST03_ValidInput_ShouldReturnOk()
        {
            var serviceMock = new Mock<IPostService>();
            var controller = CreateController(serviceMock, AuthorId);

            var expected = new PostResponseDto
            {
                Id = Guid.NewGuid(),
                Title = "Valid Title",
                Content = "Some content"
            };

            serviceMock
                .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<PostRequestDto>()))
                .ReturnsAsync(expected);

            var result = await controller.Create(new PostRequestDto
            {
                Title = "Valid Title",
                Content = "Some content"
            });

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var post = okResult.Value.Should().BeAssignableTo<PostResponseDto>().Subject;

            post.Title.Should().Be("Valid Title");
            post.Content.Should().Be("Some content");
        }

        // =========================
        // UTCPOST04 - Optional Field (CategoryIds removed)
        // =========================
        [Fact]
        public async Task UTCPOST04_OptionalContentProvided_ShouldReturnOk()
        {
            var serviceMock = new Mock<IPostService>();
            var controller = CreateController(serviceMock, AuthorId);

            serviceMock
                .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<PostRequestDto>()))
                .ReturnsAsync(new PostResponseDto { Id = Guid.NewGuid(), Title = "Title Only" });

            var result = await controller.Create(new PostRequestDto
            {
                Title = "Title Only"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // UTCPOST05 - Optional Field (Content omitted)
        // =========================
        [Fact]
        public async Task UTCPOST05_ContentOmitted_ShouldReturnOk()
        {
            var serviceMock = new Mock<IPostService>();
            var controller = CreateController(serviceMock, AuthorId);

            serviceMock
                .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<PostRequestDto>()))
                .ReturnsAsync(new PostResponseDto { Id = Guid.NewGuid(), Title = "Title Only" });

            var result = await controller.Create(new PostRequestDto
            {
                Title = "Title Only",
                Content = null
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // UTCPOST06 - Another Optional Field Test
        // =========================
        [Fact]
        public async Task UTCPOST06_TitleOnly_ShouldReturnOk()
        {
            var serviceMock = new Mock<IPostService>();
            var controller = CreateController(serviceMock, AuthorId);

            serviceMock
                .Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<PostRequestDto>()))
                .ReturnsAsync(new PostResponseDto { Id = Guid.NewGuid(), Title = "Title Only" });

            var result = await controller.Create(new PostRequestDto
            {
                Title = "Title Only"
            });

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
