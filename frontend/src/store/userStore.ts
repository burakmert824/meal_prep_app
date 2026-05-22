import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { User } from '../types/user'

interface UserStore {
  selectedUser: User | null
  setUser: (user: User) => void
  clearUser: () => void
}

export const useUserStore = create<UserStore>()(
  persist(
    (set) => ({
      selectedUser: null,
      setUser: (user) => set({ selectedUser: user }),
      clearUser: () => set({ selectedUser: null }),
    }),
    { name: 'meal-prepper-user' }
  )
)