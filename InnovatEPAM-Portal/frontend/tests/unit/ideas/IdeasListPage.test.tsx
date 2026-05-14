import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IdeasListPage } from '@/features/ideas/IdeasListPage';

// T040 — verify the new Phase 5 surfaces on IdeasListPage:
//   - `LoadingSkeleton` rendered while data is in flight (role="status" + aria-busy)
//   - `EmptyState` rendered when the query resolves with zero items (role="status")
//   - One `StatusBadge` per row (role="status", aria-label="Status: <label>")
// Selectors mirror the primitive APIs; no behavioural assertions on routing or
// data fetching (those are covered by IdeasFilters / API tests).

const useIdeasQueryMock = vi.fn();
const useCategoriesQueryMock = vi.fn();

vi.mock('@/features/ideas/api', async () => {
  const actual = await vi.importActual<typeof import('@/features/ideas/api')>('@/features/ideas/api');
  return {
    ...actual,
    useIdeasQuery: (...args: unknown[]) => useIdeasQueryMock(...args),
    useCategoriesQuery: (...args: unknown[]) => useCategoriesQueryMock(...args),
  };
});

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/ideas']}>
        <IdeasListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  useIdeasQueryMock.mockReset();
  useCategoriesQueryMock.mockReset();
  useCategoriesQueryMock.mockReturnValue({ data: [], isLoading: false });
});

describe('IdeasListPage (T040, 008-ui-polish)', () => {
  it('renders the LoadingSkeleton while the ideas query is in flight', () => {
    useIdeasQueryMock.mockReturnValue({ isLoading: true, isError: false, data: undefined });
    renderPage();
    // LoadingSkeleton sets role="status" + aria-busy="true" + sr-only "Loading…" label.
    const skeletons = screen.getAllByRole('status');
    expect(skeletons.some((node) => node.getAttribute('aria-busy') === 'true')).toBe(true);
    expect(screen.getByText(/loading…/i)).toBeInTheDocument();
  });

  it('renders an EmptyState when the query returns zero items', () => {
    useIdeasQueryMock.mockReturnValue({
      isLoading: false,
      isError: false,
      data: { items: [], total: 0 },
    });
    renderPage();
    expect(screen.getByText(/no matching ideas yet/i)).toBeInTheDocument();
  });

  it('renders exactly one StatusBadge per idea row', () => {
    useIdeasQueryMock.mockReturnValue({
      isLoading: false,
      isError: false,
      data: {
        total: 2,
        items: [
          {
            id: 'i1',
            title: 'First idea',
            categoryId: 'c1',
            categoryName: 'Process',
            status: 'Submitted',
            submitterId: 'u1',
            submitterName: 'Ada',
            hasAttachment: false,
            createdAt: '2026-01-01T00:00:00Z',
            updatedAt: '2026-01-01T00:00:00Z',
          },
          {
            id: 'i2',
            title: 'Second idea',
            categoryId: 'c2',
            categoryName: 'Tech',
            status: 'Accepted',
            submitterId: 'u2',
            submitterName: 'Grace',
            hasAttachment: true,
            createdAt: '2026-01-02T00:00:00Z',
            updatedAt: '2026-01-02T00:00:00Z',
          },
        ],
      },
    });
    renderPage();
    const list = screen.getByRole('list', { name: /ideas/i });
    const items = within(list).getAllByRole('listitem');
    expect(items).toHaveLength(2);
    // Each row exposes a single StatusBadge via `role="status"` with the
    // matching `aria-label`.
    const [first, second] = items as [HTMLElement, HTMLElement];
    const submitted = within(first).getAllByRole('status');
    expect(submitted.filter((n) => n.getAttribute('aria-label') === 'Status: Submitted')).toHaveLength(1);
    const accepted = within(second).getAllByRole('status');
    expect(accepted.filter((n) => n.getAttribute('aria-label') === 'Status: Accepted')).toHaveLength(1);
  });
});
