import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { IdeasFilters } from '@/features/ideas/IdeasFilters';

const useIdeasQuery = vi.fn();

vi.mock('@/features/ideas/api', async () => {
  const actual = await vi.importActual<typeof import('@/features/ideas/api')>('@/features/ideas/api');
  return {
    ...actual,
    useCategoriesQuery: () => ({
      data: [
        { id: 'c1', code: 'process', name: 'Process', sortOrder: 10 },
        { id: 'c2', code: 'tech', name: 'Tech', sortOrder: 20 },
      ],
      isLoading: false,
    }),
    useIdeasQuery: (...args: unknown[]) => useIdeasQuery(...args),
  };
});

function LocationSpy({ onChange }: { onChange: (search: string) => void }) {
  const loc = useLocation();
  onChange(loc.search);
  return null;
}

function renderFilters(initial = '/ideas') {
  let lastSearch = '';
  const utils = render(
    <MemoryRouter initialEntries={[initial]}>
      <Routes>
        <Route
          path="/ideas"
          element={
            <>
              <IdeasFilters />
              <LocationSpy onChange={(s) => (lastSearch = s)} />
            </>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
  return { ...utils, getSearch: () => lastSearch };
}

describe('IdeasFilters (T094)', () => {
  beforeEach(() => {
    useIdeasQuery.mockReset();
  });

  it('writes the selected status to the URL search params', async () => {
    const user = userEvent.setup();
    const { getSearch } = renderFilters();
    await user.selectOptions(screen.getByLabelText(/filter by status/i), 'Accepted');
    await waitFor(() => expect(getSearch()).toContain('status=Accepted'));
  });

  it('writes the selected category code to the URL', async () => {
    const user = userEvent.setup();
    const { getSearch } = renderFilters();
    await user.selectOptions(screen.getByLabelText(/filter by category/i), 'process');
    await waitFor(() => expect(getSearch()).toContain('categoryCode=process'));
  });

  it('resets the page when a filter changes', async () => {
    const user = userEvent.setup();
    const { getSearch } = renderFilters('/ideas?page=4');
    await user.selectOptions(screen.getByLabelText(/sort ideas/i), 'title');
    await waitFor(() => {
      const search = getSearch();
      expect(search).toContain('sort=title');
      expect(search).not.toContain('page=');
    });
  });

  it('updates the page size in the URL', async () => {
    const user = userEvent.setup();
    const { getSearch } = renderFilters();
    await user.selectOptions(screen.getByLabelText(/results per page/i), '50');
    await waitFor(() => expect(getSearch()).toContain('pageSize=50'));
  });
});
