import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useCategoriesQuery, useIdeasQuery } from '@/features/ideas/api';
import type { IdeasFilter } from '@/features/ideas/api';
import { StatusBadge } from '@/components/state/StatusBadge';
import type { IdeaStatus } from '@/components/state/StatusBadge';
import { LoadingState } from '@/components/state/LoadingState';
import { ErrorState } from '@/components/state/ErrorState';
import { EmptyState } from '@/components/state/EmptyState';
import { formatIdeaDate } from '@/lib/date';

const STATUS_OPTIONS: IdeaStatus[] = ['Submitted', 'UnderReview', 'Accepted', 'Rejected'];
const PAGE_SIZE = 20;

/** Idea list view (T075) — filterable + paginated. */
export function IdeasListPage(): JSX.Element {
  const categoriesQuery = useCategoriesQuery();
  const [filter, setFilter] = useState<IdeasFilter>({});
  const [page, setPage] = useState(1);
  const ideasQuery = useIdeasQuery(filter, page, PAGE_SIZE);

  const totalPages = ideasQuery.data ? Math.max(1, Math.ceil(ideasQuery.data.total / PAGE_SIZE)) : 1;

  return (
    <main className="mx-auto max-w-6xl px-4 py-8">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Ideas</h1>
          <p className="mt-1 text-sm text-muted-foreground">Browse all ideas submitted to the portal.</p>
        </div>
        <Link
          to="/ideas/new"
          className="inline-flex items-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
        >
          Submit new idea
        </Link>
      </header>

      <section className="mt-6 flex flex-wrap gap-3">
        <select
          value={filter.status ?? ''}
          onChange={(e) => {
            setPage(1);
            setFilter((f) => ({ ...f, status: (e.target.value || undefined) as IdeaStatus | undefined }));
          }}
          className="rounded-md border border-input bg-background px-3 py-1.5 text-sm"
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>

        <select
          value={filter.categoryCode ?? ''}
          onChange={(e) => {
            setPage(1);
            setFilter((f) => ({ ...f, categoryCode: e.target.value || undefined }));
          }}
          className="rounded-md border border-input bg-background px-3 py-1.5 text-sm"
          disabled={categoriesQuery.isLoading}
        >
          <option value="">All categories</option>
          {(categoriesQuery.data ?? []).map((c) => (
            <option key={c.id} value={c.code}>
              {c.name}
            </option>
          ))}
        </select>
      </section>

      <section className="mt-6">
        {ideasQuery.isLoading ? (
          <LoadingState />
        ) : ideasQuery.isError ? (
          <ErrorState message="Could not load ideas." onRetry={() => ideasQuery.refetch()} />
        ) : !ideasQuery.data || ideasQuery.data.items.length === 0 ? (
          <EmptyState title="No ideas yet" description="Be the first â€” submit a new idea." />
        ) : (
          <>
            <div className="overflow-hidden rounded-md border border-border">
              <table className="min-w-full divide-y divide-border text-sm">
                <thead className="bg-muted/40 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3">Title</th>
                    <th className="px-4 py-3">Category</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3">Submitter</th>
                    <th className="px-4 py-3">Submitted</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border bg-background">
                  {ideasQuery.data.items.map((idea) => (
                    <tr key={idea.id} className="hover:bg-muted/30">
                      <td className="px-4 py-3">
                        <Link to={`/ideas/${idea.id}`} className="font-medium text-primary hover:underline">
                          {idea.title}
                        </Link>
                        {idea.hasAttachment ? (
                          <span className="ml-2 text-xs text-muted-foreground">ðŸ“Ž</span>
                        ) : null}
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">{idea.categoryName}</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={idea.status} />
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">{idea.submitterName}</td>
                      <td className="px-4 py-3 text-muted-foreground">{formatIdeaDate(idea.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
              <span>
                Showing {(page - 1) * PAGE_SIZE + 1}â€“{Math.min(page * PAGE_SIZE, ideasQuery.data.total)} of {ideasQuery.data.total}
              </span>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                  className="rounded-md border border-border px-3 py-1 disabled:opacity-50"
                >
                  Previous
                </button>
                <button
                  type="button"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                  className="rounded-md border border-border px-3 py-1 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </section>
    </main>
  );
}
