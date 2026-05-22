import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getFoods, createFood, updateFood, deleteFood } from '../api/foods'
import { useUserStore } from '../store/userStore'
import type { Food, CreateFoodRequest } from '../types/food'

const emptyForm = { name: '', unit: '', caloriesPerUnit: 0 }

const FoodsPage = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { selectedUser, clearUser } = useUserStore()

  const [search, setSearch] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<Food | null>(null)
  const [form, setForm] = useState<CreateFoodRequest>(emptyForm)

  const userId = selectedUser!.id

  const { data: foods = [], isLoading } = useQuery({
    queryKey: ['foods', userId, search],
    queryFn: () => getFoods(userId, search || undefined),
  })

  const { mutate: create, isPending: isCreating } = useMutation({
    mutationFn: (req: CreateFoodRequest) => createFood(userId, req),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['foods', userId] }); closeForm() },
  })

  const { mutate: update, isPending: isUpdating } = useMutation({
    mutationFn: ({ id, req }: { id: string; req: CreateFoodRequest }) => updateFood(userId, id, req),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['foods', userId] }); closeForm() },
  })

  const { mutate: remove } = useMutation({
    mutationFn: (id: string) => deleteFood(userId, id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['foods', userId] }),
  })

  const openCreate = () => { setEditing(null); setForm(emptyForm); setShowForm(true) }
  const openEdit = (food: Food) => { setEditing(food); setForm({ name: food.name, unit: food.unit, caloriesPerUnit: food.caloriesPerUnit }); setShowForm(true) }
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(emptyForm) }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (editing) update({ id: editing.id, req: form })
    else create(form)
  }

  const handleSignOut = () => { clearUser(); navigate('/') }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Meal Prepper</h1>
        <div className="flex items-center gap-3">
          <span className="text-sm text-gray-500">
            <span className="font-medium text-gray-700">{selectedUser?.name}</span>
          </span>
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
                    {food.caloriesPerUnit} kcal / {food.unit}
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
            <input
              type="number"
              min={0}
              step="0.1"
              placeholder="Calories per unit"
              value={form.caloriesPerUnit || ''}
              onChange={(e) => setForm({ ...form, caloriesPerUnit: parseFloat(e.target.value) || 0 })}
              className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />

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