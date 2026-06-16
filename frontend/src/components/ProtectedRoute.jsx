import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// role: 'client' | 'admin' | null (samo ulogovan)
export function ProtectedRoute({ children, role }) {
  const { user, loadingAuth, isAdmin } = useAuth();

  if (loadingAuth) return null;

  if (!user) return <Navigate to="/login" replace />;

  if (role === 'admin' && !isAdmin()) return <Navigate to="/" replace />;
  if (role === 'client' && isAdmin()) return <Navigate to="/admin/reservations" replace />;

  return children;
}
