using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartPathBackend.Tests.Controllers
{
    public class AddBadgeTests
    {
        // =========================
        // UTCBD01 - Badge hợp lệ
        // =========================
        [Fact]
        public async Task UTCBD01_ValidBadge_ShouldReturnCreated()
        {
            var mockService = new Mock<IBadgeService>();
            var controller = new BadgeController(mockService.Object);

            var req = new BadgeRequestDTO
            {
                Name = "Gold",
                Point = 100
            };

            var expected = new BadgeResponseDTO
            {
                Id = Guid.NewGuid(),
                Name = "Gold",
                Point = 100
            };

            mockService.Setup(s => s.CreateAsync(It.IsAny<BadgeRequestDTO>()))
                       .ReturnsAsync(expected);

            var result = await controller.Create(req);

            var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var badge = created.Value.Should().BeAssignableTo<BadgeResponseDTO>().Subject;

            badge.Name.Should().Be("Gold");
            badge.Point.Should().Be(100);

            mockService.Verify(s => s.CreateAsync(It.IsAny<BadgeRequestDTO>()), Times.Once);
        }

        // =========================
        // UTCBD02 - Trùng Name & Point
        // =========================
        [Fact]
        public async Task UTCBD01_DuplicateNameAndPoint_ShouldReturnConflict()
        {
            var mockService = new Mock<IBadgeService>();
            var controller = new BadgeController(mockService.Object);

            mockService.Setup(s => s.CreateAsync(It.IsAny<BadgeRequestDTO>()))
                       .ThrowsAsync(new InvalidOperationException("Badge name or point already exists"));

            var req = new BadgeRequestDTO { Name = "Gold", Point = 100 };
            var result = await controller.Create(req);

            var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflict.Value.Should().NotBeNull();
        }

        // =========================
        // UTCBD03 - Name hoặc Point trống
        // =========================
        [Fact]
        public async Task UTCBD01_NameOrPointMissing_ShouldReturnBadRequest()
        {
            var mockService = new Mock<IBadgeService>();
            var controller = new BadgeController(mockService.Object);

            mockService.Setup(s => s.CreateAsync(It.IsAny<BadgeRequestDTO>()))
                       .ThrowsAsync(new ArgumentException("Badge name or point is required"));

            var req = new BadgeRequestDTO { Name = "", Point = 0 };
            var result = await controller.Create(req);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().NotBeNull();
        }

        // =========================
        // UTCBD04 - Point âm
        // =========================
        [Fact]
        public async Task UTCBD01_NegativePoint_ShouldReturnBadRequest()
        {
            var mockService = new Mock<IBadgeService>();
            var controller = new BadgeController(mockService.Object);

            mockService.Setup(s => s.CreateAsync(It.IsAny<BadgeRequestDTO>()))
                       .ThrowsAsync(new ArgumentException("Point must be non-negative"));

            var req = new BadgeRequestDTO { Name = "Silver", Point = -10 };
            var result = await controller.Create(req);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().NotBeNull();
        }

        // =========================
        // UTCBD05 - Name hợp lệ nhưng trùng Point
        // =========================
        [Fact]
        public async Task UTCBD01_DuplicatePoint_ShouldReturnConflict()
        {
            var mockService = new Mock<IBadgeService>();
            var controller = new BadgeController(mockService.Object);

            mockService.Setup(s => s.CreateAsync(It.IsAny<BadgeRequestDTO>()))
                       .ThrowsAsync(new InvalidOperationException("Badge point already exists"));

            var req = new BadgeRequestDTO { Name = "Platinum", Point = 100 };
            var result = await controller.Create(req);

            var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflict.Value.Should().NotBeNull();
        }
    }
}
