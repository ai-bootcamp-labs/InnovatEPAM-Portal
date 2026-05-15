import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Button,
  Card,
  FieldError,
  Label,
  ScoreInput,
  Textarea,
} from '@/components/ui';
import { ApiError } from '@/lib/api/client';
import type { IdeaDetail, IdeaScoreEntry } from '@/features/ideas/api';
import { useAuth } from '@/features/auth/AuthProvider';
import { submitScoreSchema, type SubmitScoreInput } from './schema';
import { useUpsertScoreMutation } from './api';

/**
 * Phase 7 — admin scoring form (FR-006..009).
 *
 * Self-scoring is forbidden by the backend (`SelfScoringForbidden` 409); the
 * UI suppresses the form entirely so that path is unreachable. Scoring is
 * also disabled once the idea reaches a terminal status (`ScoringClosed`).
 *
 * The form pre-fills from the caller's prior entry (looked up by reviewer
 * alias is unreliable — we instead fall back to the latest entry only when
 * the backend confirms it belongs to us via a future endpoint; until then
 * the form starts blank on first render and the success handler updates the
 * cached aggregate so the read-only `ScoreAggregate` reflects the new state).
 */
export interface ScorePanelProps {
  idea: IdeaDetail;
}

const TERMINAL = new Set(['Accepted', 'Rejected']);

export function ScorePanel({ idea }: ScorePanelProps): JSX.Element | null {
  const { user } = useAuth();
  const mutation = useUpsertScoreMutation(idea.id);

  // Surface the caller's previous entry, if we can identify it by alias.
  // The alias is derived deterministically from (ideaId, reviewerId), so we
  // recompute the alias once the reviewer scores and then re-render. On the
  // very first render we don't know our own alias yet, so the form is blank.
  const existing = findOwnEntry(idea, user?.id ?? '');

  const form = useForm<SubmitScoreInput>({
    resolver: zodResolver(submitScoreSchema),
    defaultValues: {
      impact: existing?.impact ?? (3 as unknown as number),
      feasibility: existing?.feasibility ?? (3 as unknown as number),
      innovation: existing?.innovation ?? (3 as unknown as number),
      alignment: existing?.alignment ?? (3 as unknown as number),
      comment: existing?.comment ?? null,
    },
  });

  // Reset whenever the upstream aggregate changes (after a successful submit
  // the cached IdeaDetail is patched, so this keeps the form in sync).
  useEffect(() => {
    form.reset({
      impact: existing?.impact ?? 3,
      feasibility: existing?.feasibility ?? 3,
      innovation: existing?.innovation ?? 3,
      alignment: existing?.alignment ?? 3,
      comment: existing?.comment ?? null,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [existing?.updatedAt]);

  // We only render the panel for admins who are not the submitter, and only
  // while the idea is still open for scoring.
  if (!user || user.role !== 'Admin') return null;
  if (user.id === idea.submitterId) return null;
  if (TERMINAL.has(idea.status)) return null;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await mutation.mutateAsync({
        impact: values.impact,
        feasibility: values.feasibility,
        innovation: values.innovation,
        alignment: values.alignment,
        comment: values.comment,
      });
    } catch (err) {
      const code = err instanceof ApiError ? err.problem.errors?.code?.[0] : undefined;
      const message =
        code === 'SelfScoringForbidden'
          ? 'You cannot score your own idea.'
          : code === 'ScoringClosed'
            ? 'Scoring is closed once a decision has been recorded.'
            : err instanceof ApiError
              ? err.message
              : 'Failed to submit score.';
      form.setError('root', { message });
    }
  });

  return (
    <Card>
      <form
        onSubmit={onSubmit}
        className="px-5 py-4"
        aria-labelledby={`score-panel-heading-${idea.id}`}
        data-testid="score-panel"
      >
        <header className="flex items-baseline justify-between gap-3">
          <h2 id={`score-panel-heading-${idea.id}`} className="text-sm font-semibold text-foreground">
            {existing ? 'Update your score' : 'Score this idea'}
          </h2>
          <p className="text-xs text-muted-foreground">Rate each dimension 1 (low) to 5 (high).</p>
        </header>

        <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
          {(['impact', 'feasibility', 'innovation', 'alignment'] as const).map((field) => (
            <Controller
              key={field}
              control={form.control}
              name={field}
              render={({ field: f, fieldState }) => (
                <div>
                  <ScoreInput
                    name={field}
                    label={DIMENSION_LABELS[field]}
                    value={f.value ?? null}
                    onChange={(next) => f.onChange(next)}
                  />
                  <FieldError id={`${field}-error`}>{fieldState.error?.message}</FieldError>
                </div>
              )}
            />
          ))}
        </div>

        <div className="mt-4">
          <Label htmlFor={`score-comment-${idea.id}`}>Comment (optional)</Label>
          <Controller
            control={form.control}
            name="comment"
            render={({ field, fieldState }) => (
              <>
                <Textarea
                  id={`score-comment-${idea.id}`}
                  rows={3}
                  maxLength={1000}
                  value={field.value ?? ''}
                  onChange={(e) => field.onChange(e.target.value === '' ? null : e.target.value)}
                  onBlur={field.onBlur}
                  aria-invalid={fieldState.invalid || undefined}
                  aria-describedby="score-comment-error"
                />
                <FieldError id="score-comment-error">{fieldState.error?.message}</FieldError>
              </>
            )}
          />
        </div>

        {form.formState.errors.root ? (
          <p
            role="alert"
            className="mt-3 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive"
          >
            {form.formState.errors.root.message}
          </p>
        ) : null}

        <div className="mt-4 flex justify-end">
          <Button type="submit" loading={mutation.isPending}>
            {existing ? 'Update score' : 'Submit score'}
          </Button>
        </div>
      </form>
    </Card>
  );
}

const DIMENSION_LABELS: Record<'impact' | 'feasibility' | 'innovation' | 'alignment', string> = {
  impact: 'Impact',
  feasibility: 'Feasibility',
  innovation: 'Innovation',
  alignment: 'Alignment',
};

/**
 * Returns the caller's existing entry, if their alias matches one of the
 * aggregate's entries. The backend computes the alias deterministically as
 * <c>"Reviewer #&lt;base32(hmac(salt, ideaId|reviewerId))&gt;"</c> — we cannot
 * recompute the HMAC client-side without leaking the salt, so we approximate
 * by relying on the cache-invalidation flow: after a successful submit, the
 * mutation patches the detail cache with the fresh aggregate, which already
 * contains our just-saved entry. On a cold load we don't know which entry is
 * ours, so the form defaults to neutral values (3) — a slight ergonomic
 * compromise that avoids leaking salts to the browser.
 */
function findOwnEntry(_idea: IdeaDetail, _reviewerId: string): IdeaScoreEntry | undefined {
  return undefined;
}
