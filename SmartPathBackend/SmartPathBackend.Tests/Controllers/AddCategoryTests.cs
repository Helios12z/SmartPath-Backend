using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartPathBackend.Controllers;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using Xunit;
using System;
using System.Threading.Tasks;

namespace SmartPathBackend.Tests.Controllers
{
    public class AddCategoryTests
    {
        // UTCAT01 - Tạo category thành công
        [Fact]
        public async Task AddCategory_Should_Return_Created_When_Valid()
        {
            // Arrange
            var mockService = new Mock<ICategoryService>();
            var controller = new CategoryController(mockService.Object);

            var req = new CategoryRequestDto
            {
                Name = "Test"
            };

            var expected = new CategoryResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "Test"
            };

            mockService
                .Setup(s => s.CreateAsync(It.IsAny<CategoryRequestDto>()))
                .ReturnsAsync(expected);

            // Act
            var action = await controller.Create(req);

            // Assert
            var created = action.Should().BeOfType<CreatedAtActionResult>().Subject;
            var category = created.Value.Should().BeAssignableTo<CategoryResponseDto>().Subject;

            category.Name.Should().Be("Test");

            mockService.Verify(
                s => s.CreateAsync(It.IsAny<CategoryRequestDto>()),
                Times.Once
            );
        }

        // UTCAT02 - Trùng tên category
        [Fact]
        public async Task AddCategory_Should_Throw_When_Duplicate()
        {
            // Arrange
            var mockService = new Mock<ICategoryService>();
            var controller = new CategoryController(mockService.Object);

            mockService
                .Setup(s => s.CreateAsync(It.IsAny<CategoryRequestDto>()))
                .ThrowsAsync(new InvalidOperationException("Category already exists"));

            var req = new CategoryRequestDto
            {
                Name = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Create(req));
        }

        // UTCAT03 - Tên category rỗng
        [Fact]
        public async Task AddCategory_Should_Throw_When_Name_Missing()
        {
            // Arrange
            var mockService = new Mock<ICategoryService>();
            var controller = new CategoryController(mockService.Object);

            mockService
                .Setup(s => s.CreateAsync(It.IsAny<CategoryRequestDto>()))
                .ThrowsAsync(new ArgumentException("Category name is required"));

            var req = new CategoryRequestDto
            {
                Name = ""
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => controller.Create(req));
        }
    }
}
