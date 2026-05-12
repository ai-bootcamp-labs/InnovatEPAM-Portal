import { describe, expect, it, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { IdeaSubmitForm } from '@/features/ideas/IdeaSubmitForm';

const createMutate = vi.fn();
const uploadMutate = vi.fn();

vi.mock('@/features/ideas/api', () => ({
  useCategoriesQuery: () => ({
    data: [{ id: '11111111-1111-1111-1111-111111111111', code: 'process', name: 'Process', sortOrder: 10 }],
    isLoading: false,
  }),
  useCreateIdeaMutation: () => ({ mutateAsync: createMutate, isPending: false }),
  useUploadAttachmentMutation: () => ({ mutateAsync: uploadMutate, isPending: false }),
}));

function renderForm() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <IdeaSubmitForm />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('IdeaSubmitForm (T052)', () => {
  beforeEach(() => {
    createMutate.mockReset();
    uploadMutate.mockReset();
  });

  it('rejects titles shorter than 5 characters', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.type(screen.getByLabelText(/title/i), 'Hi');
    await user.click(screen.getByRole('button', { name: /submit idea/i }));
    expect(await screen.findByText(/at least 5 characters/i)).toBeInTheDocument();
    expect(createMutate).not.toHaveBeenCalled();
  });

  it('rejects unsupported file extensions', async () => {
    renderForm();
    const file = new File(['malicious'], 'evil.exe', { type: 'application/octet-stream' });
    const input = screen.getByLabelText(/attachment/i) as HTMLInputElement;
    fireEvent.change(input, { target: { files: [file] } });
    expect(await screen.findByText(/unsupported file type/i)).toBeInTheDocument();
  });

  it('submits a valid payload', async () => {
    createMutate.mockResolvedValue({ id: 'idea-1' });
    const user = userEvent.setup();
    renderForm();
    await user.type(screen.getByLabelText(/title/i), 'Improve the onboarding flow');
    await user.selectOptions(screen.getByLabelText(/category/i), '11111111-1111-1111-1111-111111111111');
    await user.type(screen.getByLabelText(/description/i), 'Let us streamline new joiner experience.');
    await user.click(screen.getByRole('button', { name: /submit idea/i }));

    expect(createMutate).toHaveBeenCalledTimes(1);
    expect(createMutate).toHaveBeenCalledWith({
      title: 'Improve the onboarding flow',
      description: 'Let us streamline new joiner experience.',
      categoryId: '11111111-1111-1111-1111-111111111111',
    });
  });
});
