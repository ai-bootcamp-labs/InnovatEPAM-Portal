import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatusBadge, type IdeaStatus } from '../StatusBadge';

const cases: { status: IdeaStatus; label: string }[] = [
  { status: 'Submitted', label: 'Submitted' },
  { status: 'UnderReview', label: 'Under review' },
  { status: 'Accepted', label: 'Accepted' },
  { status: 'Rejected', label: 'Rejected' },
];

describe('StatusBadge (T015)', () => {
  it.each(cases)('renders an accessible label for $status', ({ status, label }) => {
    render(<StatusBadge status={status} />);
    const badge = screen.getByRole('status', { name: `Status: ${label}` });
    expect(badge).toHaveAttribute('data-status', status);
  });

  it('every status renders a visible icon as redundant cue (not colour alone)', () => {
    cases.forEach(({ status }) => {
      const { container, unmount } = render(<StatusBadge status={status} />);
      const icon = container.querySelector('svg');
      expect(icon, `expected icon for ${status}`).not.toBeNull();
      expect(icon).toHaveAttribute('aria-hidden', 'true');
      unmount();
    });
  });

  it('same status produces the same accessible name wherever rendered (SC-003)', () => {
    const { unmount } = render(<StatusBadge status="Accepted" />);
    const first = screen.getByRole('status').getAttribute('aria-label');
    unmount();
    render(<StatusBadge status="Accepted" />);
    expect(screen.getByRole('status').getAttribute('aria-label')).toBe(first);
  });
});
