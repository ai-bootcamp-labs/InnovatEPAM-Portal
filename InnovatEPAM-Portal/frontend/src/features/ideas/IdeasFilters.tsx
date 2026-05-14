import { useSearchParams } from 'react-router-dom';
import { useCategoriesQuery, type IdeaSort, type IdeasFilter } from '@/features/ideas/api';
import { Select, type IdeaStatus } from '@/components/ui';

const STATUS_OPTIONS: IdeaStatus[] = ['Submitted', 'UnderReview', 'Accepted', 'Rejected'];

const SORT_OPTIONS: { value: IdeaSort; label: string }[] = [
  { value: '-createdAt', label: 'Newest first' },
  { value: 'createdAt', label: 'Oldest first' },
  { value: '-updatedAt', label: 'Recently updated' },
  { value: 'title', label: 'Title (A→Z)' },
  { value: '-title', label: 'Title (Z→A)' },
];

const SORT_VALUES = new Set<IdeaSort>(SORT_OPTIONS.map((o) => o.value));
const STATUS_VALUES = new Set<IdeaStatus>(STATUS_OPTIONS);

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

export interface IdeasUrlState {
  filter: IdeasFilter;
  page: number;
  pageSize: number;
}

/** Reads filter + paging state from the URL (T097). Defaults page=1, pageSize=20. */
export function useIdeasUrlState(): IdeasUrlState {
  const [params] = useSearchParams();
  const statusParam = params.get('status');
  const sortParam = params.get('sort');
  const filter: IdeasFilter = {
    status: statusParam && STATUS_VALUES.has(statusParam as IdeaStatus) ? (statusParam as IdeaStatus) : undefined,
    categoryCode: params.get('categoryCode') ?? undefined,
    sort: sortParam && SORT_VALUES.has(sortParam as IdeaSort) ? (sortParam as IdeaSort) : undefined,
  };
  const page = Math.max(1, Number.parseInt(params.get('page') ?? '1', 10) || 1);
  const rawSize = Number.parseInt(params.get('pageSize') ?? '20', 10);
  const pageSize = PAGE_SIZE_OPTIONS.includes(rawSize) ? rawSize : 20;
  return { filter, page, pageSize };
}

/**
 * Filter bar bound to the URL search params (T097). Each control updates the
 * URL, which triggers a refetch and makes the view deep-linkable (M3.2).
 */
export function IdeasFilters(): JSX.Element {
  const [params, setParams] = useSearchParams();
  const categoriesQuery = useCategoriesQuery();
  const { pageSize } = useIdeasUrlState();

  function updateParam(key: string, value: string | null) {
    const next = new URLSearchParams(params);
    if (value === null || value === '') next.delete(key);
    else next.set(key, value);
    // Reset to page 1 whenever a filter changes; preserve pageSize.
    next.delete('page');
    setParams(next, { replace: true });
  }

  function updatePageSize(value: string) {
    const next = new URLSearchParams(params);
    next.set('pageSize', value);
    next.delete('page');
    setParams(next, { replace: true });
  }

  return (
    <section className="flex flex-wrap gap-3" aria-label="Filter ideas">
      <label className="flex flex-col gap-1 text-xs font-medium text-muted-foreground">
        Status
        <Select
          aria-label="Filter by status"
          value={params.get('status') ?? ''}
          onChange={(e) => updateParam('status', e.target.value || null)}
          className="h-9 min-w-[10rem]"
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </Select>
      </label>

      <label className="flex flex-col gap-1 text-xs font-medium text-muted-foreground">
        Category
        <Select
          aria-label="Filter by category"
          value={params.get('categoryCode') ?? ''}
          onChange={(e) => updateParam('categoryCode', e.target.value || null)}
          disabled={categoriesQuery.isLoading}
          className="h-9 min-w-[10rem]"
        >
          <option value="">All categories</option>
          {(categoriesQuery.data ?? []).map((c) => (
            <option key={c.id} value={c.code}>
              {c.name}
            </option>
          ))}
        </Select>
      </label>

      <label className="flex flex-col gap-1 text-xs font-medium text-muted-foreground">
        Sort
        <Select
          aria-label="Sort ideas"
          value={params.get('sort') ?? '-createdAt'}
          onChange={(e) => updateParam('sort', e.target.value)}
          className="h-9 min-w-[10rem]"
        >
          {SORT_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </Select>
      </label>

      <label className="flex flex-col gap-1 text-xs font-medium text-muted-foreground">
        Page size
        <Select
          aria-label="Results per page"
          value={String(pageSize)}
          onChange={(e) => updatePageSize(e.target.value)}
          className="h-9 min-w-[6rem]"
        >
          {PAGE_SIZE_OPTIONS.map((n) => (
            <option key={n} value={n}>
              {n}
            </option>
          ))}
        </Select>
      </label>
    </section>
  );
}
