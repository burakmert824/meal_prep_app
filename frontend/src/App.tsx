import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import ProfileSelectorPage from './pages/ProfileSelectorPage'
import FoodsPage from './pages/FoodsPage'
import RecipesPage from './pages/RecipesPage'
import CalendarPage from './pages/CalendarPage'
import ShoppingListPage from './pages/ShoppingListPage'
import { useUserStore } from './store/userStore'

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const selectedUser = useUserStore((s) => s.selectedUser)
  return selectedUser ? <>{children}</> : <Navigate to="/" replace />
}

const App = () => (
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<ProfileSelectorPage />} />
      <Route path="/foods" element={<ProtectedRoute><FoodsPage /></ProtectedRoute>} />
      <Route path="/recipes" element={<ProtectedRoute><RecipesPage /></ProtectedRoute>} />
      <Route path="/meal-plans" element={<ProtectedRoute><CalendarPage /></ProtectedRoute>} />
      <Route path="/shopping-list" element={<ProtectedRoute><ShoppingListPage /></ProtectedRoute>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  </BrowserRouter>
)

export default App
