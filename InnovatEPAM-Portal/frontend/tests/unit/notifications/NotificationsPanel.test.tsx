import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationsPanel } from '@/features/notifications/NotificationsPanel';
import { useUnreadCount } from '@/features/notifications/useUnreadCount';
import { renderHook } from '@testing-library/react';

const getMock = vi.fn();
const postMock = vi.fn();

vi.mock('@/lib/api/client', () => ({
  apiClient: {
    get: (path: string) => getMock(path),
    post: (path: string) => postMock(path),
  },
}));

const navigateMock = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return { ...actual, useNavigate: () => navigateMock };
});

function wrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
  Wrapper.displayName = 'NotificationsTestWrapper';
  return Wrapper;
}

beforeEach(() => {
  getMock.mockReset();
  postMock.mockReset();
  navigateMock.mockReset();
});

describe('useUnreadCount (T105)', () => {
  it('polls /notifications/unread-count and surfaces the count', async () => {
    getMock.mockResolvedValue({ count: 4 });
    const { result } = renderHook(() => useUnreadCount(true), { wrapper: wrapper() });
    await waitFor(() => expect(result.current.data).toBe(4));
    expect(getMock).toHaveBeenCalledWith('/notifications/unread-count');
  });

  it('is disabled when not authenticated', () => {
    getMock.mockResolvedValue({ count: 99 });
    const { result } = renderHook(() => useUnreadCount(false), { wrapper: wrapper() });
    expect(result.current.fetchStatus).toBe('idle');
    expect(getMock).not.toHaveBeenCalled();
  });
});

describe('NotificationsPanel (T106)', () => {
  it('renders the empty state when there are no notifications', async () => {
    getMock.mockResolvedValue([]);
    render(<NotificationsPanel open onClose={vi.fn()} />, { wrapper: wrapper() });
    expect(await screen.findByText(/no notifications yet/i)).toBeInTheDocument();
  });

  it('lists notifications, marks read, and navigates to the related idea', async () => {
    const user = userEvent.setup();
    getMock.mockResolvedValue([
      {
        id: 'n1',
        kind: 'idea_status_changed',
        ideaId: 'idea-42',
        payload: JSON.stringify({
          ideaTitle: 'Phase 6 demo',
          fromStatus: 'Submitted',
          toStatus: 'UnderReview',
          decidedByDisplayName: 'Admin One',
        }),
        readAt: null,
        createdAt: new Date().toISOString(),
      },
    ]);
    postMock.mockResolvedValue(undefined);
    const onClose = vi.fn();

    render(<NotificationsPanel open onClose={onClose} />, { wrapper: wrapper() });

    const item = await screen.findByRole('button', { name: /idea status updated/i });
    await user.click(item);

    await waitFor(() => expect(postMock).toHaveBeenCalledWith('/notifications/n1/read'));
    expect(navigateMock).toHaveBeenCalledWith('/ideas/idea-42');
    expect(onClose).toHaveBeenCalled();
  });
});

afterEach(() => {
  vi.clearAllMocks();
});
