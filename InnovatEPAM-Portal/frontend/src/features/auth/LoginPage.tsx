import { useEffect, useId, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider';
import { ApiError } from '@/lib/api/client';
import { Container } from '@/components/layout/Container';
import { Button, Input, Label } from '@/components/ui';

/** Login page (T072) — honours `?returnUrl=` so guards can round-trip. */
export function LoginPage(): JSX.Element {
  const { login, isAuthenticated } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const formId = useId();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const emailId = `${formId}-email`;
  const passwordId = `${formId}-password`;
  const params = new URLSearchParams(location.search);
  const returnUrl = decodeURIComponent(params.get('returnUrl') ?? '/ideas');
  const safeReturn = returnUrl.startsWith('/') ? returnUrl : '/ideas';

  useEffect(() => {
    if (isAuthenticated) navigate(safeReturn, { replace: true });
  }, [isAuthenticated, navigate, safeReturn]);

  if (isAuthenticated) return <Navigate to={safeReturn} replace />;

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await login({ email: email.trim(), password });
      navigate(safeReturn, { replace: true });
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.problem.detail ?? err.problem.title ?? 'Login failed.'
          : 'Login failed.';
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Container as="main" className="max-w-md py-16">
      <h1 className="text-3xl font-semibold tracking-tight">Sign in</h1>
      <p className="mt-2 text-sm text-muted-foreground">Welcome back to the InnovatEPAM Portal.</p>

      <form className="mt-8 space-y-5" onSubmit={onSubmit} noValidate>
        <div className="space-y-1">
          <Label htmlFor={emailId} required>
            Email
          </Label>
          <Input
            id={emailId}
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={submitting}
            aria-required
            required
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor={passwordId} required>
            Password
          </Label>
          <Input
            id={passwordId}
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={submitting}
            aria-required
            required
          />
        </div>

        {error ? (
          <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error}
          </p>
        ) : null}

        <Button
          type="submit"
          variant="primary"
          loading={submitting}
          disabled={submitting || !email || !password}
          className="w-full"
        >
          {submitting ? 'Signing in…' : 'Sign in'}
        </Button>
      </form>

      <p className="mt-6 text-sm text-muted-foreground">
        New here?{' '}
        <Link
          to="/register"
          className="rounded font-medium text-primary hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          Create an account
        </Link>
      </p>
    </Container>
  );
}
