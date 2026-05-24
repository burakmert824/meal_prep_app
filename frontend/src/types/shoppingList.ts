export interface ShoppingListItem {
  id: string
  foodId: string
  foodName: string
  unit: string
  totalQuantity: number
  isChecked: boolean
}

export interface ShoppingList {
  id: string
  userId: string
  fromDate: string
  toDate: string
  generatedAt: string
  items: ShoppingListItem[]
}
