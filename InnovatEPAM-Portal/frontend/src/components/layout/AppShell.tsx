import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider';
import { NotificationsPanel } from '@/features/notifications/NotificationsPanel';
import { useUnreadCount } from '@/features/notifications/useUnreadCount';
import { Button } from '@/components/ui';
import { Container } from './Container';
import { cn } from '@/lib/ui/cn';

/**
 * Application chrome (008-ui-polish T021).
 *
 * Renders a styled top bar (brand + role badge + notifications + log-out)
 * inside a shared `Container`. Under `md` the secondary chrome (display
 * name + log-out) collapses behind a hamburger button so the bar stays
 * usable down to 375 px (FR-010, US1 acceptance #2).
 *
 * NOTE: the `<main>` landmark currently lives in individual pages so we
 * don't double up landmarks during the refactor. Phase 3 task T026 will
 * lift it into this shell once every feature page consumes `Container`
 * directly.
 */
export function AppShell({ children }: { children: ReactNode }): JSX.Element {
  const { user, isAuthenticated, logout } = useAuth();
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const unreadCountQuery = useUnreadCount(isAuthenticated);
  const unreadCount = unreadCountQuery.data ?? 0;

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="border-b border-border bg-card">
        <Container className="flex h-14 items-center justify-between gap-4">
          <Link
            to="/ideas"
            className={cn(
              'rounded-md font-semibold tracking-tight text-foreground',
              'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
              'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
              'hover:text-foreground/80',
            )}
          >
            InnovatEPAM Portal
          </Link>

          {isAuthenticated && user ? (
            <>
              {/* Desktop bar (>= md) */}
              <nav
                aria-label="Primary"
                className="hidden items-center gap-3 text-sm md:flex"
              >
                <span className="rounded-full border border-border bg-secondary px-2 py-0.5 text-xs font-medium text-secondary-foreground">
                  {user.role}
                </span>
                <span className="text-muted-foreground">{user.displayName}</span>
                <NotificationsButton
                  unreadCount={unreadCount}
                  onClick={() => setNotificationsOpen(true)}
                />
                <Button variant="secondary" size="sm" onClick={logout}>
                  Log out
                </Button>
              </nav>

              {/* Mobile bar (< md): bell + hamburger */}
              <div className="flex items-center gap-2 md:hidden">
                <NotificationsButton
                  unreadCount={unreadCount}
                  onClick={() => setNotificationsOpen(true)}
                />
                <Button
                  variant="ghost"
                  size="sm"
                  aria-controls="app-shell-mobile-menu"
                  aria-expanded={mobileMenuOpen}
                  aria-label={mobileMenuOpen ? 'Close menu' : 'Open menu'}
                  onClick={() => setMobileMenuOpen((open) => !open)}
                >
                  <span aria-hidden="true" className="text-lg leading-none">
                    {mobileMenuOpen ? '✕' : '☰'}
                  </span>
                </Button>
              </div>
            </>
          ) : (
            <nav aria-label="Primary" className="flex items-center gap-3 text-sm">
              <Link
                to="/login"
                className={cn(
                  'rounded-md px-2 py-1 text-sm text-foreground hover:underline',
                  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
                )}
              >
                Sign in
              </Link>
            </nav>
          )}
        </Container>

        {/* Mobile collapse panel: visible only when toggled, under md */}
        {isAuthenticated && user && mobileMenuOpen ? (
          <div
            id="app-shell-mobile-menu"
            className="border-t border-border bg-card md:hidden"
          >
            <Container className="flex flex-col gap-2 py-3 text-sm">
              <div className="flex items-center gap-2">
                <span className="rounded-full border border-border bg-secondary px-2 py-0.5 text-xs font-medium text-secondary-foreground">
                  {user.role}
                </span>
                <span className="text-muted-foreground">{user.displayName}</span>
              </div>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setMobileMenuOpen(false);
                  logout();
                }}
              >
                Log out
              </Button>
            </Container>
          </div>
        ) : null}
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

function NotificationsButton({
  unreadCount,
  onClick,
}: {
  unreadCount: number;
  onClick: () => void;
}): JSX.Element {
  const label =
    unreadCount > 0 ? `Notifications (${unreadCount} unread)` : 'Notifications';
  return (
    // ui-polish-exception: icon-only trigger with an absolutely-positioned unread
    // badge. The Button primitive's fixed padding/sizing rhythm doesn't fit a
    // 32×32 icon control; the focus/hover/motion tokens below mirror Button.
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      className={cn(
        'relative inline-flex h-8 items-center justify-center rounded-md border border-border bg-background px-2 text-sm text-foreground',
        'hover:bg-accent hover:text-accent-foreground',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
        'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
      )}
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
  );
}
