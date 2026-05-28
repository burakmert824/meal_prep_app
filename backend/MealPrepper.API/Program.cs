using MealPrepper.Core.Interfaces;
using MealPrepper.Infrastructure.Data;
using MealPrepper.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (databaseUrl != null)
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseUrl));
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

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
