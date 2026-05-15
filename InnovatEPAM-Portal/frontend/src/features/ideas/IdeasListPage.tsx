import { Link, useSearchParams } from 'react-router-dom';
import type { SVGProps } from 'react';
import { useIdeasQuery } from '@/features/ideas/api';
import { IdeasFilters, useIdeasUrlState } from '@/features/ideas/IdeasFilters';
import { ErrorState } from '@/components/state/ErrorState';
import { Container } from '@/components/layout/Container';
import { Button, Card, EmptyState, LoadingSkeleton, StatusBadge } from '@/components/ui';
import { cn } from '@/lib/ui/cn';
import { formatIdeaDate } from '@/lib/date';
import { SubmitterLabel } from '@/features/ideas/SubmitterLabel';

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
    <Container as="main" className="py-8">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Ideas</h1>
          <p className="mt-1 text-sm text-muted-foreground">Browse all ideas submitted to the portal.</p>
        </div>
        <Link
          to="/ideas/new"
          // ui-polish-exception: <Link> needs anchor semantics; mirrors Button primary styling.
          className={cn(
            'inline-flex h-9 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground',
            'hover:bg-primary/90',
            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
            'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
          )}
        >
          Submit new idea
        </Link>
      </header>

      <div className="mt-6">
        <IdeasFilters />
      </div>

      <section className="mt-6" aria-live="polite">
        {ideasQuery.isLoading ? (
          <LoadingSkeleton rows={Math.min(pageSize, 6)} rowClassName="h-20" />
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
            action={
              <Link
                to="/ideas/new"
                // ui-polish-exception: <Link> needs anchor semantics; mirrors Button primary styling.
                className={cn(
                  'inline-flex h-9 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground',
                  'hover:bg-primary/90',
                  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
                  'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
                )}
              >
                Submit new idea
              </Link>
            }
          />
        ) : (
          <>
            <ul className="grid gap-3" aria-label="Ideas">
              {ideasQuery.data.items.map((idea) => (
                <li key={idea.id}>
                  <Card hoverable className="px-4 py-3">
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <Link
                            to={`/ideas/${idea.id}`}
                            className="rounded text-base font-medium text-primary hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                          >
                            <span className="break-words">{idea.title}</span>
                          </Link>
                          {idea.hasAttachment ? (
                            <span
                              className="inline-flex items-center gap-1 text-xs text-muted-foreground"
                              aria-label="Has attachment"
                              title="Has attachment"
                            >
                              <AttachmentIcon className="h-3.5 w-3.5" aria-hidden="true" />
                              Attachment
                            </span>
                          ) : null}
                        </div>
                        <dl className="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                          <div className="flex items-center gap-1">
                            <dt className="sr-only">Category</dt>
                            <dd>{idea.categoryName}</dd>
                          </div>
                          <span aria-hidden="true">·</span>
                          <div className="flex items-center gap-1">
                            <dt className="sr-only">Submitter</dt>
                            <dd>
                              <SubmitterLabel
                                name={idea.submitterName}
                                alias={idea.submitterAlias}
                              />
                            </dd>
                          </div>
                          <span aria-hidden="true">·</span>
                          <div className="flex items-center gap-1">
                            <dt className="sr-only">Submitted</dt>
                            <dd>{formatIdeaDate(idea.createdAt)}</dd>
                          </div>
                          {idea.reviewerCount > 0 ? (
                            <>
                              <span aria-hidden="true">·</span>
                              <div className="flex items-center gap-1" data-testid="idea-score">
                                <dt className="sr-only">Score</dt>
                                <dd className="tabular-nums">
                                  <span className="font-semibold text-foreground">
                                    {(idea.overall ?? 0).toFixed(2)}
                                  </span>{' '}
                                  ({idea.reviewerCount})
                                </dd>
                              </div>
                            </>
                          ) : null}
                        </dl>
                      </div>
                      <div className="flex-shrink-0">
                        <StatusBadge status={idea.status} />
                      </div>
                    </div>
                  </Card>
                </li>
              ))}
            </ul>

            <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
              <span>
                Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}
              </span>
              {total > pageSize ? (
                <div className="flex items-center gap-2">
                  <span className="hidden sm:inline">
                    Page {page} of {totalPages}
                  </span>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setPage(page - 1)}
                    disabled={page <= 1}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setPage(page + 1)}
                    disabled={page >= totalPages}
                  >
                    Next
                  </Button>
                </div>
              ) : null}
            </div>
          </>
        )}
      </section>
    </Container>
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

function AttachmentIcon(props: SVGProps<SVGSVGElement>): JSX.Element {
  return (
    <svg viewBox="0 0 20 20" fill="none" {...props}>
      <path
        d="M12.5 5.5l-6 6a2.5 2.5 0 1 0 3.54 3.54l6.36-6.36a4 4 0 1 0-5.66-5.66L4.5 9.5"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
