import { Navigate, Route, Routes } from 'react-router-dom';
import { RequireAuth, RequireRole } from '@/app/guards';
import { AppShell } from '@/components/layout/AppShell';

/**
 * Application route map (T038). All real pages are placeholders for Phase 2;
 * Phase 3 (US1) wires in the login/register/idea pages.
 */
export function Router(): JSX.Element {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/ideas" replace />} />

      <Route path="/login" element={<PlaceholderPage title="Sign in" subtitle="Login form lands in T072 (Phase 3 / US1)." />} />
      <Route path="/register" element={<PlaceholderPage title="Create account" subtitle="Register form lands in T071 (Phase 3 / US1)." />} />

      <Route
        path="/ideas"
        element={
          <RequireAuth>
            <AppShell>
              <PlaceholderPage title="Ideas" subtitle="List view lands in T075 (Phase 3 / US1)." />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/ideas/new"
        element={
          <RequireAuth>
            <AppShell>
              <PlaceholderPage title="New idea" subtitle="Submit form lands in T074 (Phase 3 / US1)." />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/ideas/:id"
        element={
          <RequireAuth>
            <AppShell>
              <PlaceholderPage title="Idea details" subtitle="Detail view lands in T076 (Phase 3 / US1)." />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/admin"
        element={
          // eslint-disable-next-line jsx-a11y/aria-role -- `role` here is our domain prop, not the ARIA attribute.
          <RequireRole role="Admin">
            <AppShell>
              <PlaceholderPage title="Admin review" subtitle="Admin dashboard lands in Phase 4 (US2)." />
            </AppShell>
          </RequireRole>
        }
      />

      <Route path="*" element={<PlaceholderPage title="Not found" subtitle="The page you requested does not exist." />} />
    </Routes>
  );
}

function PlaceholderPage({ title, subtitle }: { title: string; subtitle: string }): JSX.Element {
  return (
    <main className="mx-auto max-w-3xl px-4 py-16">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">{title}</h1>
      <p className="mt-2 text-muted-foreground">{subtitle}</p>
    </main>
  );
}
