import { cn } from '@/lib/ui/cn';

/**
 * Phase 6 — render the submitter as either a real name or the blind-review
 * alias (FR-001/FR-003). The component decides purely from the props the
 * backend already provided: at most one of `name` / `alias` is non-null.
 * When neither is present we fall back to a generic placeholder (e.g. a
 * deleted user).
 */
export interface SubmitterLabelProps {
  name: string | null | undefined;
  alias: string | null | undefined;
  /** Optional className on the wrapping span. */
  className?: string;
}

export function SubmitterLabel({ name, alias, className }: SubmitterLabelProps): JSX.Element {
  if (name) {
    return <span className={className}>{name}</span>;
  }
  if (alias) {
    return (
      <span
        className={cn('font-mono text-foreground/90', className)}
        data-testid="submitter-alias"
        aria-label={`Anonymous submitter ${alias}`}
        title="Identity hidden during blind review"
      >
        {alias}
      </span>
    );
  }
  return <span className={cn('italic text-muted-foreground', className)}>Unknown submitter</span>;
}
