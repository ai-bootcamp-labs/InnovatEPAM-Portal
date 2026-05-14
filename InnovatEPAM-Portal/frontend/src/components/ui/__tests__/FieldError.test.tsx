import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FieldError } from '../FieldError';

describe('FieldError (T014)', () => {
  it('renders nothing when no message is supplied', () => {
    const { container } = render(<FieldError />);
    expect(container).toBeEmptyDOMElement();
  });

  it('renders the message inside an alert with aria-live polite', () => {
    render(<FieldError id="title-error">Title is required</FieldError>);
    const alert = screen.getByRole('alert');
    expect(alert).toHaveAttribute('id', 'title-error');
    expect(alert).toHaveAttribute('aria-live', 'polite');
    expect(alert).toHaveTextContent('Title is required');
  });

  it('icon is hidden from assistive tech (icon + text, not colour alone)', () => {
    const { container } = render(<FieldError>Bad</FieldError>);
    const icon = container.querySelector('svg');
    expect(icon).not.toBeNull();
    expect(icon).toHaveAttribute('aria-hidden', 'true');
  });
});
