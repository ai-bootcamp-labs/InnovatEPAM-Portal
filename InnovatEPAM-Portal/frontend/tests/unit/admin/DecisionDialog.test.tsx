import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DecisionDialog } from '@/features/admin/DecisionDialog';

const mutateAsync = vi.fn();

vi.mock('@/features/admin/api', () => ({
  useDecisionMutation: () => ({ mutateAsync, isPending: false }),
}));

function renderDialog(props: Partial<React.ComponentProps<typeof DecisionDialog>> = {}) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const onClose = vi.fn();
  const onSuccess = vi.fn();
  const utils = render(
    <QueryClientProvider client={qc}>
      <DecisionDialog
        open
        ideaId="idea-1"
        action="Accept"
        onClose={onClose}
        onSuccess={onSuccess}
        {...props}
      />
    </QueryClientProvider>,
  );
  return { ...utils, onClose, onSuccess };
}

describe('DecisionDialog (T080)', () => {
  beforeEach(() => {
    mutateAsync.mockReset();
  });

  it('rejects an empty comment for Accept', async () => {
    const user = userEvent.setup();
    renderDialog({ action: 'Accept' });
    await user.click(screen.getByRole('button', { name: /^accept$/i }));
    expect(await screen.findByText(/comment is required/i)).toBeInTheDocument();
    expect(mutateAsync).not.toHaveBeenCalled();
  });

  it('rejects an empty comment for Reject', async () => {
    const user = userEvent.setup();
    renderDialog({ action: 'Reject' });
    await user.click(screen.getByRole('button', { name: /^reject$/i }));
    expect(await screen.findByText(/comment is required/i)).toBeInTheDocument();
    expect(mutateAsync).not.toHaveBeenCalled();
  });

  it('submits Move to Under Review without a comment', async () => {
    mutateAsync.mockResolvedValue({ id: 'idea-1' });
    const user = userEvent.setup();
    const { onClose, onSuccess } = renderDialog({ action: 'MoveToUnderReview' });
    await user.click(screen.getByRole('button', { name: /move to under review/i }));
    await waitFor(() => expect(mutateAsync).toHaveBeenCalledTimes(1));
    expect(mutateAsync).toHaveBeenCalledWith({
      ideaId: 'idea-1',
      action: 'MoveToUnderReview',
      comment: undefined,
    });
    expect(onSuccess).toHaveBeenCalledWith('Decision recorded');
    expect(onClose).toHaveBeenCalled();
  });

  it('submits Accept with a comment', async () => {
    mutateAsync.mockResolvedValue({ id: 'idea-1' });
    const user = userEvent.setup();
    const { onClose } = renderDialog({ action: 'Accept' });
    await user.type(screen.getByLabelText(/comment/i), 'Great proposal');
    await user.click(screen.getByRole('button', { name: /^accept$/i }));
    await waitFor(() => expect(mutateAsync).toHaveBeenCalledTimes(1));
    expect(mutateAsync).toHaveBeenCalledWith({
      ideaId: 'idea-1',
      action: 'Accept',
      comment: 'Great proposal',
    });
    expect(onClose).toHaveBeenCalled();
  });
});
