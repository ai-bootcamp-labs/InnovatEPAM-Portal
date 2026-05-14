import type { ElementType, HTMLAttributes, ReactNode } from 'react';
import { cn } from '@/lib/ui/cn';

export interface ContainerProps extends HTMLAttributes<HTMLElement> {
  /** Element to render. Defaults to `div`. `AppShell` passes `main`. */
  as?: ElementType;
  children?: ReactNode;
}

/**
 * Shared max-width / horizontal padding wrapper (008-ui-polish T020).
 *
 * Every page in the portal is wrapped in this so horizontal rhythm is
 * identical across routes (FR-003).
 */
export function Container({
  as: Component = 'div',
  className,
  children,
  ...props
}: ContainerProps): JSX.Element {
  return (
    <Component
      className={cn('mx-auto w-full max-w-6xl px-4 sm:px-6 lg:px-8', className)}
      {...props}
    >
      {children}
    </Component>
  );
}
