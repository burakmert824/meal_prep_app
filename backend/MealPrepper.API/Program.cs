using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using MealPrepper.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (databaseUrl != null)
{
    string connectionString;

    if (databaseUrl.StartsWith("postgresql://") || databaseUrl.StartsWith("postgres://"))
    {
        // URI format: postgresql://user:pass@host:port/db  — convert to key-value
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
                           $"Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};" +
                           $"SSL Mode=Require;Trust Server Certificate=true";
    }
    else
    {
        // Already in key-value format: Host=...;Port=...;Username=...
        connectionString = databaseUrl;
    }

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=mealprepper.db"));
}

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IMealEntryService, MealEntryService>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();

var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',')
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (databaseUrl != null)
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

var enableDocs = app.Environment.IsDevelopment() ||
                 Environment.GetEnvironmentVariable("ENABLE_API_DOCS") == "true";

if (enableDocs)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", database = ex.Message }, statusCode: 503);
    }
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
