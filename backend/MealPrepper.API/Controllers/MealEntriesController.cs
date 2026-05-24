using MealPrepper.Core.DTOs;
using MealPrepper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPrepper.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/meal-entries")]
public class MealEntriesController(IMealEntryService mealEntryService) : ControllerBase
{
    /// <summary>Returns all meal entries for a user within the given date range.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRange(Guid userId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (from > to) return BadRequest("from must be on or before to.");
        return Ok(await mealEntryService.GetRangeAsync(userId, from, to));
    }

    /// <summary>Creates a new meal entry for the user.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, [FromBody] CreateMealEntryDto dto)
    {
        if (dto.PortionMultiplier <= 0) return BadRequest("PortionMultiplier must be greater than 0.");
        try
        {
            var entry = await mealEntryService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetRange), new { userId }, entry);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Updates the portion multiplier of an existing meal entry.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid userId, Guid id, [FromBody] UpdateMealEntryDto dto)
    {
        if (dto.PortionMultiplier <= 0) return BadRequest("PortionMultiplier must be greater than 0.");
        var entry = await mealEntryService.UpdateAsync(userId, id, dto);
        return entry is null ? NotFound() : Ok(entry);
    }

    /// <summary>Deletes a meal entry.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid id)
    {
        var deleted = await mealEntryService.DeleteAsync(userId, id);
        return deleted ? NoContent() : NotFound();
    }
}
