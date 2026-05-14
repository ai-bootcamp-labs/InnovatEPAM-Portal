/**
 * Class-name helper for the UI primitives layer (008-ui-polish T008).
 *
 * Re-exports the existing shadcn-style `cn` from `@/lib/utils` so the
 * UI layer can import from a single, plan-blessed path
 * (`@/lib/ui/cn`) without duplicating the implementation.
 *
 * Semantics: clsx for conditional composition, then tailwind-merge so
 * later utilities win conflicts (e.g. `px-2 px-4` → `px-4`).
 */
export { cn } from '@/lib/utils';
export type { ClassValue } from 'clsx';
