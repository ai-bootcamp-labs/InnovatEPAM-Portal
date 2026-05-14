import { forwardRef, type InputHTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';

const base =
  'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground ' +
  'placeholder:text-muted-foreground ' +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ' +
  'disabled:cursor-not-allowed disabled:opacity-60 ' +
  'aria-[invalid=true]:border-destructive aria-[invalid=true]:focus-visible:ring-destructive ' +
  'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  /** When true, applies `aria-invalid="true"` and the error border treatment. */
  invalid?: boolean;
}

/**
 * Shared text input primitive (008-ui-polish T010). Suitable for
 * `text`, `email`, `password`, `number`, `url` etc. — uses the shared
 * border, radius, padding, and focus ring across the portal.
 */
export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { type = 'text', invalid, className, ...props },
  ref,
) {
  return (
    <input
      ref={ref}
      type={type}
      aria-invalid={invalid || props['aria-invalid']}
      className={cn(base, className)}
      {...props}
    />
  );
});
