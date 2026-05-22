# Shared Agent Memory

All agents read this file at the start of their work and append to it when they finish.
This is the single source of truth for what has been built, decided, and tested.

---

## Architecture decisions
- Runtime: .NET 10 (not .NET 8 — that's what's installed on this machine)
- Frontend state: React Query for server state, Zustand for UI/client state
- API proxy: Vite dev server proxies `/api` → `http://localhost:5000`
- CSS: Tailwind CSS v4 via `@tailwindcss/vite` plugin

## Built so far
### Scaffold (2026-05-15)
- `backend/MealPrepper.sln` — solution with 4 projects
- `backend/MealPrepper.API` — ASP.NET Core web API
- `backend/MealPrepper.Core` — class library (entities, interfaces, DTOs)
- `backend/MealPrepper.Infrastructure` — class library (EF Core, repositories)
- `backend/MealPrepper.Tests` — xUnit test project
- Project references wired: API → Core + Infrastructure, Infrastructure → Core, Tests → all
- `frontend/` — Vite + React 18 + TypeScript
- `frontend/src/{api,components,pages,store,hooks,types}/` — folder structure created
- Dependencies installed: @tanstack/react-query, axios, zustand, tailwindcss
- `main.tsx` — QueryClientProvider wired up
- `App.tsx` — boilerplate cleared, clean starting point

## Tested so far
- Backend: `dotnet build` — 0 warnings, 0 errors
- Frontend: `npm run build` — clean build

## Known issues / deferred items
- Migration not yet created — run: `dotnet ef migrations add InitialCreate --project MealPrepper.Infrastructure --startup-project MealPrepper.API`
- No pages or components yet (frontend-dev phase pending)

---

### database-dev + backend-dev completed (2026-05-15) — PAUSED mid-session

**Packages installed:**
- MealPrepper.Infrastructure: EntityFrameworkCore, EntityFrameworkCore.Sqlite, EntityFrameworkCore.Design
- MealPrepper.API: EntityFrameworkCore.Design

**Files created:**
- `MealPrepper.Core/Entities/User.cs` — Id, Name, CreatedAt, UpdatedAt
- `MealPrepper.Core/Entities/Food.cs` — Id, UserId, Name, Unit, CaloriesPerUnit, CreatedAt, UpdatedAt
- `MealPrepper.Core/DTOs/UserDtos.cs` — UserDto, CreateUserDto
- `MealPrepper.Core/DTOs/FoodDtos.cs` — FoodDto, CreateFoodDto, UpdateFoodDto
- `MealPrepper.Core/Interfaces/IUserService.cs`
- `MealPrepper.Core/Interfaces/IFoodService.cs`
- `MealPrepper.Infrastructure/Data/AppDbContext.cs` — Fluent API config, Users + Foods tables
- `MealPrepper.Infrastructure/Services/UserService.cs`
- `MealPrepper.Infrastructure/Services/FoodService.cs`
- `MealPrepper.API/Controllers/UsersController.cs` — GET /api/users, GET /api/users/{id}, POST /api/users
- `MealPrepper.API/Controllers/FoodsController.cs` — GET/POST/PUT/DELETE /api/users/{userId}/foods
- `MealPrepper.API/Program.cs` — DbContext, services, CORS registered; auto-migrate on startup

**Build status:** ✅ 0 warnings, 0 errors

**All complete ✅**

---

### frontend-dev completed (2026-05-22)

**Packages installed:** react-router-dom@7

**Files created:**
- `src/types/user.ts` — User, CreateUserRequest
- `src/types/food.ts` — Food, CreateFoodRequest, UpdateFoodRequest
- `src/api/client.ts` — axios instance, baseURL = /api
- `src/api/users.ts` — getUsers, createUser
- `src/api/foods.ts` — getFoods, createFood, updateFood, deleteFood
- `src/store/userStore.ts` — Zustand store, persisted to localStorage as 'meal-prepper-user'
- `src/pages/ProfileSelectorPage.tsx` — profile picker + create profile modal
- `src/pages/FoodsPage.tsx` — food list with search, add/edit/delete, switch user
- `src/App.tsx` — BrowserRouter with routes: / → ProfileSelectorPage, /foods → FoodsPage (protected)

**Build status:** ✅ 0 errors

## How to run
**Backend:**
```bash
cd backend && dotnet run --project MealPrepper.API
# Runs on http://localhost:5000, auto-migrates DB on start
```
**Frontend:**
```bash
cd frontend && npm run dev
# Runs on http://localhost:5173, proxies /api → http://localhost:5000
```
