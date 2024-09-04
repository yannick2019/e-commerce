using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using YanikoRestaurant.Controllers;
using YanikoRestaurant.Data;
using YanikoRestaurant.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using YanikoRestaurant.Repository;

namespace YanikoRestaurant.Tests;

public class OrdersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IRepository<Product>> _mockProducts;
    private readonly Mock<IRepository<Category>> _mockCategories;
    private readonly Mock<IRepository<Order>> _mockOrders;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        // Set up in-memory database
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Set up UserManager mock
        _mockUserManager = MockUserManager();

        // Set up repository mocks
        _mockProducts = new Mock<IRepository<Product>>();
        _mockCategories = new Mock<IRepository<Category>>();
        _mockOrders = new Mock<IRepository<Order>>();

        // Set up default behaviors for repository mocks
        _mockProducts.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Product>());
        _mockCategories.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Category>());

        // Set up HttpContext with mock session
        var httpContext = new DefaultHttpContext()
        {
            Session = new MockHttpSession()
        };

        // Create controller with mocks
        _controller = new OrdersController(
            _context,
            _mockUserManager.Object,
            _mockProducts.Object,
            _mockCategories.Object,
            _mockOrders.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        // Set up TempData provider
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

        return mgr;
    }

    private static byte[] SerializeObject(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return Encoding.UTF8.GetBytes(json);
    }

    [Fact]
    public async Task Create_ReturnsViewResult_WithOrderViewModel()
    {
        // Arrange
        var product = new Product { ProductId = 1, Name = "Test Product" };
        var category = new Category { CategoryId = 1, Name = "Test Category" };

        _context.Products.Add(product);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Create(null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<OrderViewModel>(viewResult.Model);
        Assert.NotNull(model.Products);
        Assert.NotNull(model.OrderItems);
    }

    [Fact]
    public async Task AddItem_ReturnsRedirectToActionResult()
    {
        // Arrange
        var product = new Product { ProductId = 1, Name = "Test Product", Price = 10 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var sessionMock = new Mock<ISession>();
        var sessionStorage = new Dictionary<string, byte[]>();

        sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionStorage[key] = value);

        _controller.ControllerContext.HttpContext.Session = sessionMock.Object;

        // Act
        var result = await _controller.AddItem(1, 2);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Create", redirectToActionResult.ActionName);
        Assert.True(sessionStorage.ContainsKey("OrderViewModel"));
    }

    [Fact]
    public async Task Cart_ReturnsViewResult_WhenOrderViewModelExists()
    {
        // Arrange
        var orderViewModel = new OrderViewModel
        {
            OrderItems = new List<OrderItemViewModel> { new OrderItemViewModel() }
        };

        var sessionMock = new Mock<ISession>();
        var sessionStorage = new Dictionary<string, byte[]>();

        sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns((string key, out byte[] value) =>
            {
                return sessionStorage.TryGetValue(key, out value!);
            });

        sessionStorage["OrderViewModel"] = SerializeObject(orderViewModel);

        _controller.ControllerContext.HttpContext.Session = sessionMock.Object;

        // Act
        var result = await _controller.Cart();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<OrderViewModel>(viewResult.Model);
        Assert.NotNull(model.OrderItems);
        Assert.Single(model.OrderItems);
    }

    [Fact]
    public async Task PlaceOrder_ReturnsRedirectToActionResult()
    {
        // Arrange
        var orderViewModel = new OrderViewModel
        {
            OrderItems = new List<OrderItemViewModel> { new OrderItemViewModel() }
        };
        var sessionMock = new Mock<ISession>();
        var sessionStorage = new Dictionary<string, byte[]>();

        sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns((string key, out byte[] value) =>
            {
                return sessionStorage.TryGetValue(key, out value!);
            });

        sessionMock.Setup(s => s.Remove(It.IsAny<string>()))
            .Callback<string>((key) => sessionStorage.Remove(key));

        sessionStorage["OrderViewModel"] = SerializeObject(orderViewModel);

        _controller.ControllerContext.HttpContext.Session = sessionMock.Object;
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("testUserId");

        // Act
        var result = await _controller.PlaceOrder();

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewOrders", redirectToActionResult.ActionName);
        Assert.False(sessionStorage.ContainsKey("OrderViewModel"));
    }

    [Fact]
    public async Task ViewOrders_ReturnsViewResult_WithListOfOrders()
    {
        // Arrange
        var user = new ApplicationUser { Id = "testUserId", UserName = "testUser" };
        var order = new Order { OrderId = 1, UserId = user.Id };
        var orders = new List<Order> { order };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
        _mockOrders.Setup(repo => repo.GetAllByIdAsync(user.Id, "UserId", It.IsAny<QueryOptions<Order>>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.ViewOrders();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task DeleteOrder_ReturnsRedirectToActionResult()
    {
        // Arrange
        var user = new ApplicationUser { Id = "testUserId", UserName = "testUser" };
        var order = new Order { OrderId = 1, UserId = user.Id };
        var orders = new List<Order> { order };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
        _mockOrders.Setup(repo => repo.GetAllByIdAsync(user.Id, "UserId", It.IsAny<QueryOptions<Order>>()))
            .ReturnsAsync(orders);
        _mockOrders.Setup(repo => repo.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteOrder(1);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewOrders", redirectToActionResult.ActionName);
        _mockOrders.Verify(repo => repo.DeleteAsync(1), Times.Once);
    }
}

public class MockHttpSession : ISession
{
    private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public string Id => throw new NotImplementedException();

    public bool IsAvailable => true;

    public void Clear() => _sessionStorage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _sessionStorage.Remove(key);

    public void Set(string key, byte[] value) => _sessionStorage[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value!);
}
