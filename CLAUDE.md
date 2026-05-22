# Meal Prepper — Project Guide

## What this app is
A meal prep planning web application. Users save their foods and recipes, plan their meals across a chosen date range, and get an auto-generated shopping list scaled to their portions. A mobile app will follow — keep APIs clean and RESTful.

## Tech stack
- **Backend:** C# ASP.NET Core (.NET 10), Entity Framework Core, SQLite (dev) / SQL Server (prod)
- **Frontend:** React 18, TypeScript, Vite, Tailwind CSS, React Query (server state), Zustand (UI state)
- **Auth:** No login screen for now — users select their profile on the home page (like Netflix). The User entity and user-scoped data model must be in place from day one.
- **Offline / local storage:** Under discussion. Web uses the backend DB. Keep this in mind for the future mobile app — do not couple data fetching in a way that blocks offline support later.

## Project structure
```
meal_prepper/
├── backend/          # ASP.NET Core solution
│   ├── MealPrepper.API/
│   ├── MealPrepper.Core/       # entities, interfaces, DTOs
│   ├── MealPrepper.Infrastructure/  # EF Core, repositories
│   └── MealPrepper.Tests/
├── frontend/         # Vite + React app
│   ├── src/
│   │   ├── api/          # axios calls, one file per domain
│   │   ├── components/   # reusable UI components
│   │   ├── pages/        # route-level page components
│   │   ├── store/        # Zustand stores
│   │   ├── hooks/        # custom React hooks
│   │   └── types/        # TypeScript interfaces
└── docs/
    ├── requirements.md
    └── memory/
        └── shared.md     # agent shared memory — read before starting, update when done
```

## Core domain
| Entity | Belongs to | Notes |
|---|---|---|
| User | — | Profile name + avatar only, no password yet |
| Food | User | An ingredient — name, unit, calories per unit |
| Recipe | User | Name, portion size, list of foods with quantities |
| MealPlan | User | A named plan with a start and end date |
| MealPlanEntry | MealPlan | Recipe assigned to a specific date + meal slot |
| ShoppingList | MealPlan | Auto-generated; one list per plan |
| ShoppingListItem | ShoppingList | Food + total quantity (summed + scaled by portion) |

## Key rules for all agents
- **Always read `docs/memory/shared.md` before starting** — it tracks what has been built and decided
- **Always append to `docs/memory/shared.md` when done** — keep it up to date
- All data is user-scoped — every query must filter by `userId`
- No hardcoded secrets or connection strings — use environment variables
- Keep it simple — do not add features not listed in `docs/requirements.md`
- Mobile app will consume the same API later — keep response shapes clean and consistent