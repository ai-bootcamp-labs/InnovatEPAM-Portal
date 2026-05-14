import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Label } from '../Label';

describe('Label (T013)', () => {
  it('renders a block label with the given htmlFor', () => {
    render(<Label htmlFor="title">Title</Label>);
    const label = screen.getByText('Title');
    expect(label.tagName).toBe('LABEL');
    expect(label).toHaveAttribute('for', 'title');
  });

  it('shows the required indicator only when required', () => {
    const { rerender } = render(<Label htmlFor="title">Title</Label>);
    expect(screen.queryByTestId('required-indicator')).not.toBeInTheDocument();

    rerender(
      <Label htmlFor="title" required>
        Title
      </Label>,
    );
    expect(screen.getByTestId('required-indicator')).toBeInTheDocument();
  });
});
