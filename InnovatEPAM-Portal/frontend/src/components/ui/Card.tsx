import { forwardRef, type HTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Adds a subtle hover treatment (list rows, navigable cards). */
  hoverable?: boolean;
}

/**
 * Surface primitive (008-ui-polish T016). Used by list rows, idea
 * detail body, form containers — anywhere we want the shared border
 * + radius + shadow + padding rhythm.
 */
export const Card = forwardRef<HTMLDivElement, CardProps>(function Card(
  { hoverable = false, className, ...props },
  ref,
) {
  return (
    <div
      ref={ref}
      data-hoverable={hoverable || undefined}
      className={cn(
        'rounded-lg border border-border bg-card text-card-foreground shadow-sm',
        hoverable &&
          'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard hover:border-foreground/20 hover:bg-accent/40',
        className,
      )}
      {...props}
    />
  );
});

export const CardHeader = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(
  function CardHeader({ className, ...props }, ref) {
    return (
      <div
        ref={ref}
        className={cn('flex flex-col gap-1.5 border-b border-border px-5 py-4', className)}
        {...props}
      />
    );
  },
);

export const CardBody = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(
  function CardBody({ className, ...props }, ref) {
    return <div ref={ref} className={cn('px-5 py-4', className)} {...props} />;
  },
);

export const CardFooter = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(
  function CardFooter({ className, ...props }, ref) {
    return (
      <div
        ref={ref}
        className={cn(
          'flex items-center justify-end gap-2 border-t border-border px-5 py-3',
          className,
        )}
        {...props}
      />
    );
  },
);
