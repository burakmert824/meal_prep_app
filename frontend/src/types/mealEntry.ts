export type MealSlot = 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack'
export const MEAL_SLOTS: MealSlot[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack']

export interface MealEntry {
  id: string
  userId: string
  recipeId: string
  recipeName: string
  date: string        // "YYYY-MM-DDTHH:mm:ss" from backend — use .split('T')[0]
  mealSlot: MealSlot
  portionMultiplier: number
}

export interface CreateMealEntryRequest {
  recipeId: string
  date: string        // "YYYY-MM-DD"
  mealSlot: MealSlot  // string enum: "Breakfast" etc.
  portionMultiplier: number
}
