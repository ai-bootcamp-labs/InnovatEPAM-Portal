import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '@/lib/ui/cn';

export interface FieldErrorProps extends HTMLAttributes<HTMLParagraphElement> {
  /** The error message. When falsy, nothing is rendered. */
  children?: ReactNode;
}

/**
 * Inline form error primitive (008-ui-polish T014).
 *
 * Renders a leading icon plus the message inside an `aria-live` region.
 * State is communicated by **icon + text**, not colour alone (FR-009).
 * Wire to its field at the call site via `aria-describedby={id}` so
 * screen-reader users get the error announced.
 */
export function FieldError({
  id,
  className,
  children,
  ...props
}: FieldErrorProps): JSX.Element | null {
  if (!children) return null;
  return (
    <p
      id={id}
      role="alert"
      aria-live="polite"
      className={cn(
        'mt-1 flex items-start gap-1.5 text-sm text-destructive',
        className,
      )}
      {...props}
    >
      <svg
        aria-hidden="true"
        viewBox="0 0 20 20"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className="mt-[2px] h-4 w-4 shrink-0"
      >
        <circle cx="10" cy="10" r="8" stroke="currentColor" strokeWidth="1.5" />
        <path
          d="M10 6.5v4"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
        />
        <circle cx="10" cy="13.5" r="0.9" fill="currentColor" />
      </svg>
      <span>{children}</span>
    </p>
  );
}
