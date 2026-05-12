import { useParams } from 'react-router-dom';
import { useIdeaQuery } from '@/features/ideas/api';
import { StatusBadge } from '@/components/state/StatusBadge';
import { LoadingState } from '@/components/state/LoadingState';
import { ErrorState } from '@/components/state/ErrorState';
import { formatIdeaDateTime } from '@/lib/date';
import { apiClient } from '@/lib/api/client';

/** Read-only idea detail page (T076). */
export function IdeaDetailPage(): JSX.Element {
  const { id } = useParams<{ id: string }>();
  const ideaQuery = useIdeaQuery(id);

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
    </main>
  );
}
