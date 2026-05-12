import { useState } from 'react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider';
import { NotificationsPanel } from '@/features/notifications/NotificationsPanel';
import { useUnreadCount } from '@/features/notifications/useUnreadCount';

/** Application chrome: header with brand, role badge, notification bell, logout (T043, T106). */
export function AppShell({ children }: { children: ReactNode }): JSX.Element {
  const { user, isAuthenticated, logout } = useAuth();
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const unreadCountQuery = useUnreadCount(isAuthenticated);
  const unreadCount = unreadCountQuery.data ?? 0;
  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-card">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4">
          <Link to="/ideas" className="font-semibold tracking-tight">
            InnovatEPAM Portal
          </Link>
          <nav className="flex items-center gap-3 text-sm">
            {isAuthenticated && user ? (
              <>
                <span className="rounded-full border border-border bg-secondary px-2 py-0.5 text-xs font-medium text-secondary-foreground">
                  {user.role}
                </span>
                <span className="hidden text-muted-foreground sm:inline">{user.displayName}</span>
                <button
                  type="button"
                  onClick={() => setNotificationsOpen(true)}
                  aria-label={
                    unreadCount > 0
                      ? `Notifications (${unreadCount} unread)`
                      : 'Notifications'
                  }
                  className="relative rounded-md border border-border bg-background px-2 py-1 text-sm hover:bg-accent hover:text-accent-foreground"
                >
                  <span aria-hidden="true">🔔</span>
                  {unreadCount > 0 ? (
                    <span
                      aria-hidden="true"
                      className="absolute -right-1 -top-1 inline-flex min-w-[1.25rem] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold leading-4 text-primary-foreground"
                    >
                      {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                  ) : null}
                </button>
                <button
                  type="button"
                  onClick={logout}
                  className="rounded-md border border-border bg-background px-3 py-1 text-sm hover:bg-accent hover:text-accent-foreground"
                >
                  Log out
                </button>
              </>
            ) : (
              <Link to="/login" className="text-sm hover:underline">
                Sign in
              </Link>
            )}
          </nav>
        </div>
      </header>
      <div className="flex-1">{children}</div>
      {isAuthenticated ? (
        <NotificationsPanel
          open={notificationsOpen}
          onClose={() => setNotificationsOpen(false)}
        />
      ) : null}
    </div>
  );
}
