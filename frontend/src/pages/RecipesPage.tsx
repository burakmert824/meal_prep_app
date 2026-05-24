import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getRecipes, createRecipe, updateRecipe, deleteRecipe } from '../api/recipes'
import { getFoods } from '../api/foods'
import { useUserStore } from '../store/userStore'
import type { Recipe, CreateRecipeRequest, RecipeIngredientInput } from '../types/recipe'

const emptyForm = (): CreateRecipeRequest => ({
  name: '',
  defaultPortionSize: 1,
  ingredients: [],
})

const RecipesPage = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { selectedUser, clearUser } = useUserStore()
  const userId = selectedUser!.id

  const [search, setSearch] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<Recipe | null>(null)
  const [form, setForm] = useState<CreateRecipeRequest>(emptyForm())

  const { data: recipes = [], isLoading } = useQuery({
    queryKey: ['recipes', userId, search],
    queryFn: () => getRecipes(userId, search || undefined),
  })

  const { data: foods = [] } = useQuery({
    queryKey: ['foods', userId],
    queryFn: () => getFoods(userId),
  })

  const { mutate: create, isPending: isCreating } = useMutation({
    mutationFn: (req: CreateRecipeRequest) => createRecipe(userId, req),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['recipes', userId] }); closeForm() },
  })

  const { mutate: update, isPending: isUpdating } = useMutation({
    mutationFn: ({ id, req }: { id: string; req: CreateRecipeRequest }) => updateRecipe(userId, id, req),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['recipes', userId] }); closeForm() },
  })

  const { mutate: remove } = useMutation({
    mutationFn: (id: string) => deleteRecipe(userId, id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['recipes', userId] }),
  })

  const openCreate = () => { setEditing(null); setForm(emptyForm()); setShowForm(true) }
  const openEdit = (r: Recipe) => {
    setEditing(r)
    setForm({
      name: r.name,
      defaultPortionSize: r.defaultPortionSize,
      ingredients: r.ingredients.map(i => ({ foodId: i.foodId, quantity: i.quantity })),
    })
    setShowForm(true)
  }
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(emptyForm()) }

  const addIngredient = () =>
    setForm(f => ({ ...f, ingredients: [...f.ingredients, { foodId: '', quantity: 1 }] }))

  const updateIngredient = (idx: number, patch: Partial<RecipeIngredientInput>) =>
    setForm(f => ({
      ...f,
      ingredients: f.ingredients.map((ing, i) => i === idx ? { ...ing, ...patch } : ing),
    }))

  const removeIngredient = (idx: number) =>
    setForm(f => ({ ...f, ingredients: f.ingredients.filter((_, i) => i !== idx) }))

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const valid = form.ingredients.every(i => i.foodId && i.quantity > 0)
    if (!valid) return
    if (editing) update({ id: editing.id, req: form })
    else create(form)
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Meal Prepper</h1>
        <div className="flex items-center gap-4">
          <nav className="flex gap-4 text-sm">
            <button onClick={() => navigate('/foods')} className="text-gray-400 hover:text-gray-700">Foods</button>
            <span className="font-medium text-indigo-600">Recipes</span>
            <button onClick={() => navigate('/meal-plans')} className="text-gray-400 hover:text-gray-700">Meal Plans</button>
            <button onClick={() => navigate('/shopping-list')} className="text-gray-400 hover:text-gray-700">Shopping List</button>
          </nav>
          <button onClick={() => { clearUser(); navigate('/') }} className="text-sm text-gray-400 hover:text-gray-600">
            Switch
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-semibold text-gray-800">My Recipes</h2>
          <button onClick={openCreate} className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700">
            + Add Recipe
          </button>
        </div>

        <input
          type="text"
          placeholder="Search recipes..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="w-full border border-gray-200 rounded-lg px-4 py-2 text-sm mb-4 focus:outline-none focus:ring-2 focus:ring-indigo-300"
        />

        {isLoading ? (
          <p className="text-center text-gray-400 py-10">Loading...</p>
        ) : recipes.length === 0 ? (
          <p className="text-center text-gray-400 py-10">
            {search ? 'No recipes match your search.' : 'No recipes yet. Add your first one!'}
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {recipes.map(recipe => (
              <li key={recipe.id} className="bg-white rounded-xl px-5 py-4 shadow-sm">
                <div className="flex items-center justify-between mb-2">
                  <div>
                    <p className="font-medium text-gray-800">{recipe.name}</p>
                    <p className="text-sm text-gray-400">
                      {recipe.ingredients.length} ingredient{recipe.ingredients.length !== 1 ? 's' : ''} · {recipe.defaultPortionSize} portion
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button onClick={() => openEdit(recipe)} className="text-sm text-indigo-500 hover:text-indigo-700">Edit</button>
                    <button onClick={() => remove(recipe.id)} className="text-sm text-red-400 hover:text-red-600">Delete</button>
                  </div>
                </div>
                {recipe.ingredients.length > 0 && (
                  <ul className="text-xs text-gray-400 flex flex-wrap gap-2">
                    {recipe.ingredients.map(ing => (
                      <li key={ing.id} className="bg-gray-50 rounded px-2 py-1">
                        {ing.foodName} — {ing.quantity} {ing.unit}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        )}
      </main>

      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 overflow-y-auto">
          <form onSubmit={handleSubmit} className="bg-white rounded-2xl p-6 w-full max-w-md shadow-xl flex flex-col gap-4 my-4">
            <h2 className="text-lg font-semibold text-gray-800">{editing ? 'Edit Recipe' : 'New Recipe'}</h2>

            <input
              autoFocus
              type="text"
              placeholder="Recipe name"
              value={form.name}
              onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />

            <div className="flex items-center gap-3">
              <label className="text-sm text-gray-500 whitespace-nowrap">Default portions</label>
              <input
                type="number"
                min={0.5}
                step={0.5}
                value={form.defaultPortionSize}
                onChange={e => setForm(f => ({ ...f, defaultPortionSize: parseFloat(e.target.value) || 1 }))}
                className="w-24 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
              />
            </div>

            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-gray-700">Ingredients</span>
                <button type="button" onClick={addIngredient} className="text-xs text-indigo-500 hover:text-indigo-700">+ Add</button>
              </div>
              {form.ingredients.length === 0 && (
                <p className="text-xs text-gray-400">No ingredients yet.</p>
              )}
              {form.ingredients.map((ing, idx) => (
                <div key={idx} className="flex gap-2 items-center">
                  <select
                    value={ing.foodId}
                    onChange={e => updateIngredient(idx, { foodId: e.target.value })}
                    className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  >
                    <option value="">Select food</option>
                    {foods.map(f => (
                      <option key={f.id} value={f.id}>{f.name} ({f.unit})</option>
                    ))}
                  </select>
                  <input
                    type="number"
                    min={0}
                    step={0.1}
                    placeholder="Qty"
                    value={ing.quantity || ''}
                    onChange={e => updateIngredient(idx, { quantity: parseFloat(e.target.value) || 0 })}
                    className="w-20 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  />
                  <button type="button" onClick={() => removeIngredient(idx)} className="text-red-400 hover:text-red-600 text-lg leading-none">×</button>
                </div>
              ))}
            </div>

            <div className="flex gap-2 justify-end pt-2">
              <button type="button" onClick={closeForm} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">Cancel</button>
              <button
                type="submit"
                disabled={isCreating || isUpdating || !form.name.trim()}
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

export default RecipesPage
