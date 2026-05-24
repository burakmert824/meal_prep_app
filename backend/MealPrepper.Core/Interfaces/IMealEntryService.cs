using MealPrepper.Core.DTOs;

namespace MealPrepper.Core.Interfaces;

public interface IMealEntryService
{
    Task<List<MealEntryDto>> GetRangeAsync(Guid userId, DateTime from, DateTime to);
    Task<MealEntryDto> CreateAsync(Guid userId, CreateMealEntryDto dto);
    Task<MealEntryDto?> UpdateAsync(Guid userId, Guid id, UpdateMealEntryDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
