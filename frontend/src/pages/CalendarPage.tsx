import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getMealEntries, createMealEntry, updateMealEntry, deleteMealEntry } from '../api/mealEntries'
import { getRecipes } from '../api/recipes'
import { useUserStore } from '../store/userStore'
import type { MealEntry, MealSlot, CreateMealEntryRequest } from '../types/mealEntry'
import { MEAL_SLOTS } from '../types/mealEntry'

// ---------------------------------------------------------------------------
// Date helpers
// ---------------------------------------------------------------------------

const toISO = (d: Date) => d.toISOString().split('T')[0]

// Get Monday of the week containing `d`
const getWeekStart = (d: Date) => {
  const day = d.getDay()
  const diff = day === 0 ? -6 : 1 - day
  const monday = new Date(d)
  monday.setDate(d.getDate() + diff)
  return monday
}

// Get all dates in [start, end] inclusive
const getDatesInRange = (start: Date, end: Date): Date[] => {
  const dates: Date[] = []
  const cur = new Date(start)
  while (cur <= end) {
    dates.push(new Date(cur))
    cur.setDate(cur.getDate() + 1)
  }
  return dates
}

// ---------------------------------------------------------------------------
// EntryChip
// ---------------------------------------------------------------------------

const EntryChip = ({
  entry,
  onEdit,
  onRemove,
}: {
  entry: MealEntry
  onEdit: () => void
  onRemove: () => void
}) => (
  <div className="flex items-center gap-1 bg-indigo-50 rounded-lg px-2 py-1.5 text-xs">
    <button onClick={onEdit} className="text-indigo-800 hover:text-indigo-600 text-left flex-1 py-0.5">
      {entry.recipeName}
      {entry.portionMultiplier !== 1 && (
        <span className="text-indigo-400 ml-1">×{entry.portionMultiplier}</span>
      )}
    </button>
    <button onClick={onRemove} className="text-indigo-300 hover:text-red-400 leading-none ml-0.5 px-0.5 py-0.5">
      ×
    </button>
  </div>
)

// ---------------------------------------------------------------------------
// CalendarPage
// ---------------------------------------------------------------------------

type CalendarView = 'day' | 'week' | 'month'

export const CalendarPage = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { selectedUser, clearUser } = useUserStore()
  const userId = selectedUser!.id

  const [view, setView] = useState<CalendarView>('week')
  const [focusDate, setFocusDate] = useState(() => new Date())

  // Add modal state
  const [addModal, setAddModal] = useState<{ date: string; slot: MealSlot } | null>(null)
  const [addRecipeId, setAddRecipeId] = useState('')
  const [addPortion, setAddPortion] = useState(1)

  // Edit modal state
  const [editingEntry, setEditingEntry] = useState<MealEntry | null>(null)
  const [editPortion, setEditPortion] = useState(1)

  // ---------------------------------------------------------------------------
  // Derived date ranges
  // ---------------------------------------------------------------------------

  const weekStart = getWeekStart(focusDate)
  const weekEnd = new Date(weekStart)
  weekEnd.setDate(weekStart.getDate() + 6)

  const monthStart = new Date(focusDate.getFullYear(), focusDate.getMonth(), 1)
  const monthEnd = new Date(focusDate.getFullYear(), focusDate.getMonth() + 1, 0)

  const queryFrom =
    view === 'day' ? toISO(focusDate) : view === 'week' ? toISO(weekStart) : toISO(monthStart)
  const queryTo =
    view === 'day' ? toISO(focusDate) : view === 'week' ? toISO(weekEnd) : toISO(monthEnd)

  // ---------------------------------------------------------------------------
  // Data fetching
  // ---------------------------------------------------------------------------

  const { data: entries = [], isLoading } = useQuery({
    queryKey: ['mealEntries', userId, queryFrom, queryTo],
    queryFn: () => getMealEntries(userId, queryFrom, queryTo),
  })

  const { data: recipes = [] } = useQuery({
    queryKey: ['recipes', userId],
    queryFn: () => getRecipes(userId),
  })

  // ---------------------------------------------------------------------------
  // Mutations
  // ---------------------------------------------------------------------------

  const { mutate: addEntry, isPending: isAdding } = useMutation({
    mutationFn: (req: CreateMealEntryRequest) => createMealEntry(userId, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mealEntries', userId] })
      closeAddModal()
    },
  })

  const { mutate: removeEntry } = useMutation({
    mutationFn: (id: string) => deleteMealEntry(userId, id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['mealEntries', userId] }),
  })

  const { mutate: saveEntry, isPending: isSaving } = useMutation({
    mutationFn: ({ id, portion }: { id: string; portion: number }) =>
      updateMealEntry(userId, id, portion),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mealEntries', userId] })
      setEditingEntry(null)
    },
  })

  // ---------------------------------------------------------------------------
  // Modal helpers
  // ---------------------------------------------------------------------------

  const openAddModal = (date: string, slot: MealSlot) => {
    setAddModal({ date, slot })
    setAddRecipeId(recipes[0]?.id ?? '')
    setAddPortion(1)
  }
  const closeAddModal = () => {
    setAddModal(null)
    setAddRecipeId('')
    setAddPortion(1)
  }

  const openEditModal = (entry: MealEntry) => {
    setEditingEntry(entry)
    setEditPortion(entry.portionMultiplier)
  }

  const handleEditSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingEntry) return
    saveEntry({ id: editingEntry.id, portion: editPortion })
  }

  const handleAddSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!addModal || !addRecipeId) return
    addEntry({
      recipeId: addRecipeId,
      date: addModal.date,
      mealSlot: addModal.slot,
      portionMultiplier: addPortion,
    })
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  const goNext = () => {
    const next = new Date(focusDate)
    if (view === 'day') next.setDate(next.getDate() + 1)
    else if (view === 'week') next.setDate(next.getDate() + 7)
    else next.setMonth(next.getMonth() + 1)
    setFocusDate(next)
  }

  const goPrev = () => {
    const prev = new Date(focusDate)
    if (view === 'day') prev.setDate(prev.getDate() - 1)
    else if (view === 'week') prev.setDate(prev.getDate() - 7)
    else prev.setMonth(prev.getMonth() - 1)
    setFocusDate(prev)
  }

  const goToday = () => setFocusDate(new Date())

  // ---------------------------------------------------------------------------
  // Header label
  // ---------------------------------------------------------------------------

  const headerLabel =
    view === 'day'
      ? focusDate.toLocaleDateString('en-US', {
          weekday: 'long',
          month: 'long',
          day: 'numeric',
          year: 'numeric',
        })
      : view === 'week'
      ? `${weekStart.toLocaleDateString('en-US', {
          month: 'short',
          day: 'numeric',
        })} – ${weekEnd.toLocaleDateString('en-US', {
          month: 'short',
          day: 'numeric',
          year: 'numeric',
        })}`
      : focusDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })

  // ---------------------------------------------------------------------------
  // Entry lookup helper
  // ---------------------------------------------------------------------------

  const getEntries = (dateStr: string, slot: MealSlot) =>
    entries.filter(e => e.date.split('T')[0] === dateStr && e.mealSlot === slot)

  // ---------------------------------------------------------------------------
  // View renderers
  // ---------------------------------------------------------------------------

  const renderDayView = () => {
    const dateStr = toISO(focusDate)
    return (
      <div className="flex flex-col gap-3">
        {MEAL_SLOTS.map(slot => {
          const cellEntries = getEntries(dateStr, slot)
          return (
            <div key={slot} className="bg-white rounded-xl shadow-sm p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-semibold text-gray-700">{slot}</span>
                <button
                  onClick={() => openAddModal(dateStr, slot)}
                  className="text-xs text-indigo-500 hover:text-indigo-700"
                >
                  + Add
                </button>
              </div>
              {cellEntries.length === 0 ? (
                <p className="text-xs text-gray-300">Nothing planned</p>
              ) : (
                <div className="flex flex-wrap gap-2">
                  {cellEntries.map(entry => (
                    <EntryChip
                      key={entry.id}
                      entry={entry}
                      onEdit={() => openEditModal(entry)}
                      onRemove={() => removeEntry(entry.id)}
                    />
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </div>
    )
  }

  const renderWeekView = () => {
    const weekDates = getDatesInRange(weekStart, weekEnd)
    const todayStr = toISO(new Date())
    return (
      <div className="overflow-x-auto rounded-xl shadow-sm bg-white">
        <table className="border-collapse min-w-full text-sm">
          <thead>
            <tr>
              <th className="border border-gray-100 bg-gray-50 px-4 py-3 text-xs font-medium text-gray-600 w-24 text-left">
                Meal
              </th>
              {weekDates.map(date => (
                <th
                  key={toISO(date)}
                  className="border border-gray-100 px-3 py-3 min-w-36 text-center"
                >
                  <div className="font-semibold text-gray-700">
                    {date.toLocaleDateString('en-US', { weekday: 'short' })}
                  </div>
                  <div
                    className={`text-xs ${
                      toISO(date) === todayStr
                        ? 'text-indigo-600 font-bold'
                        : 'text-gray-400'
                    }`}
                  >
                    {date.toLocaleDateString('en-US', { day: 'numeric', month: 'short' })}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {MEAL_SLOTS.map(slot => (
              <tr key={slot}>
                <td className="border border-gray-100 bg-gray-50 px-4 py-3 text-xs font-medium text-gray-600">
                  {slot}
                </td>
                {weekDates.map(date => {
                  const dateStr = toISO(date)
                  const cellEntries = getEntries(dateStr, slot)
                  return (
                    <td
                      key={dateStr}
                      className="border border-gray-100 px-2 py-2 align-top min-w-36"
                    >
                      <div className="flex flex-col gap-1">
                        {cellEntries.map(e => (
                          <EntryChip
                            key={e.id}
                            entry={e}
                            onEdit={() => openEditModal(e)}
                            onRemove={() => removeEntry(e.id)}
                          />
                        ))}
                        <button
                          onClick={() => openAddModal(dateStr, slot)}
                          className="text-xs text-gray-300 hover:text-indigo-500 text-left"
                        >
                          + Add
                        </button>
                      </div>
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    )
  }

  const renderMonthView = () => {
    const gridStart = getWeekStart(monthStart)
    const gridEnd = new Date(getWeekStart(monthEnd))
    gridEnd.setDate(gridEnd.getDate() + 6)
    const gridDates = getDatesInRange(gridStart, gridEnd)

    const weeks: Date[][] = []
    for (let i = 0; i < gridDates.length; i += 7) {
      weeks.push(gridDates.slice(i, i + 7))
    }

    const todayStr = toISO(new Date())

    return (
      <div className="bg-white rounded-xl shadow-sm overflow-hidden">
        {/* Day-of-week header */}
        <div className="grid grid-cols-7 border-b border-gray-100">
          {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(d => (
            <div key={d} className="py-2 text-center text-xs font-medium text-gray-500">
              {d}
            </div>
          ))}
        </div>
        {/* Weeks */}
        {weeks.map((week, wi) => (
          <div
            key={wi}
            className="grid grid-cols-7 divide-x divide-gray-100 border-b border-gray-100"
          >
            {week.map(date => {
              const dateStr = toISO(date)
              const isCurrentMonth = date.getMonth() === focusDate.getMonth()
              const isToday = dateStr === todayStr
              const dayEntries = MEAL_SLOTS.flatMap(slot => getEntries(dateStr, slot))
              return (
                <div
                  key={dateStr}
                  className={`min-h-24 p-1.5 ${isCurrentMonth ? '' : 'bg-gray-50'}`}
                >
                  <div className="flex items-center justify-between mb-1">
                    <span
                      className={`text-xs font-medium w-6 h-6 flex items-center justify-center rounded-full
                        ${
                          isToday
                            ? 'bg-indigo-600 text-white'
                            : isCurrentMonth
                            ? 'text-gray-700'
                            : 'text-gray-300'
                        }`}
                    >
                      {date.getDate()}
                    </span>
                  </div>
                  <div className="flex flex-col gap-0.5">
                    {dayEntries.slice(0, 3).map(e => (
                      <div key={e.id} className="flex items-center gap-0.5 bg-indigo-50 rounded px-1 py-0.5 text-xs">
                        <button onClick={() => openEditModal(e)} className="text-indigo-700 truncate flex-1 text-left">
                          {e.recipeName}
                          {e.portionMultiplier !== 1 && <span className="text-indigo-400 ml-1">×{e.portionMultiplier}</span>}
                        </button>
                        <button onClick={() => removeEntry(e.id)} className="text-indigo-300 hover:text-red-400 leading-none shrink-0">×</button>
                      </div>
                    ))}
                    {dayEntries.length > 3 && (
                      <div className="text-xs text-gray-400">+{dayEntries.length - 3} more</div>
                    )}
                    {isCurrentMonth && (
                      <button
                        onClick={() => {
                          setFocusDate(date)
                          setView('day')
                        }}
                        className="text-xs text-gray-300 hover:text-indigo-500 text-left mt-0.5"
                      >
                        + Add
                      </button>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        ))}
      </div>
    )
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Meal Prepper</h1>
        <div className="flex items-center gap-4">
          <nav className="flex gap-4 text-sm">
            <button onClick={() => navigate('/foods')} className="text-gray-400 hover:text-gray-700">
              Foods
            </button>
            <button onClick={() => navigate('/recipes')} className="text-gray-400 hover:text-gray-700">
              Recipes
            </button>
            <span className="font-medium text-indigo-600">Meal Plans</span>
            <button
              onClick={() => navigate('/shopping-list')}
              className="text-gray-400 hover:text-gray-700"
            >
              Shopping List
            </button>
          </nav>
          <button
            onClick={() => { clearUser(); navigate('/') }}
            className="text-sm text-gray-400 hover:text-gray-600"
          >
            Switch
          </button>
        </div>
      </header>

      <main className="max-w-full px-4 py-4">
        {/* Calendar toolbar */}
        <div className="flex items-center justify-between mb-4 flex-wrap gap-3">
          {/* Left: prev/today/next + label */}
          <div className="flex items-center gap-2">
            <button
              onClick={goPrev}
              className="px-2 py-1 text-lg text-gray-500 hover:text-gray-800 leading-none"
            >
              ‹
            </button>
            <button
              onClick={goToday}
              className="text-xs px-2 py-1 border border-gray-200 rounded text-gray-600 hover:bg-gray-50"
            >
              Today
            </button>
            <button
              onClick={goNext}
              className="px-2 py-1 text-lg text-gray-500 hover:text-gray-800 leading-none"
            >
              ›
            </button>
            <span className="text-base font-semibold text-gray-800 ml-2">{headerLabel}</span>
          </div>
          {/* Right: Day/Week/Month toggle + Shopping List button */}
          <div className="flex items-center gap-3">
            <div className="flex rounded-lg border border-gray-200 overflow-hidden text-sm">
              {(['day', 'week', 'month'] as const).map(v => (
                <button
                  key={v}
                  onClick={() => setView(v)}
                  className={
                    view === v
                      ? 'px-3 py-1.5 bg-indigo-600 text-white'
                      : 'px-3 py-1.5 text-gray-500 hover:bg-gray-50'
                  }
                >
                  {v.charAt(0).toUpperCase() + v.slice(1)}
                </button>
              ))}
            </div>
            <button
              onClick={() => navigate('/shopping-list')}
              className="px-3 py-1.5 text-sm border border-gray-200 rounded-lg text-gray-600 hover:bg-gray-50"
            >
              Shopping List
            </button>
          </div>
        </div>

        {/* Calendar content */}
        {isLoading ? (
          <p className="text-center text-gray-400 py-10">Loading...</p>
        ) : view === 'day' ? (
          renderDayView()
        ) : view === 'week' ? (
          renderWeekView()
        ) : (
          renderMonthView()
        )}
      </main>

      {/* Edit entry modal */}
      {editingEntry && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <form
            onSubmit={handleEditSubmit}
            className="bg-white rounded-2xl p-6 w-full max-w-sm shadow-xl flex flex-col gap-4"
          >
            <h2 className="text-lg font-semibold text-gray-800">Edit Entry</h2>
            <p className="text-sm text-gray-600">{editingEntry.recipeName}</p>
            <div className="flex flex-col gap-1">
              <label className="text-xs text-gray-500">Portion multiplier</label>
              <input
                autoFocus
                type="number"
                min={0.5}
                step={0.5}
                value={editPortion}
                onChange={e => setEditPortion(parseFloat(e.target.value) || 1)}
                className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>
            <div className="flex gap-2 justify-end">
              <button
                type="button"
                onClick={() => setEditingEntry(null)}
                className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSaving}
                className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
              >
                {isSaving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Add entry modal */}
      {addModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <form
            onSubmit={handleAddSubmit}
            className="bg-white rounded-2xl p-6 w-full max-w-sm shadow-xl flex flex-col gap-4"
          >
            <h2 className="text-lg font-semibold text-gray-800">
              Add to {addModal.slot} —{' '}
              {new Date(addModal.date + 'T00:00:00').toLocaleDateString('en-US', {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
              })}
            </h2>
            {recipes.length === 0 ? (
              <p className="text-sm text-gray-400">
                No recipes yet. Add some from the Recipes page.
              </p>
            ) : (
              <>
                <select
                  value={addRecipeId}
                  onChange={e => setAddRecipeId(e.target.value)}
                  className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                >
                  <option value="">Select a recipe</option>
                  {recipes.map(r => (
                    <option key={r.id} value={r.id}>
                      {r.name}
                    </option>
                  ))}
                </select>
                <div className="flex flex-col gap-1">
                  <label className="text-xs text-gray-500">Portion multiplier</label>
                  <input
                    type="number"
                    min={0.5}
                    step={0.5}
                    value={addPortion}
                    onChange={e => setAddPortion(parseFloat(e.target.value) || 1)}
                    className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  />
                </div>
              </>
            )}
            <div className="flex gap-2 justify-end">
              <button
                type="button"
                onClick={closeAddModal}
                className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700"
              >
                Cancel
              </button>
              {recipes.length > 0 && (
                <button
                  type="submit"
                  disabled={isAdding || !addRecipeId}
                  className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                >
                  {isAdding ? 'Adding...' : 'Add'}
                </button>
              )}
            </div>
          </form>
        </div>
      )}
    </div>
  )
}

export default CalendarPage
