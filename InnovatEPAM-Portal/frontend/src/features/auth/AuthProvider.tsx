import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

/**
 * AuthProvider scaffold (T070 fully implements the API integration). Phase 2
 * exposes the shape so the routing layer (T038) can branch on authentication
 * state without coupling to a specific backend client.
 */

export type Role = 'Submitter' | 'Admin';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  role: Role;
}

interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (token: string, user: AuthUser) => void;
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

  useEffect(() => {
    if (token && user) {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ token, user }));
    } else {
      window.localStorage.removeItem(STORAGE_KEY);
    }
  }, [token, user]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: token !== null && user !== null,
      login: (nextToken, nextUser) => {
        setToken(nextToken);
        setUser(nextUser);
      },
      logout: () => {
        setToken(null);
        setUser(null);
      },
    }),
    [token, user],
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
