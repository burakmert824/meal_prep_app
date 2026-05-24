import client from './client'
import type { Recipe, CreateRecipeRequest, UpdateRecipeRequest } from '../types/recipe'

export const getRecipes = async (userId: string, search?: string): Promise<Recipe[]> => {
  const { data } = await client.get<Recipe[]>(`/users/${userId}/recipes`, {
    params: search ? { search } : undefined,
  })
  return data
}

export const createRecipe = async (userId: string, req: CreateRecipeRequest): Promise<Recipe> => {
  const { data } = await client.post<Recipe>(`/users/${userId}/recipes`, req)
  return data
}

export const updateRecipe = async (userId: string, id: string, req: UpdateRecipeRequest): Promise<Recipe> => {
  const { data } = await client.put<Recipe>(`/users/${userId}/recipes/${id}`, req)
  return data
}

export const deleteRecipe = async (userId: string, id: string): Promise<void> => {
  await client.delete(`/users/${userId}/recipes/${id}`)
}