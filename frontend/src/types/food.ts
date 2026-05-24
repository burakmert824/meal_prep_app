export interface Food {
  id: string
  userId: string
  name: string
  unit: string
  caloriesPerUnit: number
  proteinPerUnit: number
}

export interface CreateFoodRequest {
  name: string
  unit: string
  caloriesPerUnit: number
  proteinPerUnit: number
}

export interface UpdateFoodRequest {
  name: string
  unit: string
  caloriesPerUnit: number
  proteinPerUnit: number
}
