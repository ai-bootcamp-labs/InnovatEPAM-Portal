import { forwardRef, type TextareaHTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';

const base =
  'flex w-full min-h-[6rem] rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground ' +
  'placeholder:text-muted-foreground ' +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ' +
  'disabled:cursor-not-allowed disabled:opacity-60 ' +
  'aria-[invalid=true]:border-destructive aria-[invalid=true]:focus-visible:ring-destructive ' +
  'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard';

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean;
}

/** Multi-line variant of `Input` (008-ui-polish T011). Same border /
 * radius / focus / error treatment, sized for multi-line content. */
export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea(
  { invalid, className, ...props },
  ref,
) {
  return (
    <textarea
      ref={ref}
      aria-invalid={invalid || props['aria-invalid']}
      className={cn(base, className)}
      {...props}
    />
  );
});
