import { cn } from '@/lib/utils';

/** Idea status mirror of the backend `IdeaStatus` enum. */
export type IdeaStatus = 'Submitted' | 'UnderReview' | 'Accepted' | 'Rejected';

const styles: Record<IdeaStatus, { label: string; className: string }> = {
  Submitted: {
    label: 'Submitted',
    className: 'bg-secondary text-secondary-foreground border-secondary',
  },
  UnderReview: {
    label: 'Under review',
    className: 'bg-accent text-accent-foreground border-accent',
  },
  Accepted: {
    label: 'Accepted',
    className: 'bg-primary/10 text-primary border-primary/20',
  },
  Rejected: {
    label: 'Rejected',
    className: 'bg-destructive/10 text-destructive border-destructive/20',
  },
};

/** Renders a coloured badge for any `IdeaStatus` value (T044). */
export function StatusBadge({ status, className }: { status: IdeaStatus; className?: string }): JSX.Element {
  const { label, className: variant } = styles[status];
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium',
        variant,
        className,
      )}
    >
      {label}
    </span>
  );
}
