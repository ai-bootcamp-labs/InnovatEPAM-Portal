import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useIdeaQuery } from '@/features/ideas/api';
import { StatusBadge } from '@/components/state/StatusBadge';
import { LoadingState } from '@/components/state/LoadingState';
import { ErrorState } from '@/components/state/ErrorState';
import { formatIdeaDateTime } from '@/lib/date';
import { apiClient } from '@/lib/api/client';
import { DecideControls } from '@/features/admin/DecideControls';
import { useIdeaHistoryQuery } from '@/features/admin/api';

/** Read-only idea detail page (T076). */
export function IdeaDetailPage(): JSX.Element {
  const { id } = useParams<{ id: string }>();
  const ideaQuery = useIdeaQuery(id);
  const historyQuery = useIdeaHistoryQuery(id);
  const [flash, setFlash] = useState<string | null>(null);

  if (ideaQuery.isLoading) {
    return (
      <main className="mx-auto max-w-4xl px-4 py-8">
        <LoadingState />
      </main>
    );
  }
  if (ideaQuery.isError || !ideaQuery.data) {
    return (
      <main className="mx-auto max-w-4xl px-4 py-8">
        <ErrorState title="Idea not available" message="Could not load this idea." onRetry={() => ideaQuery.refetch()} />
      </main>
    );
  }
  const idea = ideaQuery.data;

  async function downloadAttachment() {
    if (!idea.attachment) return;
    const { blob, fileName } = await apiClient.fetchBlob(`/ideas/${idea.id}/attachment`);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName ?? idea.attachment.originalFileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-8">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">{idea.title}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Submitted by {idea.submitterName} on {formatIdeaDateTime(idea.createdAt)} · {idea.categoryName}
          </p>
        </div>
        <StatusBadge status={idea.status} />
      </header>

      <div className="mt-4">
        <DecideControls idea={idea} onDecisionRecorded={(msg) => setFlash(msg)} />
      </div>

      {flash ? (
        <p
          role="status"
          className="mt-3 rounded-md border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-700 dark:text-emerald-300"
        >
          {flash}
        </p>
      ) : null}

      <section className="mt-6 whitespace-pre-wrap rounded-md border border-border bg-card p-6 text-sm leading-relaxed text-foreground">
        {idea.description}
      </section>

      {idea.attachment ? (
        <section className="mt-6 rounded-md border border-border bg-card p-4">
          <h2 className="text-sm font-semibold text-foreground">Attachment</h2>
          <div className="mt-2 flex items-center justify-between text-sm">
            <span className="text-muted-foreground">
              {idea.attachment.originalFileName} · {(idea.attachment.sizeBytes / 1024).toFixed(1)} KB
            </span>
            <button
              type="button"
              onClick={downloadAttachment}
              className="rounded-md border border-border bg-background px-3 py-1 text-sm hover:bg-accent hover:text-accent-foreground"
            >
              Download
            </button>
          </div>
        </section>
      ) : null}

      {idea.lastDecisionAt ? (
        <section className="mt-6 rounded-md border border-border bg-card p-4">
          <h2 className="text-sm font-semibold text-foreground">Latest decision</h2>
          <p className="mt-1 text-xs text-muted-foreground">
            {idea.lastDecisionByName} · {formatIdeaDateTime(idea.lastDecisionAt)}
          </p>
          {idea.lastDecisionComment ? (
            <p className="mt-2 whitespace-pre-wrap text-sm text-foreground">{idea.lastDecisionComment}</p>
          ) : null}
        </section>
      ) : null}

      <section className="mt-6 rounded-md border border-border bg-card p-4">
        <h2 className="text-sm font-semibold text-foreground">Status history</h2>
        {historyQuery.isLoading ? (
          <p className="mt-2 text-xs text-muted-foreground">Loading history…</p>
        ) : historyQuery.isError ? (
          <p className="mt-2 text-xs text-destructive">Could not load history.</p>
        ) : (historyQuery.data?.length ?? 0) === 0 ? (
          <p className="mt-2 text-xs text-muted-foreground">No history entries yet.</p>
        ) : (
          <ol className="mt-3 space-y-3 border-l border-border pl-4">
            {historyQuery.data!.map((entry) => (
              <li key={entry.id} className="relative">
                <span className="absolute -left-[21px] top-1 inline-block h-2.5 w-2.5 rounded-full bg-primary" aria-hidden="true" />
                <p className="text-sm text-foreground">
                  {entry.fromStatus ? (
                    <>
                      <span className="font-medium">{entry.fromStatus}</span> → <span className="font-medium">{entry.toStatus}</span>
                    </>
                  ) : (
                    <span className="font-medium">{entry.toStatus}</span>
                  )}
                </p>
                <p className="text-xs text-muted-foreground">
                  {entry.actorDisplayName} · {formatIdeaDateTime(entry.occurredAt)}
                </p>
                {entry.comment ? (
                  <p className="mt-1 whitespace-pre-wrap text-sm text-foreground">{entry.comment}</p>
                ) : null}
              </li>
            ))}
          </ol>
        )}
      </section>
    </main>
  );
}
