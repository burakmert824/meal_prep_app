using MealPrepper.Core.DTOs;
using MealPrepper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPrepper.API.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/shopping-list")]
public class ShoppingListController(IShoppingListService shoppingListService) : ControllerBase
{
    /// <summary>Returns the current shopping list for a user, or 404 if none has been generated yet.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(Guid userId)
    {
        var list = await shoppingListService.GetAsync(userId);
        return list is null ? NotFound() : Ok(list);
    }

    /// <summary>Generates (or regenerates) the shopping list for a user-chosen date range.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(Guid userId, [FromBody] GenerateShoppingListDto dto)
    {
        if (dto.FromDate > dto.ToDate) return BadRequest("FromDate must be on or before ToDate.");
        var list = await shoppingListService.GenerateAsync(userId, dto.FromDate, dto.ToDate);
        return Ok(list);
    }

    /// <summary>Toggles the IsChecked state of a shopping list item.</summary>
    [HttpPatch("items/{itemId:guid}")]
    public async Task<IActionResult> ToggleItem(Guid userId, Guid itemId, [FromBody] ToggleShoppingListItemDto dto)
    {
        var item = await shoppingListService.ToggleItemAsync(userId, itemId, dto.IsChecked);
        return item is null ? NotFound() : Ok(item);
    }
}
