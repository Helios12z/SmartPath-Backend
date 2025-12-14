using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SmartPathBackend.Tests.Controllers
{
    public class AddFollowTests
    {
        private static FriendshipController CreateController(
            Mock<IFriendshipService> serviceMock,
            Guid userId)
        {
            var controller = new FriendshipController(serviceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            return controller;
        }

        // UTCFOL01 (1)
        // followerId hợp lệ, followedUserId hợp lệ, chưa tồn tại
        [Fact]
        public async Task AddFollow_Should_Return_Ok_When_NewRelationship()
        {
            var followerId = Guid.NewGuid();
            var followedUserId = Guid.NewGuid();

            var mockService = new Mock<IFriendshipService>();

            var expected = new FriendshipResponseDto
            {
                Id = Guid.NewGuid(),
                FollowerId = followerId,
                FollowedUserId = followedUserId,
                Status = Status.Pending
            };

            mockService
                .Setup(s => s.AddFriendAsync(
                    followerId,
                    It.IsAny<FriendshipRequestDto>()))
                .ReturnsAsync(expected);

            var controller = CreateController(mockService, followerId);

            var action = await controller.Follow(new FriendshipRequestDto
            {
                FollowedUserId = followedUserId
            });

            var ok = action.Should().BeOfType<OkObjectResult>().Subject;
            var result = ok.Value.Should().BeAssignableTo<FriendshipResponseDto>().Subject;

            result.FollowerId.Should().Be(followerId);
            result.FollowedUserId.Should().Be(followedUserId);
            result.Status.Should().Be(Status.Pending);
        }

        // UTCFOL01 (2)
        // Quan hệ đã tồn tại
        [Fact]
        public async Task AddFollow_Should_Return_Ok_When_RelationshipExists()
        {
            var followerId = Guid.NewGuid();
            var followedUserId = Guid.NewGuid();

            var mockService = new Mock<IFriendshipService>();

            var existing = new FriendshipResponseDto
            {
                Id = Guid.NewGuid(),
                FollowerId = followerId,
                FollowedUserId = followedUserId,
                Status = Status.Pending
            };

            mockService
                .Setup(s => s.AddFriendAsync(
                    followerId,
                    It.IsAny<FriendshipRequestDto>()))
                .ReturnsAsync(existing);

            var controller = CreateController(mockService, followerId);

            var action = await controller.Follow(new FriendshipRequestDto
            {
                FollowedUserId = followedUserId
            });

            action.Should().BeOfType<OkObjectResult>();
        }

        // UTCFOL01 (3)
        // followerId == followedUserId (controller KHÔNG validate)
        [Fact]
        public async Task AddFollow_Should_Allow_SelfFollow()
        {
            var userId = Guid.NewGuid();

            var mockService = new Mock<IFriendshipService>();

            mockService
                .Setup(s => s.AddFriendAsync(
                    userId,
                    It.IsAny<FriendshipRequestDto>()))
                .ReturnsAsync(new FriendshipResponseDto());

            var controller = CreateController(mockService, userId);

            var action = await controller.Follow(new FriendshipRequestDto
            {
                FollowedUserId = userId
            });

            action.Should().BeOfType<OkObjectResult>();
        }

        // UTCFOL01 (4)
        // FollowedUserId = Guid.Empty (controller KHÔNG validate)
        [Fact]
        public async Task AddFollow_Should_Allow_EmptyGuid()
        {
            var followerId = Guid.NewGuid();

            var mockService = new Mock<IFriendshipService>();

            mockService
                .Setup(s => s.AddFriendAsync(
                    followerId,
                    It.IsAny<FriendshipRequestDto>()))
                .ReturnsAsync(new FriendshipResponseDto());

            var controller = CreateController(mockService, followerId);

            var action = await controller.Follow(new FriendshipRequestDto
            {
                FollowedUserId = Guid.Empty
            });

            action.Should().BeOfType<OkObjectResult>();
        }
    }
}
