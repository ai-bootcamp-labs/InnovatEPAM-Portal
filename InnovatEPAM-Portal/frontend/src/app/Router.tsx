import { Navigate, Route, Routes, Link } from 'react-router-dom';
import { RequireAuth, RequireRole } from '@/app/guards';
import { AppShell } from '@/components/layout/AppShell';
import { Container } from '@/components/layout/Container';
import { EmptyState } from '@/components/ui';
import { cn } from '@/lib/ui/cn';
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
              <AdminLandingPage />
            </AppShell>
          </RequireRole>
        }
      />

      <Route path="*" element={<PlaceholderPage title="Not found" subtitle="The page you requested does not exist." />} />
    </Routes>
  );
}

/**
 * Admin landing page (T045, 008-ui-polish). The admin queue uses the public
 * idea list (`/ideas?status=Submitted` etc.) so the same `Card` + `StatusBadge`
 * + `EmptyState` + `LoadingSkeleton` treatment is rendered for admins — this
 * landing page just routes them there. Decision actions appear inline on the
 * idea detail page via `DecideControls`.
 */
function AdminLandingPage(): JSX.Element {
  return (
    <Container as="main" className="max-w-3xl py-16">
      <EmptyState
        title="Admin review"
        description="The review queue lives on the shared ideas list. Filter by status to triage submissions; decision actions appear on each idea's detail page."
        action={
          <Link
            to="/ideas?status=Submitted"
            // ui-polish-exception: <Link> needs anchor semantics; mirrors Button primary styling.
            className={cn(
              'inline-flex h-9 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground',
              'hover:bg-primary/90',
              'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
              'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
            )}
          >
            Open submitted queue
          </Link>
        }
      />
    </Container>
  );
}

function PlaceholderPage({ title, subtitle }: { title: string; subtitle: string }): JSX.Element {
  return (
    <Container as="main" className="max-w-3xl py-16">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">{title}</h1>
      <p className="mt-2 text-muted-foreground">{subtitle}</p>
    </Container>
  );
}
