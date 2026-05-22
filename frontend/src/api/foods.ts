import client from './client'
import type { Food, CreateFoodRequest, UpdateFoodRequest } from '../types/food'

export const getFoods = async (userId: string, search?: string): Promise<Food[]> => {
  const { data } = await client.get<Food[]>(`/users/${userId}/foods`, {
    params: search ? { search } : undefined,
  })
  return data
}

export const createFood = async (userId: string, req: CreateFoodRequest): Promise<Food> => {
  const { data } = await client.post<Food>(`/users/${userId}/foods`, req)
  return data
}

export const updateFood = async (userId: string, id: string, req: UpdateFoodRequest): Promise<Food> => {
  const { data } = await client.put<Food>(`/users/${userId}/foods/${id}`, req)
  return data
}

export const deleteFood = async (userId: string, id: string): Promise<void> => {
  await client.delete(`/users/${userId}/foods/${id}`)
}