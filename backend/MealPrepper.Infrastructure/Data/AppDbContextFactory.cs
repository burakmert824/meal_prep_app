using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MealPrepper.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=mealprepper;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
