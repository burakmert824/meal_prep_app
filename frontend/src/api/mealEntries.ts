import client from './client'
import type { MealEntry, CreateMealEntryRequest } from '../types/mealEntry'

export const getMealEntries = async (userId: string, from: string, to: string): Promise<MealEntry[]> => {
  const { data } = await client.get(`/users/${userId}/meal-entries`, { params: { from, to } })
  return data
}

export const createMealEntry = async (userId: string, req: CreateMealEntryRequest): Promise<MealEntry> => {
  const { data } = await client.post(`/users/${userId}/meal-entries`, req)
  return data
}

export const updateMealEntry = async (userId: string, id: string, portionMultiplier: number): Promise<MealEntry> => {
  const { data } = await client.put(`/users/${userId}/meal-entries/${id}`, { portionMultiplier })
  return data
}

export const deleteMealEntry = async (userId: string, id: string): Promise<void> => {
  await client.delete(`/users/${userId}/meal-entries/${id}`)
}
