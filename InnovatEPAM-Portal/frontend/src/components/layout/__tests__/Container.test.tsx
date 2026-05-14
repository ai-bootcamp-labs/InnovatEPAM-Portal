import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Container } from '../Container';

describe('Container (T020)', () => {
  it('renders a div by default with the shared max-width utilities', () => {
    render(
      <Container data-testid="c">
        <span>child</span>
      </Container>,
    );
    const el = screen.getByTestId('c');
    expect(el.tagName).toBe('DIV');
    expect(el.className).toContain('max-w-6xl');
    expect(el.className).toContain('mx-auto');
    expect(screen.getByText('child')).toBeInTheDocument();
  });

  it('renders as a custom element when `as` is passed', () => {
    render(
      <Container as="main" data-testid="c">
        body
      </Container>,
    );
    expect(screen.getByTestId('c').tagName).toBe('MAIN');
  });
});
