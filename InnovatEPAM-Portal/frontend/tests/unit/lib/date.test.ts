import { describe, expect, it } from 'vitest';
import { formatIdeaDate, formatIdeaDateTime, formatRelative } from '@/lib/date';

describe('date helpers (T041)', () => {
  const iso = '2026-05-12T14:32:00Z';

  it('formats a calendar date', () => {
    expect(formatIdeaDate(iso)).toMatch(/2026/);
    expect(formatIdeaDate(iso)).toMatch(/May/);
  });

  it('formats a date + time', () => {
    expect(formatIdeaDateTime(iso)).toMatch(/2026/);
    expect(formatIdeaDateTime(iso)).toMatch(/:/);
  });

  it('formats a relative phrase', () => {
    expect(formatRelative(iso)).toMatch(/ago|in /);
  });
});
