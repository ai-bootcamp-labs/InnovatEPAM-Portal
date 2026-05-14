import { useId, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider';
import { ApiError } from '@/lib/api/client';
import { Container } from '@/components/layout/Container';
import { Button, FieldError, Input, Label } from '@/components/ui';

/** Register form schema (T071) — matches backend RegisterRequestValidator. */
const schema = z.object({
  displayName: z.string().min(1, 'Display name is required.').max(120, 'Display name is too long.'),
  email: z.string().email('Enter a valid email.'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters.')
    .regex(/[a-z]/, 'Password must contain a lowercase letter.')
    .regex(/[0-9]/, 'Password must contain a digit.'),
});

type FormValues = z.infer<typeof schema>;

export function RegisterPage(): JSX.Element {
  const { register: registerUser, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);
  const formId = useId();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), mode: 'onBlur' });

  if (isAuthenticated) return <Navigate to="/ideas" replace />;

  async function onSubmit(values: FormValues) {
    setServerError(null);
    try {
      await registerUser({
        displayName: values.displayName.trim(),
        email: values.email.trim(),
        password: values.password,
      });
      navigate('/ideas', { replace: true });
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.problem.detail ?? err.problem.title ?? 'Registration failed.'
          : 'Registration failed.';
      setServerError(message);
    }
  }

  const fieldDefs = [
    { name: 'displayName' as const, label: 'Display name', type: 'text', autoComplete: 'name' },
    { name: 'email' as const, label: 'Email', type: 'email', autoComplete: 'email' },
    { name: 'password' as const, label: 'Password', type: 'password', autoComplete: 'new-password' },
  ];

  return (
    <Container as="main" className="max-w-md py-16">
      <h1 className="text-3xl font-semibold tracking-tight">Create account</h1>
      <p className="mt-2 text-sm text-muted-foreground">Submit and track your innovation ideas.</p>

      <form className="mt-8 space-y-5" onSubmit={handleSubmit(onSubmit)} noValidate>
        {fieldDefs.map((f) => {
          const fieldId = `${formId}-${f.name}`;
          const errorId = `${fieldId}-error`;
          const message = errors[f.name]?.message;
          return (
            <div key={f.name} className="space-y-1">
              <Label htmlFor={fieldId} required>
                {f.label}
              </Label>
              <Input
                id={fieldId}
                type={f.type}
                autoComplete={f.autoComplete}
                aria-required
                aria-describedby={message ? errorId : undefined}
                invalid={Boolean(message)}
                disabled={isSubmitting}
                {...register(f.name)}
              />
              <FieldError id={errorId}>{message}</FieldError>
            </div>
          );
        })}

        {serverError ? (
          <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {serverError}
          </p>
        ) : null}

        <Button
          type="submit"
          variant="primary"
          loading={isSubmitting}
          disabled={isSubmitting}
          className="w-full"
        >
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </Button>
      </form>

      <p className="mt-6 text-sm text-muted-foreground">
        Already have an account?{' '}
        <Link
          to="/login"
          className="rounded font-medium text-primary hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          Sign in
        </Link>
      </p>
    </Container>
  );
}
