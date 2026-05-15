import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ScoreInput } from '@/components/ui/ScoreInput';

// Phase 7 — keyboard-driven score input (FR-008/FR-011, T038).
// We exercise both pointer and keyboard paths to confirm the roving-tabindex
// + arrow-key contract the WAI-ARIA Authoring Practices require for radio
// groups, plus pointer click for the obvious happy path.
describe('ScoreInput', () => {
  it('emits the chosen rating on click', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<ScoreInput name="impact" label="Impact" value={null} onChange={onChange} />);

    await user.click(screen.getByRole('radio', { name: '4 out of 5' }));
    expect(onChange).toHaveBeenCalledWith(4);
  });

  it('walks through values with arrow keys', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<ScoreInput name="impact" label="Impact" value={3} onChange={onChange} />);

    const current = screen.getByRole('radio', { name: '3 out of 5' });
    current.focus();
    await user.keyboard('{ArrowRight}');
    expect(onChange).toHaveBeenLastCalledWith(4);

    await user.keyboard('{End}');
    expect(onChange).toHaveBeenLastCalledWith(5);

    await user.keyboard('{Home}');
    expect(onChange).toHaveBeenLastCalledWith(1);
  });

  it('exposes the selected option via aria-checked', () => {
    render(<ScoreInput name="impact" label="Impact" value={2} onChange={() => undefined} />);
    expect(screen.getByRole('radio', { name: '2 out of 5' })).toHaveAttribute('aria-checked', 'true');
  });
});
