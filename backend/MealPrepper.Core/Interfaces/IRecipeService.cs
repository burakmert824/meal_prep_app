using MealPrepper.Core.DTOs;

namespace MealPrepper.Core.Interfaces;

public interface IRecipeService
{
    Task<IEnumerable<RecipeDto>> GetByUserAsync(Guid userId, string? search);
    Task<RecipeDto?> GetByIdAsync(Guid userId, Guid id);
    Task<RecipeDto> CreateAsync(Guid userId, CreateRecipeDto dto);
    Task<RecipeDto?> UpdateAsync(Guid userId, Guid id, UpdateRecipeDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}