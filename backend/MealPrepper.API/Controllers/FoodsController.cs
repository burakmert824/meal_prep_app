using MealPrepper.Core.DTOs;
using MealPrepper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPrepper.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/foods")]
public class FoodsController(IFoodService foodService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId, [FromQuery] string? search) =>
        Ok(await foodService.GetByUserAsync(userId, search));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid userId, Guid id)
    {
        var food = await foodService.GetByIdAsync(userId, id);
        return food is null ? NotFound() : Ok(food);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, [FromBody] CreateFoodDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");
        if (string.IsNullOrWhiteSpace(dto.Unit))
            return BadRequest("Unit is required.");

        var food = await foodService.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { userId, id = food.Id }, food);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid userId, Guid id, [FromBody] UpdateFoodDto dto)
    {
        var food = await foodService.UpdateAsync(userId, id, dto);
        return food is null ? NotFound() : Ok(food);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid id)
    {
        var deleted = await foodService.DeleteAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }
}
