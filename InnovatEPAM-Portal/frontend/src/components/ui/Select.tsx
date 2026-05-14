import { forwardRef, type SelectHTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';

const base =
  'flex h-10 w-full appearance-none rounded-md border border-input bg-background px-3 py-2 pr-9 text-sm text-foreground ' +
  'bg-no-repeat bg-[right_0.65rem_center] bg-[length:1rem_1rem] ' +
  // inline SVG caret (foreground colour) so we don't depend on an icon font
  "bg-[url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M6 8l4 4 4-4'/></svg>\")] " +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ' +
  'disabled:cursor-not-allowed disabled:opacity-60 ' +
  'aria-[invalid=true]:border-destructive aria-[invalid=true]:focus-visible:ring-destructive ' +
  'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard';

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean;
}

/**
 * Styled native `<select>` primitive (008-ui-polish T012). Plan NFR-001
 * defaults to no headless library — native `<select>` is the simplest
 * accessible choice for the portal's short option lists.
 */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { invalid, className, children, ...props },
  ref,
) {
  return (
    <select
      ref={ref}
      aria-invalid={invalid || props['aria-invalid']}
      className={cn(base, className)}
      {...props}
    >
      {children}
    </select>
  );
});
