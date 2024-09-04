using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using YanikoRestaurant.Controllers;
using YanikoRestaurant.Data;
using YanikoRestaurant.Models;

namespace YanikoRestaurant.Tests;

public class ProductsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _controller = new ProductsController(_context, _mockWebHostEnvironment.Object);

        // Seed the database
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var categories = new List<Category>
        {
            new Category { CategoryId = 1, Name = "Category 1" },
            new Category { CategoryId = 2, Name = "Category 2" }
        };

        _context.Categories.AddRange(categories);

        var products = new List<Product>
        {
            new Product { ProductId = 1, Name = "Product 1", CategoryId = 1 },
            new Product { ProductId = 2, Name = "Product 2", CategoryId = 2 }
        };

        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfProducts()
    {
        // Act
        var result = await _controller.Index(null!, null!, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Product>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task AddEdit_Get_ReturnsViewResult_WithNewProduct_WhenIdIsZero()
    {
        // Act
        var result = await _controller.AddEdit(0);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Product>(viewResult.Model);
        Assert.Equal(0, model.ProductId);
        Assert.Equal("Add", viewResult.ViewData["Operation"]);
    }

    [Fact]
    public async Task AddEdit_Post_RedirectsToIndex_WhenModelStateIsValid()
    {
        // Arrange
        var product = new Product { ProductId = 0, Name = "New Product", CategoryId = 1 };
        var ingredientIds = new int[] { 1, 2 };
        var catId = 1;

        // Act
        var result = await _controller.AddEdit(product, ingredientIds, catId);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectToActionResult.ActionName);
        Assert.Equal("Products", redirectToActionResult.ControllerName);

        // Verify the product was added to the database
        var addedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Name == "New Product");
        Assert.NotNull(addedProduct);
    }

    [Fact]
    public async Task Delete_RedirectsToIndex_WhenProductExists()
    {
        // Arrange
        int productId = 1;

        // Act
        var result = await _controller.Delete(productId);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectToActionResult.ActionName);

        // Verify the product was removed from the database
        var deletedProduct = await _context.Products.FindAsync(productId);
        Assert.Null(deletedProduct);
    }
}
