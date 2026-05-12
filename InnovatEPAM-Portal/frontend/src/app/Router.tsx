import { Navigate, Route, Routes } from 'react-router-dom';
import { RequireAuth, RequireRole } from '@/app/guards';
import { AppShell } from '@/components/layout/AppShell';
import { LoginPage } from '@/features/auth/LoginPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { IdeasListPage } from '@/features/ideas/IdeasListPage';
import { IdeaCreatePage } from '@/features/ideas/IdeaCreatePage';
import { IdeaDetailPage } from '@/features/ideas/IdeaDetailPage';

/** Application route map (T038, updated for Phase 3 / US1). */
export function Router(): JSX.Element {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/ideas" replace />} />

      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route
        path="/ideas"
        element={
          <RequireAuth>
            <AppShell>
              <IdeasListPage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/ideas/new"
        element={
          <RequireAuth>
            <AppShell>
              <IdeaCreatePage />
            </AppShell>
          </RequireAuth>
        }
      />
      <Route
        path="/ideas/:id"
        element={
          <RequireAuth>
            <AppShell>
              <IdeaDetailPage />
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
