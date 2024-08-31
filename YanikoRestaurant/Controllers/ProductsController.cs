using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YanikoRestaurant.Data;
using YanikoRestaurant.Models;
using YanikoRestaurant.Repository;

namespace YanikoRestaurant.Controllers
{
    public class ProductsController : Controller
    {
        private readonly Repository<Product> _products;
        private readonly Repository<Ingredient> _ingredients;
        private readonly Repository<Category> _categories;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _products = new Repository<Product>(context);
            _ingredients = new Repository<Ingredient>(context);
            _categories = new Repository<Category>(context);
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        public async Task<IActionResult> Index(string name, string category, decimal? price)
        {
            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            if (!string.IsNullOrEmpty(name))
            {
                products = products.Where(p => p.Name!.ToLower().Contains(name.ToLower()));
            }

            if (!string.IsNullOrEmpty(category))
            {
                int categoryId = int.Parse(category);
                products = products.Where(p => p.Category!.CategoryId == categoryId);
            }

            if (price.HasValue)
            {
                products = products.Where(p => p.Price <= price.Value);
            }

            ViewBag.Categories = await _categories.GetAllAsync();
            ViewData["name"] = name;
            ViewData["category"] = category;
            ViewData["price"] = price;

            return View(await products.ToListAsync());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AddEdit(int id)
        {
            ViewBag.Ingredients = await _ingredients.GetAllAsync();
            ViewBag.Categories = await _categories.GetAllAsync();

            if (id == 0)
            {
                ViewBag.Operation = "Add";
                return View(new Product());
            }
            else
            {
                Product product = await _products.GetByIdAsync(id, new QueryOptions<Product>
                {
                    Includes = "ProductIngredients.Ingredient, Category"
                });

                ViewBag.Operation = "Edit";
                return View(product);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddEdit(Product product, int[] ingredientIds, int catId)
        {
            ViewBag.Ingredients = await _ingredients.GetAllAsync();
            ViewBag.Categories = await _categories.GetAllAsync();

            if (ModelState.IsValid)
            {
                if (product.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = uniqueFileName;
                }

                if (product.ProductId == 0)
                {
                    product.CategoryId = catId;

                    //add ingredients
                    foreach (int id in ingredientIds)
                    {
                        product.ProductIngredients?.Add(new ProductIngredient { IngredientId = id, ProductId = product.ProductId });
                    }

                    await _products.AddAsync(product);
                    return RedirectToAction("Index", "Products");
                }
                else
                {
                    var existingProduct = await _products.GetByIdAsync(product.ProductId, new QueryOptions<Product> { Includes = "ProductIngredients" });

                    if (existingProduct == null)
                    {
                        ModelState.AddModelError("", "Product not found.");
                        ViewBag.Ingredients = await _ingredients.GetAllAsync();
                        ViewBag.Categories = await _categories.GetAllAsync();
                        return View(product);
                    }

                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;
                    existingProduct.CategoryId = catId;

                    // Update product ingredients
                    existingProduct.ProductIngredients?.Clear();

                    foreach (int id in ingredientIds)
                    {
                        existingProduct.ProductIngredients?.Add(new ProductIngredient { IngredientId = id, ProductId = product.ProductId });
                    }

                    try
                    {
                        await _products.UpdateAsync(existingProduct);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error: {ex.GetBaseException().Message}");
                        ViewBag.Ingredients = await _ingredients.GetAllAsync();
                        ViewBag.Categories = await _categories.GetAllAsync();
                        return View(product);
                    }
                }
            }
            return RedirectToAction("Index", "Products");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _products.DeleteAsync(id);
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("", "Product not found.");
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Product product = await _products.GetByIdAsync(id, new QueryOptions<Product>
            {
                Includes = "ProductIngredients, Category"
            });

            if (product == null)
            {
                return NotFound("Product not found");
            }

            // Delete the image file
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", product.ImageUrl);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}