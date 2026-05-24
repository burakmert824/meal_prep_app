export interface RecipeIngredient {
  id: string
  foodId: string
  foodName: string
  unit: string
  quantity: number
}

export interface Recipe {
  id: string
  userId: string
  name: string
  defaultPortionSize: number
  ingredients: RecipeIngredient[]
}

export interface RecipeIngredientInput {
  foodId: string
  quantity: number
}

export interface CreateRecipeRequest {
  name: string
  defaultPortionSize: number
  ingredients: RecipeIngredientInput[]
}

export interface UpdateRecipeRequest {
  name: string
  defaultPortionSize: number
  ingredients: RecipeIngredientInput[]
}