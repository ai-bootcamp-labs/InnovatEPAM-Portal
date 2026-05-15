import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SubmitterLabel } from '@/features/ideas/SubmitterLabel';

// Phase 6 — render-only assertions (FR-001 / FR-003 mapping).
// The component is dumb: it has no state, no fetching, no role checks.
// Its only job is "given the props the backend already redacted, render the
// right label" — so we cover the three render branches and that's it.
describe('SubmitterLabel', () => {
  it('renders the real submitter name when present', () => {
    render(<SubmitterLabel name="Jane Doe" alias={null} />);
    expect(screen.getByText('Jane Doe')).toBeInTheDocument();
    expect(screen.queryByTestId('submitter-alias')).toBeNull();
  });

  it('renders the alias when the name is hidden', () => {
    render(<SubmitterLabel name={null} alias="Submitter #ABCD" />);
    const alias = screen.getByTestId('submitter-alias');
    expect(alias).toHaveTextContent('Submitter #ABCD');
    expect(alias).toHaveAttribute('title', expect.stringMatching(/blind review/i));
  });

  it('falls back to a placeholder when both are missing', () => {
    render(<SubmitterLabel name={null} alias={null} />);
    expect(screen.getByText(/unknown submitter/i)).toBeInTheDocument();
  });
});
