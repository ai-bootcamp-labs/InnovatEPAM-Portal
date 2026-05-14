import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card, CardBody, CardFooter, CardHeader } from '../Card';

describe('Card (T016)', () => {
  it('forwards arbitrary props and renders children', () => {
    render(
      <Card data-testid="surface">
        <span>Hello</span>
      </Card>,
    );
    expect(screen.getByTestId('surface')).toHaveTextContent('Hello');
  });

  it('opts into a hoverable variant via data attribute', () => {
    render(<Card hoverable data-testid="surface" />);
    expect(screen.getByTestId('surface')).toHaveAttribute('data-hoverable', 'true');
  });

  it('Header / Body / Footer render and compose', () => {
    render(
      <Card data-testid="surface">
        <CardHeader>
          <h2>Title</h2>
        </CardHeader>
        <CardBody>body</CardBody>
        <CardFooter>footer</CardFooter>
      </Card>,
    );
    expect(screen.getByRole('heading', { name: 'Title' })).toBeInTheDocument();
    expect(screen.getByText('body')).toBeInTheDocument();
    expect(screen.getByText('footer')).toBeInTheDocument();
  });
});
