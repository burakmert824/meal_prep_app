import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getUsers, createUser } from '../api/users'
import { useUserStore } from '../store/userStore'

const ProfileSelectorPage = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const setUser = useUserStore((s) => s.setUser)

  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState('')

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
  })

  const { mutate: create, isPending } = useMutation({
    mutationFn: createUser,
    onSuccess: (user) => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      setUser(user)
      navigate('/foods')
    },
  })

  const handleSelect = (user: (typeof users)[0]) => {
    setUser(user)
    navigate('/foods')
  }

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault()
    if (name.trim()) create({ name: name.trim() })
  }

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-6">
      <h1 className="text-3xl font-bold text-gray-800 mb-2">Meal Prepper</h1>
      <p className="text-gray-500 mb-10">Who's planning meals today?</p>

      {isLoading ? (
        <p className="text-gray-400">Loading profiles...</p>
      ) : (
        <div className="flex flex-wrap gap-4 justify-center mb-8">
          {users.map((user) => (
            <button
              key={user.id}
              onClick={() => handleSelect(user)}
              className="flex flex-col items-center gap-2 p-5 bg-white rounded-2xl shadow hover:shadow-md hover:scale-105 transition-all w-32"
            >
              <div className="w-14 h-14 rounded-full bg-indigo-100 flex items-center justify-center text-2xl font-bold text-indigo-600">
                {user.name[0].toUpperCase()}
              </div>
              <span className="text-sm font-medium text-gray-700 truncate w-full text-center">
                {user.name}
              </span>
            </button>
          ))}

          <button
            onClick={() => setShowForm(true)}
            className="flex flex-col items-center gap-2 p-5 bg-white rounded-2xl shadow hover:shadow-md hover:scale-105 transition-all w-32 border-2 border-dashed border-gray-200"
          >
            <div className="w-14 h-14 rounded-full bg-gray-100 flex items-center justify-center text-2xl text-gray-400">
              +
            </div>
            <span className="text-sm font-medium text-gray-400">Add Profile</span>
          </button>
        </div>
      )}

      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
          <form
            onSubmit={handleCreate}
            className="bg-white rounded-2xl p-6 w-full max-w-sm shadow-xl flex flex-col gap-4"
          >
            <h2 className="text-lg font-semibold text-gray-800">New Profile</h2>
            <input
              autoFocus
              type="text"
              placeholder="Your name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="border border-gray-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            <div className="flex gap-2 justify-end">
              <button
                type="button"
                onClick={() => { setShowForm(false); setName('') }}
                className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isPending || !name.trim()}
                className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50"
              >
                {isPending ? 'Creating...' : 'Create'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  )
}

export default ProfileSelectorPage