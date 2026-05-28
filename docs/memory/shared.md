# Shared Agent Memory

All agents read this file at the start of their work and append to it when they finish.
This is the single source of truth for what has been built, decided, and tested.

---

## Architecture decisions
- Runtime: .NET 10 (not .NET 8 — that's what's installed on this machine)
- Frontend state: React Query for server state, Zustand for UI/client state
- API proxy: Vite dev server proxies `/api` → `http://localhost:5051` (backend runs on 5051, NOT 5000)
- CSS: Tailwind CSS v4 via `@tailwindcss/vite` plugin
- Agent teams: VSCode extension does not support Shift+Down teammate navigation — orchestrator does work directly in main session instead of spawning teammates

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

---

### backend-dev: Meal Planning + Shopping List (2026-05-22)

**New entities (MealPrepper.Core/Entities/):**
- `MealSlot.cs` — enum: Breakfast=0, Lunch=1, Dinner=2, Snack=3
- `MealPlan.cs` — Id, UserId, Name, StartDate, EndDate, CreatedAt, UpdatedAt; nav: User, Entries, ShoppingList
- `MealPlanEntry.cs` — Id, MealPlanId, RecipeId, Date, MealSlot, PortionMultiplier; nav: MealPlan, Recipe
- `ShoppingList.cs` — Id, MealPlanId, GeneratedAt; nav: MealPlan, Items
- `ShoppingListItem.cs` — Id, ShoppingListId, FoodId, TotalQuantity, IsChecked; nav: ShoppingList, Food

**Navigation properties added to existing entities:**
- `User.cs` — added `ICollection<MealPlan> MealPlans`
- `Recipe.cs` — added `ICollection<MealPlanEntry> MealPlanEntries`
- `Food.cs` — added `ICollection<ShoppingListItem> ShoppingListItems`

**New DTOs (MealPrepper.Core/DTOs/):**
- `MealPlanDtos.cs` — MealPlanDto, MealPlanSummaryDto, CreateMealPlanDto, UpdateMealPlanDto, MealPlanEntryDto, CreateMealPlanEntryDto, UpdateMealPlanEntryDto
- `ShoppingListDtos.cs` — ShoppingListDto, ShoppingListItemDto, ToggleShoppingListItemDto

**New interfaces (MealPrepper.Core/Interfaces/):**
- `IMealPlanService.cs`
- `IShoppingListService.cs`

**New services (MealPrepper.Infrastructure/Services/):**
- `MealPlanService.cs` — full CRUD for plans + entries; date-range validation; user-ownership checks
- `ShoppingListService.cs` — get, generate (aggregates ingredients × PortionMultiplier), toggleItem

**New controllers (MealPrepper.API/Controllers/):**
- `MealPlansController.cs` — `[Route("api/users/{userId}/meal-plans")]`; 8 endpoints (CRUD plans + CRUD entries)
- `ShoppingListController.cs` — `[Route("api/users/{userId}/meal-plans/{planId}/shopping-list")]`; GET, POST /generate, PATCH /items/{itemId}

**AppDbContext updated:**
- 4 new DbSets: MealPlans, MealPlanEntries, ShoppingLists, ShoppingListItems
- Fluent API: MealPlan (cascade from User), MealPlanEntry (cascade from MealPlan, restrict from Recipe), ShoppingList (unique index on MealPlanId, cascade from MealPlan), ShoppingListItem (cascade from ShoppingList, restrict from Food)

**Program.cs updated:** IMealPlanService and IShoppingListService registered as Scoped.

**Migration pending — run:**
```bash
cd /Users/burakersoz/Desktop/fastcode/meal_prepper/backend
dotnet ef migrations add AddMealPlanning --project MealPrepper.Infrastructure --startup-project MealPrepper.API
dotnet ef database update --project MealPrepper.Infrastructure --startup-project MealPrepper.API
```

**Build status:** Code complete — awaiting user to run build + migration verification.

---

### backend-dev: CalendarRefactor — MealEntry + on-demand ShoppingList (2026-05-24)

**Breaking change:** MealPlan / MealPlanEntry model removed. Replaced with flat MealEntry. ShoppingList is now user-scoped (one per user) and generated on demand for any date range.

**Deleted:**
- `MealPrepper.Core/Entities/MealPlan.cs`
- `MealPrepper.Core/Entities/MealPlanEntry.cs`
- `MealPrepper.Core/Interfaces/IMealPlanService.cs`
- `MealPrepper.Infrastructure/Services/MealPlanService.cs`
- `MealPrepper.API/Controllers/MealPlansController.cs`
- `MealPrepper.Core/DTOs/MealPlanDtos.cs`

**New / replaced entity files (MealPrepper.Core/Entities/):**
- `MealEntry.cs` — Id, UserId, RecipeId, Date, MealSlot, PortionMultiplier; nav: User, Recipe
- `ShoppingList.cs` — Id, UserId, FromDate, ToDate, GeneratedAt; nav: User, Items (was MealPlanId-scoped)
- `User.cs` — removed MealPlans nav; added MealEntries + ShoppingList navs
- `Recipe.cs` — removed MealPlanEntries nav; added MealEntries nav

**New / replaced DTOs (MealPrepper.Core/DTOs/):**
- `MealEntryDtos.cs` — MealEntryDto, CreateMealEntryDto, UpdateMealEntryDto
- `ShoppingListDtos.cs` — ShoppingListDto (now has FromDate/ToDate/UserId), ShoppingListItemDto, GenerateShoppingListDto, ToggleShoppingListItemDto

**New / replaced interfaces (MealPrepper.Core/Interfaces/):**
- `IMealEntryService.cs` — GetRangeAsync, CreateAsync, UpdateAsync, DeleteAsync
- `IShoppingListService.cs` — GetAsync(userId), GenerateAsync(userId, from, to), ToggleItemAsync(userId, itemId, isChecked)

**New / replaced services (MealPrepper.Infrastructure/Services/):**
- `MealEntryService.cs` — full implementation of IMealEntryService
- `ShoppingListService.cs` — generate aggregates MealEntries in range; one ShoppingList per user (replaces on regenerate)

**New / replaced controllers (MealPrepper.API/Controllers/):**
- `MealEntriesController.cs` — `[Route("api/users/{userId}/meal-entries")]`; GET (range query), POST, PUT /{id}, DELETE /{id}
- `ShoppingListController.cs` — `[Route("api/users/{userId}/shopping-list")]`; GET, POST /generate, PATCH /items/{itemId}

**AppDbContext:** Replaced entirely — removed MealPlans/MealPlanEntries DbSets, added MealEntries; ShoppingList FK changed from MealPlanId to UserId (unique index).

**Program.cs:** Replaced IMealPlanService with IMealEntryService registration.

**Migration pending — run these commands:**
```bash
cd /Users/burakersoz/Desktop/fastcode/meal_prepper/backend
dotnet build
dotnet ef migrations add CalendarRefactor --project MealPrepper.Infrastructure --startup-project MealPrepper.API
dotnet ef database update --project MealPrepper.Infrastructure --startup-project MealPrepper.API
```

Note: The generated migration will automatically emit DROP TABLE for MealPlanEntries and MealPlans, and CREATE TABLE for MealEntries plus ALTER TABLE for ShoppingLists (MealPlanId→UserId). If EF complains about constraints, manually add `migrationBuilder.DropTable("MealPlanEntries"); migrationBuilder.DropTable("MealPlans");` at the top of the `Up()` method before running `database update`.

**Build status:** Code complete — build + migration not yet verified (Bash access required).

---

### frontend-dev: Meal Planning + Shopping List (2026-05-22)

**No new packages installed** — all deps already present.

**New type files (src/types/):**
- `mealPlan.ts` — MealSlot, MEAL_SLOTS, MealPlanEntry, MealPlan, MealPlanSummary, CreateMealPlanRequest, CreateMealPlanEntryRequest, UpdateMealPlanEntryRequest
- `shoppingList.ts` — ShoppingListItem, ShoppingList

**New API files (src/api/):**
- `mealPlans.ts` — getMealPlans, getMealPlan, createMealPlan, updateMealPlan, deleteMealPlan, addEntry, updateEntry, deleteEntry
- `shoppingList.ts` — getShoppingList, generateShoppingList, toggleShoppingListItem

**New pages (src/pages/):**
- `MealPlansPage.tsx` — list of plans, create/delete with modal, defaults to current week (Mon–Sun), navigates to detail on create
- `MealPlanDetailPage.tsx` — calendar grid (dates as columns, meal slots as rows), add/delete entries per cell, edit/delete plan, breadcrumb, "Shopping List →" button
- `ShoppingListPage.tsx` — generate/regenerate list, optimistic checkbox toggle, unchecked first / checked below with strikethrough, 404-safe fetch (returns null)

**Updated files:**
- `src/App.tsx` — added 3 new ProtectedRoutes: /meal-plans, /meal-plans/:planId, /meal-plans/:planId/shopping-list
- `src/pages/FoodsPage.tsx` — added "Meal Plans" nav link
- `src/pages/RecipesPage.tsx` — added "Meal Plans" nav link

**Key implementation details:**
- Calendar grid uses `overflow-x-auto` for long date ranges; dates appended with `T00:00:00` to avoid UTC-offset day-shift bugs
- mealSlot sent to backend as integer (MEAL_SLOT_INDEX map); received from backend as string (MealSlot type)
- Shopping list 404 handled in queryFn (returns null) rather than as an error, enabling the empty-state Generate flow
- Optimistic update on toggle: cancels in-flight queries, patches cache, rolls back on error, invalidates on settle

**Build status:** ✅ 0 TypeScript errors (verified via npm run build)

---

### frontend-dev: Calendar UI Rebuild (2026-05-24)

**Files deleted (replaced):**
- `src/pages/MealPlansPage.tsx`
- `src/pages/MealPlanDetailPage.tsx`
- `src/pages/ShoppingListPage.tsx`
- `src/api/mealPlans.ts`
- `src/api/shoppingList.ts`
- `src/types/mealPlan.ts`
- `src/types/shoppingList.ts`

**New type files (src/types/):**
- `mealEntry.ts` — MealSlot, MEAL_SLOTS, MealEntry, CreateMealEntryRequest
- `shoppingList.ts` — ShoppingListItem, ShoppingList (user-scoped, fromDate/toDate/generatedAt)

**New API files (src/api/):**
- `mealEntries.ts` — getMealEntries (range query), createMealEntry, updateMealEntry, deleteMealEntry
- `shoppingList.ts` — getShoppingList (404→null), generateShoppingList, toggleShoppingListItem

**New pages (src/pages/):**
- `CalendarPage.tsx` — replaces MealPlansPage + MealPlanDetailPage; three-view calendar (Day/Week/Month); inline EntryChip component; add-entry modal with recipe picker + portion multiplier; month view switches to day view on "+ Add" click
- `ShoppingListPage.tsx` — date range pickers default to current Mon–Sun; Generate button creates/replaces list; optimistic checkbox toggle; unchecked items first, checked below with strikethrough; "N of M items checked" counter

**Updated files:**
- `src/App.tsx` — routes simplified to /meal-plans (CalendarPage) + /shopping-list (ShoppingListPage); removed /meal-plans/:planId
- `src/pages/FoodsPage.tsx` — added "Shopping List" nav link (4-link nav: Foods active | Recipes | Meal Plans | Shopping List)
- `src/pages/RecipesPage.tsx` — added "Shopping List" nav link (4-link nav: Foods | Recipes active | Meal Plans | Shopping List)

**Key implementation details:**
- Dates always constructed as `new Date(str + 'T00:00:00')` to avoid UTC-offset day-shift bugs
- Week view: scrollable table, Mon–Sun columns, 4 meal-slot rows; today's date highlighted in indigo
- Month view: full grid from Monday before month-start to Sunday after month-end
- Shopping list optimistic toggle: cancel queries → patch cache → rollback on error → invalidate on settle
- Shopping list 404 handled in queryFn (returns null), enabling the empty-state Generate flow

**Build status:** Build not yet run — Bash access was unavailable during this session. Run `npm run build` from frontend/ to verify.

---

### Backend tests added (2026-05-28)

**Test project:** `backend/MealPrepper.Tests`

**Packages added to MealPrepper.Tests.csproj:**
- `FluentAssertions` 7.2.0
- `Microsoft.Data.Sqlite` 10.0.8
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.8

**Test DB pattern:** SQLite in-memory with a kept-open `SqliteConnection` + `db.Database.EnsureCreated()`. Each test class creates a fresh DB per test via a `CreateDb()` helper.

**Files created:**
- `FoodServiceTests.cs`: covers `FoodService` — GetByUserAsync (user isolation, search filter case-insensitive, empty list), GetByIdAsync (found, wrong user, missing id), CreateAsync (field persistence + whitespace trim), UpdateAsync (all fields, wrong user), DeleteAsync (removes, wrong user, missing id). 14 tests.
- `RecipeServiceTests.cs`: covers `RecipeService` — GetByUserAsync (user isolation, ingredients included, search, empty), GetByIdAsync (found, wrong user, missing id), CreateAsync (single and multiple ingredients), UpdateAsync (name/portion/ingredient replace, wrong user), DeleteAsync (removes, wrong user, missing id). 17 tests.
- `MealEntryServiceTests.cs`: covers `MealEntryService` — GetRangeAsync (in-range, boundary inclusion, wrong user, empty), CreateAsync (all fields, wrong-user recipe throws, all MealSlot values via Theory), UpdateAsync (portionMultiplier, wrong user, missing id), DeleteAsync (removes, wrong user, missing id). 16 tests.
- `ShoppingListServiceTests.cs`: covers `ShoppingListService` — GetAsync (null when none, returns list with items, wrong user returns null), GenerateAsync (correct quantity = ingredient × portionMultiplier, quantities summed across entries for same food, two foods = two items, out-of-range entry excluded, calling twice replaces old list, from/to dates stored), ToggleItemAsync (flip to true, flip to false, wrong user returns null, missing id returns null). 14 tests.

**Total: 58 tests — 58 passed, 0 failed.**
