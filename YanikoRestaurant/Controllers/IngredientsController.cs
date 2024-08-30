using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YanikoRestaurant.Data;
using YanikoRestaurant.Models;
using YanikoRestaurant.Repository;

namespace YanikoRestaurant.Controllers
{
    [Authorize]
    public class IngredientsController : Controller
    {
        private readonly Repository<Ingredient> _ingredients;

        public IngredientsController(ApplicationDbContext context)
        {
            _ingredients = new Repository<Ingredient>(context);
        }

        public async Task<IActionResult> Index()
        {
            var ingredients = await _ingredients.GetAllAsync();

            return View(ingredients);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ingredient = await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient>() { Includes = "ProductIngredients.Product" });

            if (ingredient == null)
            {
                return NotFound($"Ingredient with ID {id} not found");
            }

            return View(ingredient);
        }

        // Create Ingredient
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IngredientId, Name")] Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                await _ingredients.AddAsync(ingredient);

                return RedirectToAction("Index");
            }

            return View(ingredient);
        }


        // Edit Ingredient
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                await _ingredients.UpdateAsync(ingredient);

                return RedirectToAction("Index");
            }

            return View(ingredient);
        }


        // Delete Ingredient
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            return View(await _ingredients.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Ingredient ingredient)
        {
            await _ingredients.DeleteAsync(ingredient.IngredientId);

            return RedirectToAction("Index");
        }
    }
}