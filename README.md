# Meal Prepper

A meal prep planning web app. Users build a food library, create recipes from those foods, plan meals on a calendar, and get an auto-generated shopping list scaled to their portions.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | C# ASP.NET Core (.NET 10), Entity Framework Core |
| Database | SQLite (local dev) / PostgreSQL (staging + production) |
| Frontend | React 18, TypeScript, Vite, Tailwind CSS v4 |
| State | TanStack Query (server state), Zustand (UI state) |
| Hosting | Railway (backend + DB), Vercel (frontend) |
| Tests | xUnit + EF Core SQLite in-memory (backend), Vitest + RTL (frontend) |

---

## Project Structure

```
meal_prepper/
├── backend/
│   ├── MealPrepper.API/           # Controllers, Program.cs, Dockerfile
│   ├── MealPrepper.Core/          # Entities, DTOs, interfaces
│   ├── MealPrepper.Infrastructure/# EF Core DbContext, services, migrations
│   └── MealPrepper.Tests/         # xUnit unit tests
└── frontend/
    └── src/
        ├── api/        # Axios calls, one file per domain
        ├── pages/      # Route-level page components
        ├── components/ # Reusable UI components
        ├── store/      # Zustand stores
        ├── hooks/      # Custom React hooks
        └── types/      # TypeScript interfaces
```

---

## Running Locally

### Backend
```bash
cd backend
dotnet run --project MealPrepper.API
# API runs on http://localhost:5051
# Scalar API explorer: http://localhost:5051/scalar/v1
# Health check: http://localhost:5051/health
```

For auto-restart on file changes:
```bash
dotnet watch run --project MealPrepper.API
```

### Frontend
```bash
cd frontend
npm install
npm run dev
# Runs on http://localhost:5173
```

### Running Tests
```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

---

## Environment Variables

### Backend

| Variable | Required | Description |
|---|---|---|
| `DATABASE_URL` | Production only | PostgreSQL connection URI from Railway. If absent, falls back to SQLite. |
| `ALLOWED_ORIGINS` | Production only | Comma-separated list of allowed frontend URLs for CORS. Defaults to `http://localhost:5173`. |
| `ENABLE_API_DOCS` | Optional | Set to `true` to expose Scalar API explorer. Always on in development. Off by default in production. |
| `ASPNETCORE_ENVIRONMENT` | Production | Set to `Production` on Railway. |

### Frontend

| Variable | Required | Description |
|---|---|---|
| `VITE_API_URL` | Production only | Backend URL (e.g. `https://your-app.railway.app`). Defaults to `http://localhost:5051`. |

Local frontend config goes in `frontend/.env.local` (never committed to git).

---

## API Reference

All endpoints are prefixed with `/api`. Every resource is user-scoped.

### Users
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users` | List all users |
| `GET` | `/api/users/{id}` | Get user by ID |
| `POST` | `/api/users` | Create user |

### Foods
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users/{userId}/foods` | List foods (optional `?search=`) |
| `GET` | `/api/users/{userId}/foods/{id}` | Get food by ID |
| `POST` | `/api/users/{userId}/foods` | Create food |
| `PUT` | `/api/users/{userId}/foods/{id}` | Update food |
| `DELETE` | `/api/users/{userId}/foods/{id}` | Delete food |

### Recipes
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users/{userId}/recipes` | List recipes (optional `?search=`) |
| `GET` | `/api/users/{userId}/recipes/{id}` | Get recipe by ID |
| `POST` | `/api/users/{userId}/recipes` | Create recipe with ingredients |
| `PUT` | `/api/users/{userId}/recipes/{id}` | Update recipe — replaces all ingredients |
| `DELETE` | `/api/users/{userId}/recipes/{id}` | Delete recipe |

### Meal Entries (Calendar)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users/{userId}/meal-entries?from=&to=` | Get entries in date range |
| `POST` | `/api/users/{userId}/meal-entries` | Add recipe to a date + meal slot |
| `PUT` | `/api/users/{userId}/meal-entries/{id}` | Update portion multiplier |
| `DELETE` | `/api/users/{userId}/meal-entries/{id}` | Remove entry |

Meal slots: `Breakfast`, `Lunch`, `Dinner`, `Snack`

### Shopping List
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users/{userId}/shopping-list` | Get current shopping list |
| `POST` | `/api/users/{userId}/shopping-list/generate` | Generate list from meal entries in date range |
| `PATCH` | `/api/users/{userId}/shopping-list/items/{itemId}` | Toggle item checked/unchecked |

---

## Data Model

```
User
 ├── Foods          (name, unit, caloriesPerUnit, proteinPerUnit)
 ├── Recipes        (name, defaultPortionSize)
 │    └── RecipeIngredients  (food + quantity)
 ├── MealEntries    (recipe + date + mealSlot + portionMultiplier)
 └── ShoppingList   (fromDate, toDate, generatedAt)
      └── ShoppingListItems  (food + totalQuantity)
```

Shopping list quantities are calculated as:
`ingredient.quantity × (entry.portionMultiplier / recipe.defaultPortionSize)`
Items for the same food across multiple entries are summed together.

---

## Features Built

### Core
- **User profiles** — Netflix-style profile selector, no passwords
- **Foods** — Create/edit/delete foods with name, unit, calories and protein per unit. Reference amount input (e.g. per 100g). Duplicate name detection.
- **Recipes** — Create/edit/delete recipes with multiple ingredients and quantities. Default portion size.
- **Calendar** — Day / Week / Month views. Add recipes to meal slots (Breakfast, Lunch, Dinner, Snack). Edit portion multiplier. Delete entries.
- **Shopping List** — Pick a date range, generate a list. Items are summed and scaled by portion. Check items off as you shop.

### Quality
- **Error handling** — Toast notifications on every API failure and key success actions (Sonner)
- **Health check** — `GET /health` checks database connectivity, returns 200/503
- **API explorer** — Scalar UI available in development and when `ENABLE_API_DOCS=true`
- **58 backend unit tests** — FoodService, RecipeService, MealEntryService, ShoppingListService

### Infrastructure
- **Dual database** — SQLite for local dev (zero config), PostgreSQL for staging/production
- **Railway deployment** — Docker-based, auto-deploys from `main` branch
- **Environment-based config** — Connection strings, CORS origins, and API docs controlled via environment variables

---

## Key Technical Decisions

**Why SQLite locally + PostgreSQL in production?**
SQLite needs zero setup for local dev. PostgreSQL handles concurrent writes and persistent storage properly in the cloud. EF Core migrations handle both — switching is one environment variable.

**Why no auth yet?**
App is designed for personal use. User isolation is enforced at the API level (every query filters by `userId`). Auth (JWT or similar) can be added later without changing the data model.

**Why flat MealEntry instead of a MealPlan container?**
Simpler model. The shopping list is generated ad-hoc from a date range query, so there's no need to group entries into named plans. This also makes the calendar feel natural.

**Why store MealSlot as int in the DB?**
Efficient storage. The JSON API serializes it as a string (`"Breakfast"` not `0`) via `JsonStringEnumConverter`, so the frontend always works with readable values.

---

## Deployment

### Railway (backend)
- Root directory: `backend/`
- Build: Dockerfile
- Branches: `main` → production, `develop` → staging
- Required variables: `DATABASE_URL` (auto-set by Railway PostgreSQL addon), `ALLOWED_ORIGINS`, `ASPNETCORE_ENVIRONMENT=Production`

### Vercel (frontend)
- Root directory: `frontend/`
- Build command: `npm run build`
- Output: `dist/`
- Required variables: `VITE_API_URL`

---

## Roadmap

### Phase 1 — Stability ✅
- [x] Error handling — toast notifications on all API failures
- [x] Backend unit tests — 58 tests across all services
- [x] Deployment — Railway (backend + PostgreSQL) + Vercel (frontend)

### Phase 2 — UX & Core Gaps
- [ ] UX improvements — friction in adding food, picking ingredients, calendar interactions
- [ ] External meals — freeform entry with name, optional kcal/protein, exclude from shopping list flag
- [ ] Calendar swipe gestures — touch navigation

### Phase 3 — Power Features
- [ ] JSON / AI recipe import — paste JSON, review ingredients, save
- [ ] Nutrition dashboard — daily/weekly calorie + protein totals
- [ ] Export & backup — JSON download of all data

### Phase 4 — Future
- [ ] Mobile app — React Native, same API
- [ ] Market / cost integration — food prices, cheapest shopping list
