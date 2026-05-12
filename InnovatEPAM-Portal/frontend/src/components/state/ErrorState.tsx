/** Error placeholder with optional retry callback (T044). */
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
        <button
          type="button"
          onClick={onRetry}
          className="mt-4 rounded-md border border-border bg-background px-3 py-1 text-sm hover:bg-accent hover:text-accent-foreground"
        >
          Try again
        </button>
      ) : null}
    </div>
  );
}
