import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { cn } from '@/lib/ui/cn';
import { Spinner } from './LoadingSkeleton';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'destructive';
export type ButtonSize = 'sm' | 'md';

const base =
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md font-medium ' +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ' +
  'disabled:cursor-not-allowed disabled:opacity-60 ' +
  'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard';

const variants: Record<ButtonVariant, string> = {
  primary:
    'bg-primary text-primary-foreground hover:bg-primary/90 active:bg-primary/80',
  secondary:
    'bg-secondary text-secondary-foreground border border-border hover:bg-accent hover:text-accent-foreground',
  ghost: 'bg-transparent text-foreground hover:bg-accent hover:text-accent-foreground',
  destructive:
    'bg-destructive text-destructive-foreground hover:bg-destructive/90 active:bg-destructive/80',
};

const sizes: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-sm',
  md: 'h-10 px-4 text-sm',
};

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  /** When true, shows an inline spinner, sets `aria-busy`, and disables the button. */
  loading?: boolean;
}

/**
 * Shared button primitive (008-ui-polish T009).
 *
 * - Variants: `primary` | `secondary` | `ghost` | `destructive`
 * - Sizes: `sm` | `md`
 * - `loading` → disables interaction, shows `Spinner`, sets `aria-busy="true"`
 * - All transitions are `motion-safe:` so reduced-motion users get the
 *   default static look (FR-011).
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    type = 'button',
    variant = 'primary',
    size = 'md',
    loading = false,
    disabled,
    className,
    children,
    ...props
  },
  ref,
) {
  const isDisabled = disabled || loading;
  return (
    <button
      ref={ref}
      type={type}
      disabled={isDisabled}
      aria-busy={loading || undefined}
      data-variant={variant}
      data-size={size}
      data-loading={loading || undefined}
      className={cn(base, variants[variant], sizes[size], className)}
      {...props}
    >
      {loading ? <Spinner aria-hidden="true" /> : null}
      <span>{children}</span>
    </button>
  );
});
