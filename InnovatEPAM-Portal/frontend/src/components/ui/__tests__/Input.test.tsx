import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Input } from '../Input';

describe('Input (T010)', () => {
  it('renders with the requested type and forwards aria-label', () => {
    render(<Input type="email" aria-label="Email" placeholder="you@example.com" />);
    const input = screen.getByLabelText('Email');
    expect(input).toHaveAttribute('type', 'email');
    expect(input).toHaveAttribute('placeholder', 'you@example.com');
  });

  it('error state surfaces aria-invalid', () => {
    render(<Input aria-label="Title" invalid />);
    expect(screen.getByLabelText('Title')).toHaveAttribute('aria-invalid', 'true');
  });

  it('disabled removes the element from the tab order', () => {
    render(<Input aria-label="Title" disabled />);
    const input = screen.getByLabelText('Title');
    expect(input).toBeDisabled();
  });
});
