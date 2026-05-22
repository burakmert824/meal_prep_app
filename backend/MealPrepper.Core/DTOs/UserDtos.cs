namespace MealPrepper.Core.DTOs;

public record UserDto(Guid Id, string Name, DateTime CreatedAt);

public record CreateUserDto(string Name);
