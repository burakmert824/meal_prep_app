using MealPrepper.Core.DTOs;
using MealPrepper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPrepper.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await userService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var user = await userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
}
