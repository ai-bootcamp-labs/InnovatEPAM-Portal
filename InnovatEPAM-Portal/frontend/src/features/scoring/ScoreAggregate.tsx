import { Card } from '@/components/ui';
import { formatIdeaDateTime } from '@/lib/date';
import type { IdeaScoreAggregate } from '@/features/ideas/api';

/**
 * Phase 7 — read-only display of the aggregated scores (FR-010, FR-012).
 * Used on the detail page so all admins see a consistent average + the list
 * of per-reviewer entries (reviewer identities are aliased server-side).
 */
export interface ScoreAggregateProps {
  aggregate: IdeaScoreAggregate;
}

function formatNum(value: number | null | undefined): string {
  return value == null ? '—' : value.toFixed(2);
}

export function ScoreAggregate({ aggregate }: ScoreAggregateProps): JSX.Element {
  if (aggregate.count === 0) {
    return (
      <Card>
        <div className="px-5 py-4 text-sm text-muted-foreground">
          No reviewer scores yet.
        </div>
      </Card>
    );
  }
  const avg = aggregate.averageByDimension;
  return (
    <Card>
      <div className="px-5 py-4">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 className="text-sm font-semibold text-foreground">Reviewer scores</h2>
          <p className="text-xs text-muted-foreground">
            {aggregate.count} reviewer{aggregate.count === 1 ? '' : 's'}
          </p>
        </div>
        <dl className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-5" data-testid="score-aggregate">
          <Stat label="Overall" value={formatNum(aggregate.overall)} emphasised />
          <Stat label="Impact" value={formatNum(avg?.impact)} />
          <Stat label="Feasibility" value={formatNum(avg?.feasibility)} />
          <Stat label="Innovation" value={formatNum(avg?.innovation)} />
          <Stat label="Alignment" value={formatNum(avg?.alignment)} />
        </dl>

        <ol className="mt-5 space-y-3">
          {aggregate.entries.map((entry) => (
            <li
              key={`${entry.reviewerAlias}-${entry.createdAt}`}
              className="rounded-md border border-border bg-muted/30 px-4 py-3 text-sm"
            >
              <p className="flex flex-wrap items-baseline justify-between gap-x-3 text-foreground">
                <span className="font-mono">{entry.reviewerAlias}</span>
                <span className="text-xs text-muted-foreground">
                  {formatIdeaDateTime(entry.updatedAt)}
                </span>
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                Impact {entry.impact} · Feasibility {entry.feasibility} ·
                {' '}Innovation {entry.innovation} · Alignment {entry.alignment}
              </p>
              {entry.comment ? (
                <p className="mt-2 whitespace-pre-wrap text-sm text-foreground">{entry.comment}</p>
              ) : null}
            </li>
          ))}
        </ol>
      </div>
    </Card>
  );
}

function Stat({ label, value, emphasised }: { label: string; value: string; emphasised?: boolean }) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd
        className={
          emphasised
            ? 'mt-1 text-2xl font-semibold text-foreground'
            : 'mt-1 text-lg font-semibold text-foreground/90'
        }
      >
        {value}
      </dd>
    </div>
  );
}
