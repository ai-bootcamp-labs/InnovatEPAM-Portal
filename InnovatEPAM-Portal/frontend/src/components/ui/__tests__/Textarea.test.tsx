import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Textarea } from '../Textarea';

describe('Textarea (T011)', () => {
  it('renders with an accessible name and accepts placeholder', () => {
    render(<Textarea aria-label="Description" placeholder="Describe your idea" />);
    const ta = screen.getByLabelText('Description');
    expect(ta.tagName).toBe('TEXTAREA');
    expect(ta).toHaveAttribute('placeholder', 'Describe your idea');
  });

  it('error state surfaces aria-invalid', () => {
    render(<Textarea aria-label="Description" invalid />);
    expect(screen.getByLabelText('Description')).toHaveAttribute('aria-invalid', 'true');
  });

  it('disabled blocks interaction', () => {
    render(<Textarea aria-label="Description" disabled />);
    expect(screen.getByLabelText('Description')).toBeDisabled();
  });
});
