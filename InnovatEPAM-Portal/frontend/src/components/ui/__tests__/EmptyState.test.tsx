import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EmptyState } from '../EmptyState';

describe('EmptyState (T017)', () => {
  it('renders a heading and optional description', () => {
    render(<EmptyState title="No ideas yet" description="Submit one to begin." />);
    expect(screen.getByRole('heading', { name: 'No ideas yet' })).toBeInTheDocument();
    expect(screen.getByText('Submit one to begin.')).toBeInTheDocument();
  });

  it('only renders the CTA when supplied', () => {
    const { rerender } = render(<EmptyState title="Empty" />);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();

    rerender(
      <EmptyState title="Empty" action={<button type="button">Add</button>} />,
    );
    expect(screen.getByRole('button', { name: 'Add' })).toBeInTheDocument();
  });

  it('announces itself as a status region', () => {
    render(<EmptyState title="Empty" />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });
});
