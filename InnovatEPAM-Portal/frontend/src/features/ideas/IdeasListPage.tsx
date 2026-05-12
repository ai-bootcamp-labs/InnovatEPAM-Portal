import { Link, useSearchParams } from 'react-router-dom';
import { useIdeasQuery } from '@/features/ideas/api';
import { IdeasFilters, useIdeasUrlState } from '@/features/ideas/IdeasFilters';
import { StatusBadge } from '@/components/state/StatusBadge';
import { LoadingState } from '@/components/state/LoadingState';
import { ErrorState } from '@/components/state/ErrorState';
import { EmptyState } from '@/components/state/EmptyState';
import { formatIdeaDate } from '@/lib/date';

/** Idea list view (T075, refactored in T097–T099). URL is the single source of truth. */
export function IdeasListPage(): JSX.Element {
  const { filter, page, pageSize } = useIdeasUrlState();
  const [params, setParams] = useSearchParams();
  const ideasQuery = useIdeasQuery(filter, page, pageSize);

  const total = ideasQuery.data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const filterDescription = describeFilter(filter);

  function setPage(next: number) {
    const clamped = Math.min(Math.max(1, next), totalPages);
    const nextParams = new URLSearchParams(params);
    if (clamped <= 1) nextParams.delete('page');
    else nextParams.set('page', String(clamped));
    setParams(nextParams, { replace: true });
  }

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

      <div className="mt-6">
        <IdeasFilters />
      </div>

      <section className="mt-6" aria-live="polite">
        {ideasQuery.isLoading ? (
          <LoadingState />
        ) : ideasQuery.isError ? (
          <ErrorState
            message="Could not load ideas with the current filters."
            onRetry={() => ideasQuery.refetch()}
          />
        ) : !ideasQuery.data || ideasQuery.data.items.length === 0 ? (
          <EmptyState
            title={`No ${filterDescription} ideas yet`}
            description={
              hasActiveFilter(filter)
                ? 'Try widening your filters or submit a new idea.'
                : 'Be the first — submit a new idea.'
            }
          />
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
                          <span
                            className="ml-2 text-xs text-muted-foreground"
                            aria-label="Has attachment"
                            title="Has attachment"
                          >
                            📎
                          </span>
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
                Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}
              </span>
              {total > pageSize ? (
                <div className="flex items-center gap-2">
                  <span className="hidden sm:inline">
                    Page {page} of {totalPages}
                  </span>
                  <button
                    type="button"
                    onClick={() => setPage(page - 1)}
                    disabled={page <= 1}
                    className="rounded-md border border-border px-3 py-1 disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <button
                    type="button"
                    onClick={() => setPage(page + 1)}
                    disabled={page >= totalPages}
                    className="rounded-md border border-border px-3 py-1 disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
              ) : null}
            </div>
          </>
        )}
      </section>
    </main>
  );
}

function hasActiveFilter(filter: { status?: string; categoryCode?: string }): boolean {
  return Boolean(filter.status) || Boolean(filter.categoryCode);
}

/** "No Rejected ideas yet" / "No Process ideas yet" / "No matching ideas yet". */
function describeFilter(filter: { status?: string; categoryCode?: string }): string {
  const parts: string[] = [];
  if (filter.status) parts.push(filter.status);
  if (filter.categoryCode) parts.push(filter.categoryCode);
  if (parts.length === 0) return 'matching';
  return parts.join(' / ');
}
