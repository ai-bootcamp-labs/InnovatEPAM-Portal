import { Navigate, useLocation } from 'react-router-dom';
import type { ReactElement } from 'react';
import { useAuth, type Role } from '@/features/auth/AuthProvider';

/** Redirects unauthenticated visitors to /login with a returnUrl (T038). */
export function RequireAuth({ children }: { children: ReactElement }): ReactElement {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />;
  }
  return children;
}

/** Wraps RequireAuth and additionally checks the user's role (T038). */
export function RequireRole({
  role,
  children,
}: {
  role: Role;
  children: ReactElement;
}): ReactElement {
  const { user, isAuthenticated } = useAuth();
  const location = useLocation();
  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />;
  }
  if (user?.role !== role) {
    return <Navigate to="/ideas" replace />;
  }
  return children;
}
