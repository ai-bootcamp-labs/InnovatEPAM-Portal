import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { LoadingSkeleton, Skeleton, Spinner } from '../LoadingSkeleton';

describe('LoadingSkeleton (T018)', () => {
  it('renders an aria-live status region with a screen-reader-only label', () => {
    render(<LoadingSkeleton rows={2} />);
    const status = screen.getByRole('status');
    expect(status).toHaveAttribute('aria-busy', 'true');
    expect(status).toHaveAttribute('aria-live', 'polite');
    expect(screen.getByText('Loading…')).toBeInTheDocument();
  });

  it('renders the requested number of skeleton rows', () => {
    const { container } = render(<LoadingSkeleton rows={4} />);
    // The aria-hidden Skeleton siblings live inside the status region.
    const rows = container.querySelectorAll('[aria-hidden="true"]');
    expect(rows).toHaveLength(4);
  });
});

describe('Skeleton (T018)', () => {
  it('is hidden from assistive tech', () => {
    const { container } = render(<Skeleton data-testid="bar" />);
    expect(container.firstChild).toHaveAttribute('aria-hidden', 'true');
  });
});

describe('Spinner (T018)', () => {
  it('exposes an accessible name for assistive tech', () => {
    render(<Spinner />);
    expect(screen.getByRole('img', { name: 'Loading' })).toBeInTheDocument();
  });
});
