using MealPrepper.Core.DTOs;
using MealPrepper.Core.Entities;
using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPrepper.Infrastructure.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.CreatedAt))
            .ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : new UserDto(user.Id, user.Name, user.CreatedAt);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return new UserDto(user.Id, user.Name, user.CreatedAt);
    }
}
