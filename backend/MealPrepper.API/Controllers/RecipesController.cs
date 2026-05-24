using MealPrepper.Core.DTOs;
using MealPrepper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPrepper.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/recipes")]
public class RecipesController(IRecipeService recipeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId, [FromQuery] string? search) =>
        Ok(await recipeService.GetByUserAsync(userId, search));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid userId, Guid id)
    {
        var recipe = await recipeService.GetByIdAsync(userId, id);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, [FromBody] CreateRecipeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var recipe = await recipeService.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { userId, id = recipe.Id }, recipe);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid userId, Guid id, [FromBody] UpdateRecipeDto dto)
    {
        var recipe = await recipeService.UpdateAsync(userId, id, dto);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid id)
    {
        var deleted = await recipeService.DeleteAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }
}