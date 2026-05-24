import client from './client'
import type { ShoppingList } from '../types/shoppingList'

export const getShoppingList = async (userId: string): Promise<ShoppingList | null> => {
  try {
    const { data } = await client.get(`/users/${userId}/shopping-list`)
    return data
  } catch (err: unknown) {
    const e = err as { response?: { status?: number } }
    if (e?.response?.status === 404) return null
    throw err
  }
}

export const generateShoppingList = async (userId: string, fromDate: string, toDate: string): Promise<ShoppingList> => {
  const { data } = await client.post(`/users/${userId}/shopping-list/generate`, { fromDate, toDate })
  return data
}

export const toggleShoppingListItem = async (userId: string, itemId: string, isChecked: boolean): Promise<void> => {
  await client.patch(`/users/${userId}/shopping-list/items/${itemId}`, { isChecked })
}
