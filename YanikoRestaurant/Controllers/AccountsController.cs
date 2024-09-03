using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YanikoRestaurant.Models;

namespace YanikoRestaurant.Controllers;

[Authorize(Roles = "Admin")]
public class AccountsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult AddUserToRole()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddUserToRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"User with ID {userId} not found.");
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (result.Succeeded)
        {
            return Ok($"User '{user.UserName}' added to role '{role}'.");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpGet]
    public IActionResult ChangeUserRole()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ChangeUserRole(ChangeUserRoleViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound($"User with ID {model.UserId} not found.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            foreach (var error in removeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }

        var addResult = await _userManager.AddToRoleAsync(user, model.NewRole);
        if (addResult.Succeeded)
        {
            return Ok($"User '{user.UserName}' role changed to '{model.NewRole}'.");
        }

        foreach (var error in addResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }
}
