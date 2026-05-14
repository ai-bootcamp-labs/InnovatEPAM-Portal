import { useEffect, useId, useRef, useState } from 'react';
import { useDecisionMutation, type DecisionAction } from '@/features/admin/api';
import { ApiError } from '@/lib/api/client';
import { Button, FieldError, Label, Textarea } from '@/components/ui';

interface DecisionDialogProps {
  open: boolean;
  ideaId: string;
  action: DecisionAction;
  onClose: () => void;
  onSuccess?: (message: string) => void;
}

const ACTION_COPY: Record<DecisionAction, { title: string; submit: string; description: string; commentRequired: boolean }> = {
  MoveToUnderReview: {
    title: 'Move to Under Review',
    submit: 'Move to Under Review',
    description: 'Mark this idea as actively being evaluated. A comment is optional.',
    commentRequired: false,
  },
  Accept: {
    title: 'Accept idea',
    submit: 'Accept',
    description: 'Accepting is final. Provide a comment explaining the decision.',
    commentRequired: true,
  },
  Reject: {
    title: 'Reject idea',
    submit: 'Reject',
    description: 'Rejecting is final. Provide a comment explaining the decision.',
    commentRequired: true,
  },
};

/**
 * Modal that captures a decision (T089). Implemented as a focusable native
 * dialog wrapper so we don't depend on a third-party dialog library; behaviour
 * matches the shadcn/ui dialog spec referenced in the task.
 */
export function DecisionDialog({ open, ideaId, action, onClose, onSuccess }: DecisionDialogProps): JSX.Element | null {
  const copy = ACTION_COPY[action];
  const [comment, setComment] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [conflict, setConflict] = useState<string | null>(null);
  const mutation = useDecisionMutation();
  const firstFieldRef = useRef<HTMLTextAreaElement | null>(null);
  const dialogId = useId();
  const commentId = `${dialogId}-comment`;
  const commentErrId = `${commentId}-error`;

  useEffect(() => {
    if (open) {
      setComment('');
      setError(null);
      setConflict(null);
      // Defer focus until the dialog is in the DOM.
      requestAnimationFrame(() => firstFieldRef.current?.focus());
    }
  }, [open, action]);

  if (!open) return null;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setConflict(null);
    const trimmed = comment.trim();
    if (copy.commentRequired && trimmed.length === 0) {
      setError('Comment is required for Accept/Reject.');
      return;
    }
    try {
      await mutation.mutateAsync({ ideaId, action, comment: trimmed.length > 0 ? trimmed : undefined });
      onSuccess?.('Decision recorded');
      onClose();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setConflict(err.problem.detail ?? 'Idea was modified by another user. Please refresh and try again.');
        return;
      }
      const message =
        err instanceof ApiError
          ? err.problem.detail ?? err.problem.title ?? 'Could not record decision.'
          : 'Could not record decision.';
      setError(message);
    }
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="decision-dialog-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4"
    >
      {/* ui-polish-exception: invisible click-catcher used to dismiss the dialog;
          a Button primitive would render visible styling that we explicitly don't want. */}
      <button
        type="button"
        aria-label="Close dialog"
        onClick={onClose}
        className="absolute inset-0 h-full w-full cursor-default bg-transparent"
      />
      <div className="relative z-10 w-full max-w-md rounded-lg bg-card p-6 shadow-lg">
        <h2 id="decision-dialog-title" className="text-lg font-semibold text-foreground">
          {copy.title}
        </h2>
        <p className="mt-1 text-sm text-muted-foreground">{copy.description}</p>

        <form className="mt-4 space-y-3" onSubmit={handleSubmit} noValidate>
          <div className="space-y-1">
            <Label htmlFor={commentId} required={copy.commentRequired}>
              Comment
              {copy.commentRequired ? null : (
                <span className="ml-1 font-normal text-muted-foreground">(optional)</span>
              )}
            </Label>
            <Textarea
              id={commentId}
              ref={firstFieldRef}
              rows={5}
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              maxLength={2000}
              required={copy.commentRequired}
              aria-required={copy.commentRequired || undefined}
              aria-describedby={error ? commentErrId : undefined}
              invalid={Boolean(error)}
              disabled={mutation.isPending}
            />
            <FieldError id={commentErrId}>{error}</FieldError>
          </div>

          {conflict ? (
            <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {conflict}
            </p>
          ) : null}

          <div className="mt-4 flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={onClose}
              disabled={mutation.isPending}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant={action === 'Reject' ? 'destructive' : 'primary'}
              size="sm"
              loading={mutation.isPending}
              disabled={mutation.isPending}
            >
              {mutation.isPending ? 'Recording…' : copy.submit}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
