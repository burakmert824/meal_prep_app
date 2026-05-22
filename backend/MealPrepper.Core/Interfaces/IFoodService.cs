using MealPrepper.Core.DTOs;

namespace MealPrepper.Core.Interfaces;

public interface IFoodService
{
    Task<IEnumerable<FoodDto>> GetByUserAsync(Guid userId, string? search);
    Task<FoodDto?> GetByIdAsync(Guid userId, Guid id);
    Task<FoodDto> CreateAsync(Guid userId, CreateFoodDto dto);
    Task<FoodDto?> UpdateAsync(Guid userId, Guid id, UpdateFoodDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
