using Microsoft.AspNetCore.Mvc;
using YanikoRestaurant.Data;
using YanikoRestaurant.Models;
using YanikoRestaurant.Repository;

namespace YanikoRestaurant.Controllers
{
    public class IngredientsController : Controller
    {
        private readonly Repository<Ingredient> repo;

        public IngredientsController(ApplicationDbContext context)
        {
            repo = new Repository<Ingredient>(context);
        }

        public async Task<IActionResult> Index()
        {
            var ingredients = await repo.GetAllAsync();

            return View(ingredients);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ingredient = await repo.GetByIdAsync(id, new QueryOptions<Ingredient>() { Includes = "ProductIngredients.Product" });

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
                await repo.AddAsync(ingredient);

                return RedirectToAction("Index");
            }

            return View(ingredient);
        }


        // Edit Ingredient
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await repo.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                await repo.UpdateAsync(ingredient);

                return RedirectToAction("Index");
            }

            return View(ingredient);
        }


        // Delete Ingredient
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            return View(await repo.GetByIdAsync(id, new QueryOptions<Ingredient> { Includes = "ProductIngredients.Product" }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Ingredient ingredient)
        {
            await repo.DeleteAsync(ingredient.IngredientId);

            return RedirectToAction("Index");
        }
    }
}