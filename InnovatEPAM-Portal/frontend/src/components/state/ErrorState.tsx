import { Button } from '@/components/ui';

/** Error placeholder with optional retry callback (T044, T047). */
export function ErrorState({
  title = 'Something went wrong',
  message,
  onRetry,
}: {
  title?: string;
  message?: string;
  onRetry?: () => void;
}): JSX.Element {
  return (
    <div className="rounded-lg border border-destructive/40 bg-destructive/5 p-6" role="alert">
      <h2 className="text-base font-semibold text-destructive">{title}</h2>
      {message ? <p className="mt-1 text-sm text-foreground">{message}</p> : null}
      {onRetry ? (
        <div className="mt-4">
          <Button variant="secondary" size="sm" onClick={onRetry}>
            Try again
          </Button>
        </div>
      ) : null}
    </div>
  );
}
