using Microsoft.AspNetCore.Mvc;
using Moq;
using YanikoRestaurant.Controllers;
using YanikoRestaurant.Models;
using YanikoRestaurant.Repository;

namespace YanikoRestaurant.Tests;

public class IngredientsControllerTests
{
    private readonly Mock<IRepository<Ingredient>> _mockRepository;
    private readonly IngredientsController _controller;

    public IngredientsControllerTests()
    {
        _mockRepository = new Mock<IRepository<Ingredient>>();
        _controller = new IngredientsController(_mockRepository.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfIngredients()
    {
        // Arrange
        var mockRepository = new Mock<IRepository<Ingredient>>();
        var ingredients = new List<Ingredient>
            {
                new Ingredient { IngredientId = 1, Name = "Salt" },
                new Ingredient { IngredientId = 2, Name = "Pepper" }
            };
        mockRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(ingredients);

        var controller = new IngredientsController(mockRepository.Object);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Ingredient>>(viewResult.ViewData.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Details_ReturnsViewResult_WithIngredient()
    {
        // Arrange
        var ingredient = new Ingredient { IngredientId = 1, Name = "Tomato" };
        var queryOptions = new QueryOptions<Ingredient>
        {
            Includes = "ProductIngredients.Product"
        };

        _mockRepository.Setup(repo => repo.GetByIdAsync(1, It.IsAny<QueryOptions<Ingredient>>()))
            .ReturnsAsync(ingredient);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<Ingredient>(viewResult.Model);
        Assert.Equal(1, model.IngredientId);
        Assert.Equal("Tomato", model.Name);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIngredientNotFound()
    {
        // Arrange
        _mockRepository.Setup(repo => repo.GetByIdAsync(1, It.IsAny<QueryOptions<Ingredient>>()))
            .ReturnsAsync((Ingredient)null!);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Ingredient with ID 1 not found", notFoundResult.Value);
    }
}
