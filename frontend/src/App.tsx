import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import ProfileSelectorPage from './pages/ProfileSelectorPage'
import FoodsPage from './pages/FoodsPage'
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
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  </BrowserRouter>
)

export default App
