import { describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

// We must hoist mocks before importing the SUT.
vi.mock('@/features/auth/AuthProvider', () => ({
  useAuth: vi.fn(),
}));

vi.mock('@/features/notifications/useUnreadCount', () => ({
  useUnreadCount: vi.fn(),
}));

vi.mock('@/features/notifications/NotificationsPanel', () => ({
  NotificationsPanel: () => null,
}));

import { AppShell } from '@/components/layout/AppShell';
import { useAuth } from '@/features/auth/AuthProvider';
import { useUnreadCount } from '@/features/notifications/useUnreadCount';

const useAuthMock = vi.mocked(useAuth);
const useUnreadCountMock = vi.mocked(useUnreadCount);

function authed(role: 'Employee' | 'Admin' = 'Employee') {
  useAuthMock.mockReturnValue({
    user: { id: 'u1', displayName: 'Ada Lovelace', role, email: 'ada@example.com' },
    token: 't',
    isAuthenticated: true,
    register: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
  } as unknown as ReturnType<typeof useAuth>);
}

function withShell() {
  return render(
    <MemoryRouter>
      <AppShell>
        <p>page body</p>
      </AppShell>
    </MemoryRouter>,
  );
}

describe('AppShell (T024)', () => {
  it('renders the brand link, primary nav, and notifications affordance for an authed user', () => {
    authed();
    useUnreadCountMock.mockReturnValue({ data: 3 } as unknown as ReturnType<typeof useUnreadCount>);

    withShell();

    expect(screen.getByRole('link', { name: /innovatepam portal/i })).toBeInTheDocument();
    // primary nav landmark
    const nav = screen.getByRole('navigation', { name: /primary/i });
    expect(nav).toBeInTheDocument();
    // notifications affordance surfaces the unread count in its accessible name
    // (rendered twice — once in the desktop nav, once in the mobile bar — both
    //  always in the DOM since jsdom can't evaluate `md:` media queries).
    const bells = screen.getAllByRole('button', { name: /notifications \(3 unread\)/i });
    expect(bells.length).toBeGreaterThanOrEqual(1);
    // user menu trigger (log out) keyboard-reachable inside the primary nav
    expect(within(nav).getByRole('button', { name: /log out/i })).toBeInTheDocument();
  });

  it('mobile menu toggle is keyboard-reachable and toggles aria-expanded', async () => {
    authed();
    useUnreadCountMock.mockReturnValue({ data: 0 } as unknown as ReturnType<typeof useUnreadCount>);
    const user = userEvent.setup();

    withShell();

    const trigger = screen.getByRole('button', { name: /open menu/i });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');
    expect(trigger).toHaveAttribute('aria-controls', 'app-shell-mobile-menu');

    trigger.focus();
    expect(trigger).toHaveFocus();

    await user.click(trigger);
    expect(
      screen.getByRole('button', { name: /close menu/i }),
    ).toHaveAttribute('aria-expanded', 'true');
  });

  it('falls back to a sign-in link when the user is unauthenticated', () => {
    useAuthMock.mockReturnValue({
      user: null,
      token: null,
      isAuthenticated: false,
      register: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    } as unknown as ReturnType<typeof useAuth>);
    useUnreadCountMock.mockReturnValue({ data: 0 } as unknown as ReturnType<typeof useUnreadCount>);

    withShell();

    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /notifications/i })).not.toBeInTheDocument();
  });
});
