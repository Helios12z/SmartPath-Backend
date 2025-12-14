using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartPathBackend.Tests.Controllers
{
    public class EditBadgeControllerTests
    {
        private static BadgeController CreateController(Mock<IBadgeService> badgeServiceMock)
        {
            return new BadgeController(badgeServiceMock.Object);
        }

        // =========================
        // UTCBADUPD01 - Cập nhật badge thành công
        // =========================
        [Fact]
        public async Task UTCBADUPD01_ValidUpdate_ShouldReturnOk()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = " New ", Point = 20 };
            var updatedBadge = new BadgeResponseDTO { Id = id, Name = "New", Point = 20 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(updatedBadge);

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var badge = ok.Value.Should().BeAssignableTo<BadgeResponseDTO>().Subject;

            badge.Name.Should().Be("New");
            badge.Point.Should().Be(20);
        }

        // =========================
        // UTCBADUPD02 - Cập nhật badge thành công (case khác)
        // =========================
        [Fact]
        public async Task UTCBADUPD02_ValidUpdateSecondCase_ShouldReturnOk()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = "B", Point = 2 };
            var updatedBadge = new BadgeResponseDTO { Id = id, Name = "B", Point = 2 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(updatedBadge);

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // UTCBADUPD03 - Badge không tồn tại
        // =========================
        [Fact]
        public async Task UTCBADUPD03_BadgeNotFound_ShouldReturnNotFound()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = "Any", Point = 10 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync((BadgeResponseDTO?)null);

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            result.Should().BeOfType<NotFoundResult>();
        }

        // =========================
        // UTCBADUPD04 - Point âm
        // =========================
        [Fact]
        public async Task UTCBADUPD04_PointNegative_ShouldReturnBadRequest()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = "Badge", Point = -1 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request))
                       .ThrowsAsync(new ArgumentOutOfRangeException());

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // =========================
        // UTCBADUPD05 - Trùng tên hoặc điểm
        // =========================
        [Fact]
        public async Task UTCBADUPD05_DuplicateNameOrPoint_ShouldReturnConflict()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = "Dup", Point = 20 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request))
                       .ThrowsAsync(new InvalidOperationException());

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // =========================
        // UTCBADUPD06 - Cập nhật hợp lệ, không trùng
        // =========================
        [Fact]
        public async Task UTCBADUPD06_ValidNoConflict_ShouldReturnOk()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = "Y", Point = 6 };
            var updatedBadge = new BadgeResponseDTO { Id = id, Name = "Y", Point = 6 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(updatedBadge);

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // UTCBADUPD07 - Không thay đổi tên/point nhưng vẫn update
        // =========================
        [Fact]
        public async Task UTCBADUPD07_NoChange_ShouldReturnOk()
        {
            var id = Guid.NewGuid();
            var request = new BadgeRequestDTO { Name = "Same", Point = 10 };
            var updatedBadge = new BadgeResponseDTO { Id = id, Name = "Same", Point = 10 };

            var serviceMock = new Mock<IBadgeService>();
            serviceMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(updatedBadge);

            var controller = CreateController(serviceMock);

            var result = await controller.Update(id, request);

            result.Should().BeOfType<OkObjectResult>();
            var badge = ((OkObjectResult)result).Value.Should().BeAssignableTo<BadgeResponseDTO>().Subject;
            badge.Name.Should().Be("Same");
            badge.Point.Should().Be(10);
        }
    }
}
