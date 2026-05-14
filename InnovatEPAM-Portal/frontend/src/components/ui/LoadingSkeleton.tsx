import type { HTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';

/**
 * `Skeleton` — neutral placeholder rectangle/line used while async data
 * is in flight (008-ui-polish T018). Pure visual; carries no semantics
 * so it's `aria-hidden`. Pair with `LoadingSkeleton` (below) which adds
 * an `aria-live` region.
 */
export function Skeleton({
  className,
  ...props
}: HTMLAttributes<HTMLDivElement>): JSX.Element {
  return (
    <div
      aria-hidden="true"
      className={cn(
        'block rounded-md bg-muted motion-safe:animate-pulse',
        className,
      )}
      {...props}
    />
  );
}

/**
 * `LoadingSkeleton` — accessible wrapper that renders one or more
 * `<Skeleton />` rows with a single screen-reader-only "Loading…" label
 * (T018). Use this on lists and detail pages instead of bare text.
 */
export function LoadingSkeleton({
  rows = 3,
  className,
  rowClassName,
  label = 'Loading…',
}: {
  rows?: number;
  className?: string;
  rowClassName?: string;
  label?: string;
}): JSX.Element {
  return (
    <div
      role="status"
      aria-live="polite"
      aria-busy="true"
      className={cn('space-y-3', className)}
    >
      <span className="sr-only">{label}</span>
      {Array.from({ length: rows }).map((_, idx) => (
        <Skeleton key={idx} className={cn('h-12 w-full', rowClassName)} />
      ))}
    </div>
  );
}

/**
 * `Spinner` — small inline spinner used inside `Button` when
 * `loading` is true (T009/T018). Exposes an accessible name so
 * assistive tech announces it.
 */
export function Spinner({
  className,
  label = 'Loading',
}: {
  className?: string;
  label?: string;
}): JSX.Element {
  return (
    <svg
      role="img"
      aria-label={label}
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={cn('h-4 w-4 motion-safe:animate-spin', className)}
    >
      <circle
        cx="12"
        cy="12"
        r="9"
        stroke="currentColor"
        strokeOpacity="0.25"
        strokeWidth="3"
      />
      <path
        d="M21 12a9 9 0 0 0-9-9"
        stroke="currentColor"
        strokeWidth="3"
        strokeLinecap="round"
      />
    </svg>
  );
}
