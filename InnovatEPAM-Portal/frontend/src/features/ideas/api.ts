import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UseQueryOptions } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import type { IdeaStatus } from '@/components/ui';

/** ─── DTOs (mirror backend records) ───────────────────────────────────── */

export interface CategoryDto {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
}

export interface IdeaListItem {
  id: string;
  title: string;
  categoryId: string;
  categoryName: string;
  status: IdeaStatus;
  submitterId: string;
  /** Null when blind-review mode hides identity. Mutually exclusive with submitterAlias. */
  submitterName: string | null;
  hasAttachment: boolean;
  createdAt: string;
  updatedAt: string;
  /** Stable, idea-scoped alias (e.g. "Submitter #ABCD"). Non-null exactly when submitterName is null. */
  submitterAlias: string | null;
  /** Mean of the four dimension averages, rounded to 2dp. Null when no scores yet. */
  overall: number | null;
  /** Distinct reviewers who have scored this idea. */
  reviewerCount: number;
}

export interface PagedIdeas {
  items: IdeaListItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AttachmentSummary {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface IdeaScoreAverageByDimension {
  impact: number;
  feasibility: number;
  innovation: number;
  alignment: number;
}

export interface IdeaScoreEntry {
  reviewerAlias: string;
  impact: number;
  feasibility: number;
  innovation: number;
  alignment: number;
  comment: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface IdeaScoreAggregate {
  count: number;
  overall: number | null;
  averageByDimension: IdeaScoreAverageByDimension | null;
  entries: IdeaScoreEntry[];
}

export interface IdeaDetail {
  id: string;
  title: string;
  description: string;
  categoryId: string;
  categoryName: string;
  status: IdeaStatus;
  submitterId: string;
  submitterName: string | null;
  attachment: AttachmentSummary | null;
  lastDecisionComment: string | null;
  lastDecisionById: string | null;
  lastDecisionByName: string | null;
  lastDecisionAt: string | null;
  createdAt: string;
  updatedAt: string;
  submitterAlias: string | null;
  scores: IdeaScoreAggregate | null;
}

export interface CreateIdeaPayload {
  title: string;
  description: string;
  categoryId: string;
}

export type IdeaSort =
  | 'createdAt'
  | '-createdAt'
  | 'updatedAt'
  | '-updatedAt'
  | 'title'
  | '-title'
  | 'score:asc'
  | 'score:desc';

export interface IdeasFilter {
  status?: IdeaStatus;
  categoryCode?: string;
  submitterId?: string;
  sort?: IdeaSort;
}

/** ─── Query keys ──────────────────────────────────────────────────────── */
export const ideasKeys = {
  all: ['ideas'] as const,
  list: (filter: IdeasFilter, page: number, pageSize: number) =>
    ['ideas', 'list', { ...filter, page, pageSize }] as const,
  detail: (id: string) => ['ideas', 'detail', id] as const,
  categories: () => ['categories'] as const,
};

/** ─── Hooks ───────────────────────────────────────────────────────────── */

export function useCategoriesQuery(
  options?: Omit<UseQueryOptions<CategoryDto[]>, 'queryKey' | 'queryFn'>,
) {
  return useQuery<CategoryDto[]>({
    queryKey: ideasKeys.categories(),
    queryFn: ({ signal }) => apiClient.get<CategoryDto[]>('/categories', signal),
    staleTime: 5 * 60_000,
    ...options,
  });
}

export function useIdeasQuery(filter: IdeasFilter, page: number, pageSize: number) {
  const qs = new URLSearchParams();
  if (filter.status) qs.set('status', filter.status);
  if (filter.categoryCode) qs.set('categoryCode', filter.categoryCode);
  if (filter.submitterId) qs.set('submitterId', filter.submitterId);
  if (filter.sort) qs.set('sort', filter.sort);
  qs.set('page', String(page));
  qs.set('pageSize', String(pageSize));
  return useQuery<PagedIdeas>({
    queryKey: ideasKeys.list(filter, page, pageSize),
    queryFn: ({ signal }) => apiClient.get<PagedIdeas>(`/ideas?${qs.toString()}`, signal),
    placeholderData: (prev) => prev,
  });
}

export function useIdeaQuery(id: string | undefined) {
  return useQuery<IdeaDetail>({
    queryKey: ideasKeys.detail(id ?? ''),
    queryFn: ({ signal }) => apiClient.get<IdeaDetail>(`/ideas/${id}`, signal),
    enabled: Boolean(id),
  });
}

export function useCreateIdeaMutation() {
  const qc = useQueryClient();
  return useMutation<IdeaDetail, unknown, CreateIdeaPayload>({
    mutationFn: (payload) => apiClient.post<IdeaDetail>('/ideas', payload),
    onSuccess: (idea) => {
      qc.invalidateQueries({ queryKey: ideasKeys.all });
      qc.setQueryData(ideasKeys.detail(idea.id), idea);
    },
  });
}

export interface UploadAttachmentVars {
  ideaId: string;
  file: File;
}

export function useUploadAttachmentMutation() {
  const qc = useQueryClient();
  return useMutation<AttachmentSummary, unknown, UploadAttachmentVars>({
    mutationFn: async ({ ideaId, file }) => {
      const form = new FormData();
      form.append('file', file, file.name);
      return apiClient.put<AttachmentSummary>(`/ideas/${ideaId}/attachment`, form);
    },
    onSuccess: (_summary, { ideaId }) => {
      qc.invalidateQueries({ queryKey: ideasKeys.detail(ideaId) });
      qc.invalidateQueries({ queryKey: ideasKeys.all });
    },
  });
}

export function attachmentDownloadUrl(ideaId: string): string {
  return `/ideas/${ideaId}/attachment`;
}
