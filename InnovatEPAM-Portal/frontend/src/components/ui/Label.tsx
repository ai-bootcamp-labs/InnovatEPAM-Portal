import { forwardRef, type LabelHTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';

export interface LabelProps extends LabelHTMLAttributes<HTMLLabelElement> {
  /** When true, renders a visual `*` indicator. Pair with `aria-required`
   * on the field itself so assistive tech announces the requirement. */
  required?: boolean;
}

/** Block-level form label primitive (008-ui-polish T013). */
export const Label = forwardRef<HTMLLabelElement, LabelProps>(function Label(
  { required = false, className, children, ...props },
  ref,
) {
  return (
    <label
      ref={ref}
      className={cn(
        'mb-1 block text-sm font-medium text-foreground',
        'peer-disabled:cursor-not-allowed peer-disabled:opacity-60',
        className,
      )}
      {...props}
    >
      {children}
      {required ? (
        <span
          aria-hidden="true"
          className="ml-0.5 text-destructive"
          data-testid="required-indicator"
        >
          *
        </span>
      ) : null}
    </label>
  );
});
