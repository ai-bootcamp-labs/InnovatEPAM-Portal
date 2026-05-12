import { useState } from 'react';
import { useAuth } from '@/features/auth/AuthProvider';
import type { IdeaDetail } from '@/features/ideas/api';
import { DecisionDialog } from '@/features/admin/DecisionDialog';
import type { DecisionAction } from '@/features/admin/api';

interface DecideControlsProps {
  idea: IdeaDetail;
  onDecisionRecorded?: (message: string) => void;
}

const TERMINAL_STATUSES = new Set(['Accepted', 'Rejected']);

/**
 * Renders Move-to-Under-Review / Accept / Reject buttons (T090). Visible only
 * to admins; disabled once the idea has reached a terminal status (FR-020).
 */
export function DecideControls({ idea, onDecisionRecorded }: DecideControlsProps): JSX.Element | null {
  const { user } = useAuth();
  const [openAction, setOpenAction] = useState<DecisionAction | null>(null);

  if (user?.role !== 'Admin') return null;

  const isTerminal = TERMINAL_STATUSES.has(idea.status);
  const disabledTitle = isTerminal ? 'Idea has reached a terminal status' : undefined;

  const buttons: { action: DecisionAction; label: string; variant: string }[] = [
    {
      action: 'MoveToUnderReview',
      label: 'Move to Under Review',
      variant: 'border border-border bg-background hover:bg-accent hover:text-accent-foreground',
    },
    {
      action: 'Accept',
      label: 'Accept',
      variant: 'bg-emerald-600 text-white hover:bg-emerald-600/90',
    },
    {
      action: 'Reject',
      label: 'Reject',
      variant: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
    },
  ];

  return (
    <div className="flex flex-wrap items-center gap-2">
      {buttons.map(({ action, label, variant }) => (
        <button
          key={action}
          type="button"
          onClick={() => setOpenAction(action)}
          disabled={isTerminal}
          title={disabledTitle}
          className={`rounded-md px-3 py-1.5 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50 ${variant}`}
        >
          {label}
        </button>
      ))}

      {openAction ? (
        <DecisionDialog
          open
          ideaId={idea.id}
          action={openAction}
          onClose={() => setOpenAction(null)}
          onSuccess={onDecisionRecorded}
        />
      ) : null}
    </div>
  );
}
