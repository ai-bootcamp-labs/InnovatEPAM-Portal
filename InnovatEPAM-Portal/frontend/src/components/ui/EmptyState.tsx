import type { ReactNode, SVGProps } from 'react';
import { cn } from '@/lib/ui/cn';

function DefaultIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" {...props}>
      <rect
        x="3.5"
        y="5.5"
        width="17"
        height="13"
        rx="2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M3.5 9.5h17M8 13h8"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

export interface EmptyStateProps {
  title: string;
  description?: ReactNode;
  /** Optional call-to-action area (e.g. a `<Button>`). */
  action?: ReactNode;
  /** Optional custom icon. Defaults to a neutral document/inbox glyph. */
  icon?: ReactNode;
  className?: string;
}

/**
 * Shared empty-state primitive (008-ui-polish T017).
 *
 * Use on any page whose primary list is empty (idea list, admin queue,
 * notifications). Renders icon + heading + supporting line + optional CTA.
 */
export function EmptyState({
  title,
  description,
  action,
  icon,
  className,
}: EmptyStateProps): JSX.Element {
  return (
    <div
      role="status"
      className={cn(
        'flex flex-col items-center justify-center rounded-lg border border-dashed border-border bg-card px-6 py-10 text-center',
        className,
      )}
    >
      <span aria-hidden="true" className="mb-3 text-muted-foreground">
        {icon ?? <DefaultIcon className="h-8 w-8" />}
      </span>
      <h2 className="text-lg font-medium text-foreground">{title}</h2>
      {description ? (
        <p className="mt-1 max-w-sm text-sm text-muted-foreground">{description}</p>
      ) : null}
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  );
}
