import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Select } from '../Select';

function options() {
  return (
    <>
      <option value="">All</option>
      <option value="process">Process</option>
      <option value="product">Product</option>
    </>
  );
}

describe('Select (T012)', () => {
  it('renders a native <select> with the given options', () => {
    render(
      <Select aria-label="Category" defaultValue="">
        {options()}
      </Select>,
    );
    const sel = screen.getByLabelText('Category') as HTMLSelectElement;
    expect(sel.tagName).toBe('SELECT');
    expect(sel.options).toHaveLength(3);
  });

  it('emits change events from keyboard / pointer interaction', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(
      <Select aria-label="Category" defaultValue="" onChange={onChange}>
        {options()}
      </Select>,
    );
    await user.selectOptions(screen.getByLabelText('Category'), 'product');
    expect(onChange).toHaveBeenCalled();
    expect((screen.getByLabelText('Category') as HTMLSelectElement).value).toBe(
      'product',
    );
  });

  it('error state surfaces aria-invalid', () => {
    render(
      <Select aria-label="Category" invalid defaultValue="">
        {options()}
      </Select>,
    );
    expect(screen.getByLabelText('Category')).toHaveAttribute('aria-invalid', 'true');
  });
});
