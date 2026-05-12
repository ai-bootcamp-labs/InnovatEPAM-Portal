import { format, formatDistanceToNow, parseISO } from 'date-fns';

/**
 * Date helpers for the InnovatEPAM Portal (T041, R10). All input is ISO 8601
 * (the API contract). Output is locale-stable for the MVP — Phase 2 may
 * introduce per-user locale negotiation.
 */

function toDate(value: Date | string): Date {
  return value instanceof Date ? value : parseISO(value);
}

/** Calendar date suitable for list views. Example: `12 May 2026`. */
export function formatIdeaDate(value: Date | string): string {
  return format(toDate(value), 'd MMM yyyy');
}

/** Date + time suitable for detail/audit views. Example: `12 May 2026 14:32`. */
export function formatIdeaDateTime(value: Date | string): string {
  return format(toDate(value), 'd MMM yyyy HH:mm');
}

/** Relative phrase suitable for "submitted X ago" badges. */
export function formatRelative(value: Date | string): string {
  return formatDistanceToNow(toDate(value), { addSuffix: true });
}
