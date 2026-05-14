import { useState } from 'react';
import { useAuth } from '@/features/auth/AuthProvider';
import type { IdeaDetail } from '@/features/ideas/api';
import { DecisionDialog } from '@/features/admin/DecisionDialog';
import type { DecisionAction } from '@/features/admin/api';
import { Button, type ButtonVariant } from '@/components/ui';

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

  const buttons: { action: DecisionAction; label: string; variant: ButtonVariant }[] = [
    { action: 'MoveToUnderReview', label: 'Move to Under Review', variant: 'secondary' },
    { action: 'Accept', label: 'Accept', variant: 'primary' },
    { action: 'Reject', label: 'Reject', variant: 'destructive' },
  ];

  return (
    <div className="flex flex-wrap items-center gap-2">
      {buttons.map(({ action, label, variant }) => (
        <Button
          key={action}
          variant={variant}
          size="sm"
          onClick={() => setOpenAction(action)}
          disabled={isTerminal}
          title={disabledTitle}
        >
          {label}
        </Button>
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
