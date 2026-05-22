# Requirements

> Keep it simple. Build only what is listed here.

---

## Users

- On first open, the app shows a profile selector screen (no login, no password)
- User can create a new profile with a name
- User selects their profile and all data shown is their own
- The selected profile is remembered in the browser (localStorage)
- All backend data is user-scoped — a user can never see another user's data

**Out of scope for now:** authentication, passwords, email, OAuth

---

## Foods (Ingredients)

- User can add a food with: name, unit of measure (g, ml, piece, etc.), calories per unit
- User can edit and delete their foods
- Foods are private to the user who created them
- Foods list is searchable by name

---

## Recipes

- User can create a recipe with: name, default portion size (e.g. 1 portion = the amounts listed)
- A recipe has a list of foods with a quantity per food
- User can edit and delete their recipes
- Recipes are private to the user who created them
- Recipes list is searchable by name

---

## Meal Planning

- User can create a meal plan with: a name and a date range (start date → end date)
- Default view when creating a plan: current week (Monday to Sunday)
- User can change the date range freely — shorter or longer than a week
- For each day in the plan, the user can assign recipes to meal slots: Breakfast, Lunch, Dinner, Snack
- Each meal plan entry has a portion multiplier (default: 1) — e.g. 2 means double the recipe quantities
- User can have multiple meal plans (e.g. "Week 1", "Bulk phase", etc.)
- User can view, edit, and delete their meal plans

---

## Shopping List

- A shopping list is generated from a meal plan
- It aggregates all ingredients across all recipes in the plan
- Quantities are scaled by each entry's portion multiplier
- If the same food appears in multiple recipes, quantities are summed
- The user can check off items as they shop
- The shopping list can be regenerated if the meal plan changes

---

## Discussion items (not building yet)

- **Offline / local storage:** For the web app, data lives in the backend. For the future mobile app, local caching may be needed. Keep API design clean so offline sync can be added without breaking changes.
- **Shared food database:** Currently foods are user-private. In the future there could be a global food library users can import from.
- **Nutrition summary:** Recipes have calorie data via foods. A daily/weekly nutrition summary could be surfaced on the meal plan view later.
- **Meal plan templates:** Save a plan as a template and reuse it.