import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getShoppingList, generateShoppingList, toggleShoppingListItem } from '../api/shoppingList'
import { useUserStore } from '../store/userStore'
import type { ShoppingList } from '../types/shoppingList'

// ---------------------------------------------------------------------------
// Date helpers (duplicated locally to keep pages self-contained)
// ---------------------------------------------------------------------------

const toISO = (d: Date) => d.toISOString().split('T')[0]

const getWeekStart = (d: Date) => {
  const day = d.getDay()
  const diff = day === 0 ? -6 : 1 - day
  const monday = new Date(d)
  monday.setDate(d.getDate() + diff)
  return monday
}

// ---------------------------------------------------------------------------
// ShoppingListPage
// ---------------------------------------------------------------------------

export const ShoppingListPage = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { selectedUser, clearUser } = useUserStore()
  const userId = selectedUser!.id

  const today = new Date()
  const thisMonday = getWeekStart(today)

  const [fromDate, setFromDate] = useState(toISO(thisMonday))
  const [toDate, setToDate] = useState(
    toISO(new Date(thisMonday.getTime() + 6 * 86400000))
  )
  const [dateError, setDateError] = useState('')

  // ---------------------------------------------------------------------------
  // Data
  // ---------------------------------------------------------------------------

  const { data: shoppingList, isLoading } = useQuery({
    queryKey: ['shoppingList', userId],
    queryFn: () => getShoppingList(userId),
  })

  const { mutate: generate, isPending: isGenerating } = useMutation({
    mutationFn: () => {
      if (fromDate > toDate) {
        setDateError('Start must be before end')
        return Promise.reject(new Error('Start must be before end'))
      }
      setDateError('')
      return generateShoppingList(userId, fromDate, toDate)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['shoppingList', userId] }),
  })

  const { mutate: toggle } = useMutation({
    mutationFn: ({ id, checked }: { id: string; checked: boolean }) =>
      toggleShoppingListItem(userId, id, checked),
    onMutate: async ({ id, checked }) => {
      await queryClient.cancelQueries({ queryKey: ['shoppingList', userId] })
      const prev = queryClient.getQueryData<ShoppingList | null>(['shoppingList', userId])
      queryClient.setQueryData<ShoppingList | null>(['shoppingList', userId], old =>
        old
          ? { ...old, items: old.items.map(i => (i.id === id ? { ...i, isChecked: checked } : i)) }
          : old
      )
      return { prev }
    },
    onError: (_err, _vars, ctx) => {
      queryClient.setQueryData(['shoppingList', userId], ctx?.prev)
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['shoppingList', userId] }),
  })

  // ---------------------------------------------------------------------------
  // Formatting helpers
  // ---------------------------------------------------------------------------

  const formatListMeta = (list: ShoppingList) => {
    const from = new Date(list.fromDate.split('T')[0] + 'T00:00:00').toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
    })
    const to = new Date(list.toDate.split('T')[0] + 'T00:00:00').toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
    })
    const updatedAt = new Date(list.generatedAt)
    const updatedStr = updatedAt.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    const updatedTime = updatedAt.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })
    return `Generated for ${from}–${to} · Updated ${updatedStr} at ${updatedTime}`
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  const unchecked = shoppingList?.items.filter(i => !i.isChecked) ?? []
  const checked = shoppingList?.items.filter(i => i.isChecked) ?? []
  const totalCount = shoppingList?.items.length ?? 0
  const checkedCount = checked.length

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Meal Prepper</h1>
        <div className="flex items-center gap-4">
          <nav className="flex gap-4 text-sm">
            <button
              onClick={() => navigate('/foods')}
              className="text-gray-400 hover:text-gray-700"
            >
              Foods
            </button>
            <button
              onClick={() => navigate('/recipes')}
              className="text-gray-400 hover:text-gray-700"
            >
              Recipes
            </button>
            <button
              onClick={() => navigate('/meal-plans')}
              className="text-gray-400 hover:text-gray-700"
            >
              Meal Plans
            </button>
            <span className="font-medium text-indigo-600">Shopping List</span>
          </nav>
          <button
            onClick={() => { clearUser(); navigate('/') }}
            className="text-sm text-gray-400 hover:text-gray-600"
          >
            Switch
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-6">Shopping List</h2>

        {/* Date range pickers + generate */}
        <div className="bg-white rounded-xl shadow-sm p-4 mb-6">
          <div className="flex flex-wrap items-end gap-3">
            <div className="flex flex-col gap-1">
              <label className="text-xs text-gray-500">From</label>
              <input
                type="date"
                value={fromDate}
                onChange={e => setFromDate(e.target.value)}
                className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs text-gray-500">To</label>
              <input
                type="date"
                value={toDate}
                onChange={e => setToDate(e.target.value)}
                className="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>
            <button
              onClick={() => generate()}
              disabled={isGenerating}
              className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
            >
              {isGenerating ? 'Generating...' : 'Generate'}
            </button>
          </div>
          {dateError && <p className="text-xs text-red-500 mt-2">{dateError}</p>}
        </div>

        {/* List content */}
        {isLoading ? (
          <p className="text-center text-gray-400 py-10">Loading...</p>
        ) : !shoppingList ? (
          <p className="text-center text-gray-400 py-10">
            No shopping list yet. Set a date range above and click Generate.
          </p>
        ) : (
          <>
            {/* Meta info */}
            <p className="text-xs text-gray-400 mb-3">{formatListMeta(shoppingList)}</p>

            {/* Progress */}
            {totalCount > 0 && (
              <p className="text-xs text-gray-500 mb-4">
                {checkedCount} of {totalCount} items checked
              </p>
            )}

            {totalCount === 0 ? (
              <p className="text-center text-gray-400 py-6">
                No items in this list. Make sure your meal plan has recipes with ingredients.
              </p>
            ) : (
              <ul className="flex flex-col gap-2">
                {/* Unchecked items first */}
                {unchecked.map(item => (
                  <li
                    key={item.id}
                    className="bg-white rounded-xl px-5 py-4 shadow-sm flex items-center gap-3"
                  >
                    <input
                      type="checkbox"
                      checked={false}
                      onChange={() => toggle({ id: item.id, checked: true })}
                      className="w-4 h-4 accent-indigo-600 cursor-pointer"
                    />
                    <span className="text-sm text-gray-800">
                      {item.foodName} — {item.totalQuantity} {item.unit}
                    </span>
                  </li>
                ))}
                {/* Checked items below with strikethrough */}
                {checked.map(item => (
                  <li
                    key={item.id}
                    className="bg-white rounded-xl px-5 py-4 shadow-sm flex items-center gap-3"
                  >
                    <input
                      type="checkbox"
                      checked={true}
                      onChange={() => toggle({ id: item.id, checked: false })}
                      className="w-4 h-4 accent-indigo-600 cursor-pointer"
                    />
                    <span className="text-sm text-gray-400 line-through">
                      {item.foodName} — {item.totalQuantity} {item.unit}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </main>
    </div>
  )
}

export default ShoppingListPage
