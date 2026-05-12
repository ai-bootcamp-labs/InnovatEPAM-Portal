import type { ReactNode } from 'react';
import { cn } from '@/lib/utils';

/** Responsive max-width container for page bodies (T043). */
export function MainContainer({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}): JSX.Element {
  return <main className={cn('mx-auto w-full max-w-6xl px-4 py-6', className)}>{children}</main>;
}
