import { cn } from '@/lib/utils';

/** Skeleton shimmer for asynchronous lists/detail pages (T044). */
export function LoadingState({
  rows = 3,
  className,
}: {
  rows?: number;
  className?: string;
}): JSX.Element {
  return (
    <div className={cn('space-y-3', className)} role="status" aria-live="polite" aria-busy="true">
      <span className="sr-only">Loading…</span>
      {Array.from({ length: rows }).map((_, idx) => (
        <div key={idx} className="h-12 animate-pulse rounded-md bg-muted" />
      ))}
    </div>
  );
}
