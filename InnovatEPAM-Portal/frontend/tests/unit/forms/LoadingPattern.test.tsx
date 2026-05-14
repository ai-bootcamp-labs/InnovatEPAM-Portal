import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

// T032 — assert the shared loading pattern used by every Phase 4 form:
//  - Submit button surfaces aria-busy="true" + is disabled
//  - Form text inputs become non-interactive (disabled) during submission
//
// We use LoginPage as the representative form because it owns its own
// `submitting` state via `useState`, so we can drive it with a Promise
// the test controls.

const loginMock = vi.fn();

vi.mock('@/features/auth/AuthProvider', () => ({
  useAuth: () => ({
    user: null,
    token: null,
    isAuthenticated: false,
    register: vi.fn(),
    login: loginMock,
    logout: vi.fn(),
  }),
}));

import { LoginPage } from '@/features/auth/LoginPage';

function renderPage() {
  return render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>,
  );
}

describe('Shared form loading pattern (T032)', () => {
  it('disables every text input and sets aria-busy on the submit button while submitting', async () => {
    let resolveLogin: (() => void) | null = null;
    loginMock.mockImplementationOnce(
      () =>
        new Promise<void>((resolve) => {
          resolveLogin = () => resolve();
        }),
    );
    const user = userEvent.setup();
    renderPage();

    const email = screen.getByLabelText(/email/i);
    const password = screen.getByLabelText(/password/i);
    await user.type(email, 'ada@example.com');
    await user.type(password, 'Passw0rd');

    const submit = screen.getByRole('button', { name: /sign in/i });
    expect(submit).not.toHaveAttribute('aria-busy');
    expect(email).not.toBeDisabled();

    await user.click(submit);

    // While the login promise is pending, the loading affordances apply.
    await waitFor(() => {
      expect(submit).toHaveAttribute('aria-busy', 'true');
    });
    expect(submit).toBeDisabled();
    expect(email).toBeDisabled();
    expect(password).toBeDisabled();

    // Resolve the promise so the test can finish cleanly.
    resolveLogin!();
  });

  it('marks the form as busy via a Spinner inside the submit button', async () => {
    loginMock.mockImplementationOnce(() => new Promise<void>(() => {}));
    const user = userEvent.setup();
    renderPage();

    await user.type(screen.getByLabelText(/email/i), 'ada@example.com');
    await user.type(screen.getByLabelText(/password/i), 'Passw0rd');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    // The button label flips to "Signing in…" while the request is in flight.
    const submit = await screen.findByRole('button', { name: /signing in/i });
    // The loading Spinner is rendered as role="img" with the accessible
    // name "Loading" inside the button.
    expect(within(submit).getByRole('img', { name: /loading/i })).toBeInTheDocument();
  });
});
