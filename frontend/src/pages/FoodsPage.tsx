import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getFoods, createFood, updateFood, deleteFood } from '../api/foods'
import { useUserStore } from '../store/userStore'
import type { Food, CreateFoodRequest } from '../types/food'

const emptyForm = { name: '', unit: '', caloriesPerUnit: 0, proteinPerUnit: 0 }
const emptyRef = { amount: 100, calories: 0, protein: 0 }

const FoodsPage = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { selectedUser, clearUser } = useUserStore()

  const [search, setSearch] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<Food | null>(null)
  const [form, setForm] = useState<CreateFoodRequest>(emptyForm)
  const [ref, setRef] = useState(emptyRef)
  const [duplicateNotice, setDuplicateNotice] = useState('')

  const userId = selectedUser!.id

  const { data: foods = [], isLoading } = useQuery({
    queryKey: ['foods', userId, search],
    queryFn: () => getFoods(userId, search || undefined),
  })

  const { data: allFoods = [] } = useQuery({
    queryKey: ['foods', userId],
    queryFn: () => getFoods(userId),
  })

  const { mutate: create, isPending: isCreating } = useMutation({
    mutationFn: (req: CreateFoodRequest) => createFood(userId, req),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['foods', userId] }); closeForm() },
    onError: () => toast.error('Failed to save food. Please try again.'),
  })

  const { mutate: update, isPending: isUpdating } = useMutation({
    mutationFn: ({ id, req }: { id: string; req: CreateFoodRequest }) => updateFood(userId, id, req),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['foods', userId] }); closeForm() },
    onError: () => toast.error('Failed to update food. Please try again.'),
  })

  const { mutate: remove } = useMutation({
    mutationFn: (id: string) => deleteFood(userId, id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['foods', userId] }); toast.success('Food deleted.') },
    onError: () => toast.error('Failed to delete food. Please try again.'),
  })

  const openCreate = () => { setEditing(null); setForm(emptyForm); setRef(emptyRef); setDuplicateNotice(''); setShowForm(true) }
  const openEdit = (food: Food) => {
    setEditing(food)
    setForm({ name: food.name, unit: food.unit, caloriesPerUnit: food.caloriesPerUnit, proteinPerUnit: food.proteinPerUnit })
    setRef({ amount: 100, calories: food.caloriesPerUnit * 100, protein: food.proteinPerUnit * 100 })
    setShowForm(true)
  }
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(emptyForm); setRef(emptyRef); setDuplicateNotice('') }

  const updateRef = (patch: Partial<typeof emptyRef>) => {
    const next = { ...ref, ...patch }
    setRef(next)
    if (next.amount > 0) {
      setForm(f => ({
        ...f,
        caloriesPerUnit: next.calories / next.amount,
        proteinPerUnit: next.protein / next.amount,
      }))
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const nameLower = form.name.trim().toLowerCase()
    const duplicate = allFoods.find(
      f => f.name.toLowerCase() === nameLower && f.id !== editing?.id
    )
    if (duplicate) {
      setDuplicateNotice(`"${duplicate.name}" already exists. Opened it for editing.`)
      openEdit(duplicate)
      return
    }
    setDuplicateNotice('')
    if (editing) update({ id: editing.id, req: form })
    else create(form)
  }

  const handleSignOut = () => { clearUser(); navigate('/') }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Meal Prepper</h1>
        <div className="flex items-center gap-4">
          <nav className="flex gap-4 text-sm">
            <span className="font-medium text-indigo-600">Foods</span>
            <button onClick={() => navigate('/recipes')} className="text-gray-400 hover:text-gray-700">Recipes</button>
            <button onClick={() => navigate('/meal-plans')} className="text-gray-400 hover:text-gray-700">Meal Plans</button>
            <button onClick={() => navigate('/shopping-list')} className="text-gray-400 hover:text-gray-700">Shopping List</button>
          </nav>
          <button onClick={handleSignOut} className="text-sm text-gray-400 hover:text-gray-600">
            Switch
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-gray-800">My Foods</h2>
          <button
            onClick={openCreate}
            className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700"
          >
            + Add Food
          </button>
        </div>

        <input
          type="text"
          placeholder="Search foods..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full border border-gray-200 rounded-lg px-4 py-2 text-sm mb-4 focus:outline-none focus:ring-2 focus:ring-indigo-300"
        />

        {isLoading ? (
          <p className="text-center text-gray-400 py-10">Loading...</p>
        ) : foods.length === 0 ? (
          <p className="text-center text-gray-400 py-10">
            {search ? 'No foods match your search.' : 'No foods yet. Add your first one!'}
          </p>
        ) : (
          <ul className="flex flex-col gap-2">
            {foods.map((food) => (
              <li
                key={food.id}
                className="bg-white rounded-xl px-5 py-4 shadow-sm flex items-center justify-between"
              >
                <div>
                  <p className="font-medium text-gray-800">{food.name}</p>
                  <p className="text-sm text-gray-400">
                    {(food.caloriesPerUnit * 100).toFixed(1)} kcal · {(food.proteinPerUnit * 100).toFixed(1)}g protein per 100 {food.unit}
                  </p>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => openEdit(food)}
                    className="text-sm text-indigo-500 hover:text-indigo-700"
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => remove(food.id)}
                    className="text-sm text-red-400 hover:text-red-600"
                  >
                    Delete
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </main>

      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
          <form
            onSubmit={handleSubmit}
            className="bg-white rounded-2xl p-6 w-full max-w-sm shadow-xl flex flex-col gap-4"
          >
            <h2 className="text-lg font-semibold text-gray-800">
              {editing ? 'Edit Food' : 'New Food'}
            </h2>

            <input
              autoFocus
              type="text"
              placeholder="Name (e.g. Chicken breast)"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            <input
              type="text"
              placeholder="Unit (e.g. g, ml, piece)"
              value={form.unit}
              onChange={(e) => setForm({ ...form, unit: e.target.value })}
              className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            <div className="flex flex-col gap-1">
              <label className="text-xs text-gray-400">Nutrition info is per how many units?</label>
              <input
                type="number"
                min={1}
                step={1}
                placeholder="Reference amount (e.g. 100)"
                value={ref.amount || ''}
                onChange={(e) => updateRef({ amount: parseFloat(e.target.value) || 1 })}
                className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>
            <div className="flex gap-2">
              <div className="flex flex-col gap-1 flex-1 min-w-0">
                <label className="text-xs text-gray-400">Calories (kcal)</label>
                <input
                  type="number"
                  min={0}
                  step={0.1}
                  placeholder="kcal"
                  value={ref.calories}
                  onChange={(e) => updateRef({ calories: parseFloat(e.target.value) || 0 })}
                  className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                />
              </div>
              <div className="flex flex-col gap-1 flex-1 min-w-0">
                <label className="text-xs text-gray-400">Protein (g)</label>
                <input
                  type="number"
                  min={0}
                  step={0.1}
                  placeholder="g protein"
                  value={ref.protein}
                  onChange={(e) => updateRef({ protein: parseFloat(e.target.value) || 0 })}
                  className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                />
              </div>
            </div>
            {ref.amount !== 1 && (
              <p className="text-xs text-gray-400">
                = {(form.caloriesPerUnit).toFixed(2)} kcal · {(form.proteinPerUnit).toFixed(2)}g protein per {form.unit || 'unit'}
              </p>
            )}

            {duplicateNotice && (
              <div className="rounded-lg bg-amber-50 border border-amber-200 px-3 py-2 text-xs text-amber-700">
                {duplicateNotice}
              </div>
            )}

            <div className="flex gap-2 justify-end">
              <button
                type="button"
                onClick={closeForm}
                className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isCreating || isUpdating || !form.name.trim() || !form.unit.trim()}
                className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
              >
                {isCreating || isUpdating ? 'Saving...' : editing ? 'Save' : 'Add'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  )
}

export default FoodsPage