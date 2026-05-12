import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';

/**
 * AuthProvider — Phase 3 (T070, T073) wires registration and login into the
 * backend. The bearer token is persisted in localStorage so the API client
 * (which has no React dependency) can attach it on every request.
 */

export type Role = 'Submitter' | 'Admin';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  role: Role;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
}

export interface RegisterPayload {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  register: (payload: RegisterPayload) => Promise<AuthResponse>;
  login: (payload: LoginPayload) => Promise<AuthResponse>;
  logout: () => void;
}

const STORAGE_KEY = 'innovatepam.auth.v1';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readPersisted(): { token: string; user: AuthUser } | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { token: string; user: AuthUser };
    if (!parsed.token || !parsed.user) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }): JSX.Element {
  const persisted = readPersisted();
  const [token, setToken] = useState<string | null>(persisted?.token ?? null);
  const [user, setUser] = useState<AuthUser | null>(persisted?.user ?? null);
  const queryClient = useQueryClient();

  useEffect(() => {
    if (token && user) {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ token, user }));
    } else {
      window.localStorage.removeItem(STORAGE_KEY);
    }
  }, [token, user]);

  const applyAuth = useCallback((response: AuthResponse) => {
    setToken(response.accessToken);
    setUser(response.user);
    return response;
  }, []);

  const register = useCallback(
    async (payload: RegisterPayload) => applyAuth(await apiClient.post<AuthResponse>('/auth/register', payload)),
    [applyAuth],
  );

  const login = useCallback(
    async (payload: LoginPayload) => applyAuth(await apiClient.post<AuthResponse>('/auth/login', payload)),
    [applyAuth],
  );

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
    queryClient.clear();
    void apiClient.post('/auth/logout').catch(() => {
      /* logout is best-effort; server side is stateless in Phase 1 */
    });
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: token !== null && user !== null,
      register,
      login,
      logout,
    }),
    [user, token, register, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used inside <AuthProvider>.');
  }
  return ctx;
}
