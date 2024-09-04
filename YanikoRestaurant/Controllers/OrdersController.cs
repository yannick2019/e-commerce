using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YanikoRestaurant.Data;
using YanikoRestaurant.Extensions;
using YanikoRestaurant.Models;
using YanikoRestaurant.Repository;

namespace YanikoRestaurant.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<Product> _products;
    private readonly IRepository<Category> _categories;
    private readonly IRepository<Order> _orders;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IRepository<Product> products,
        IRepository<Category> categories,
        IRepository<Order> orders)
    {
        _context = context;
        _userManager = userManager;
        _products = products;
        _categories = categories;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? category, string? name)
    {
        //Retrieve or create an OrderViewModel from session
        var products = await _products.GetAllAsync();
        var categories = await _categories.GetAllAsync();

        if (!string.IsNullOrEmpty(category))
        {
            int categoryId = int.Parse(category);
            products = products.Where(p => p.Category != null && p.Category!.CategoryId == categoryId);
        }

        if (!string.IsNullOrEmpty(name))
        {
            products = products.Where(p => p.Name!.ToLower().Contains(name.ToLower()));
        }

        var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel") ?? new OrderViewModel
        {
            OrderItems = new List<OrderItemViewModel>(),
            Products = products.Where(p => p.Category == p.Category),
        };

        ViewBag.Categories = categories.Where(c => c != null).Distinct().ToList();
        ViewData["category"] = category;
        ViewData["name"] = name;


        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(int prodId, int prodQty)
    {
        var product = await _context.Products.FindAsync(prodId);
        if (product == null)
        {
            return NotFound("Product not found");
        }

        // Retrieve or create an OrderViewModel from session
        var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel") ?? new OrderViewModel
        {
            OrderItems = new List<OrderItemViewModel>(),
            Products = await _products.GetAllAsync()
        };

        // Check if the product is already in the order
        var existingItem = model.OrderItems?.FirstOrDefault(oi => oi.ProductId == prodId);

        // If the product is already in the order, update the quantity
        if (existingItem != null)
        {
            existingItem.Quantity += prodQty;
        }
        else
        {
            model.OrderItems?.Add(new OrderItemViewModel
            {
                ProductId = product.ProductId,
                Price = product.Price,
                Quantity = prodQty,
                ProductName = product.Name!
            });
        }

        // Update the total amount
        model.TotalAmount = model.OrderItems!.Sum(oi => oi.Price * oi.Quantity);

        // Save updated OrderViewModel to session
        HttpContext.Session.Set("OrderViewModel", model);

        // Redirect back to Create to show updated order items
        return RedirectToAction("Create", model);
    }

    [HttpGet]
    public async Task<IActionResult> Cart()
    {

        // Retrieve the OrderViewModel from session or other state management
        var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");

        if (model == null || model.OrderItems?.Count == 0)
        {
            return RedirectToAction("Create");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder()
    {
        var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
        if (model == null || model.OrderItems?.Count == 0)
        {
            return RedirectToAction("Create");
        }

        // Create a new Order entity
        Order order = new Order
        {
            OrderDate = DateTime.Now,
            TotalAmount = model.TotalAmount,
            UserId = _userManager.GetUserId(User)
        };

        // Add OrderItems to the Order entity
        foreach (var item in model.OrderItems!)
        {
            order.OrderItems?.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            });
        }

        // Save the Order entity to the database
        await _orders.AddAsync(order);

        // Clear the OrderViewModel from session or other state management
        HttpContext.Session.Remove("OrderViewModel");

        // Redirect to the Order Confirmation page
        return RedirectToAction("ViewOrders");
    }

    [HttpGet]
    public async Task<IActionResult> ViewOrders()
    {
        var userId = _userManager.GetUserId(User);

        var userOrders = await _orders.GetAllByIdAsync(userId, "UserId", new QueryOptions<Order>
        {
            Includes = "OrderItems.Product"
        });

        return View(userOrders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        var userId = _userManager.GetUserId(User);

        var userOrders = await _orders.GetAllByIdAsync(userId, "UserId", new QueryOptions<Order>
        {
            Includes = "OrderItems.Product"
        });

        var order = userOrders.FirstOrDefault(o => o.OrderId == orderId);

        if (order?.UserId != userId)
        {
            return Forbid("You are not authorized to delete this order");
        }

        if (order != null)
        {
            await _orders.DeleteAsync(orderId);
        }

        return RedirectToAction("ViewOrders");
    }
}
