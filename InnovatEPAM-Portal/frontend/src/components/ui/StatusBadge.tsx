import type { SVGProps } from 'react';
import { cn } from '@/lib/ui/cn';

/** Backend `IdeaStatus` enum mirror. */
export type IdeaStatus = 'Submitted' | 'UnderReview' | 'Accepted' | 'Rejected';

type StatusMeta = {
  label: string;
  className: string;
  Icon: (props: SVGProps<SVGSVGElement>) => JSX.Element;
};

function InboxIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" fill="none" {...props}>
      <path
        d="M3 12h4l1 2h4l1-2h4M3 12V5h14v7M3 12v3a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function ClockIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" fill="none" {...props}>
      <circle cx="10" cy="10" r="7" stroke="currentColor" strokeWidth="1.5" />
      <path d="M10 6v4l2.5 1.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function CheckIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" fill="none" {...props}>
      <path
        d="M5 10.5l3 3 7-7"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function CrossIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" fill="none" {...props}>
      <path
        d="M6 6l8 8M14 6l-8 8"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
    </svg>
  );
}

const meta: Record<IdeaStatus, StatusMeta> = {
  Submitted: {
    label: 'Submitted',
    className: 'bg-status-submitted text-status-submitted-foreground',
    Icon: InboxIcon,
  },
  UnderReview: {
    label: 'Under review',
    className: 'bg-status-under-review text-status-under-review-foreground',
    Icon: ClockIcon,
  },
  Accepted: {
    label: 'Accepted',
    className: 'bg-status-accepted text-status-accepted-foreground',
    Icon: CheckIcon,
  },
  Rejected: {
    label: 'Rejected',
    className: 'bg-status-rejected text-status-rejected-foreground',
    Icon: CrossIcon,
  },
};

export interface StatusBadgeProps {
  status: IdeaStatus;
  className?: string;
}

/**
 * Single source of truth for idea-status presentation
 * (008-ui-polish T015 / FR-006 / SC-003).
 *
 * Each status renders an **icon + label** so meaning is never
 * conveyed by colour alone (FR-009 / spec edge case).
 */
export function StatusBadge({ status, className }: StatusBadgeProps): JSX.Element {
  const { label, className: variant, Icon } = meta[status];
  return (
    <span
      role="status"
      aria-label={`Status: ${label}`}
      data-status={status}
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
        variant,
        className,
      )}
    >
      <Icon aria-hidden="true" className="h-3.5 w-3.5" />
      <span>{label}</span>
    </span>
  );
}
