import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { ideasKeys, type IdeaDetail } from '@/features/ideas/api';
import type { IdeaStatus } from '@/components/state/StatusBadge';

/** Mirrors `DecisionAction` in the backend. */
export type DecisionAction = 'MoveToUnderReview' | 'Accept' | 'Reject';

export interface CreateDecisionPayload {
  ideaId: string;
  action: DecisionAction;
  comment?: string;
}

export interface StatusHistoryEntry {
  id: string;
  fromStatus: IdeaStatus | null;
  toStatus: IdeaStatus;
  actorId: string;
  actorDisplayName: string;
  comment: string | null;
  decisionId: string | null;
  occurredAt: string;
}

const adminKeys = {
  history: (id: string) => ['ideas', 'history', id] as const,
};

/**
 * Records an admin decision (T088). On success invalidates the idea list and
 * detail queries so the UI re-renders with the new status + decision metadata.
 */
export function useDecisionMutation() {
  const qc = useQueryClient();
  return useMutation<IdeaDetail, unknown, CreateDecisionPayload>({
    mutationFn: ({ ideaId, action, comment }) =>
      apiClient.post<IdeaDetail>(`/ideas/${ideaId}/decisions`, { action, comment: comment ?? null }),
    onSuccess: (idea) => {
      qc.setQueryData(ideasKeys.detail(idea.id), idea);
      qc.invalidateQueries({ queryKey: ideasKeys.all });
      qc.invalidateQueries({ queryKey: adminKeys.history(idea.id) });
    },
  });
}

/** Reads the full status timeline for the idea (FR-021). */
export function useIdeaHistoryQuery(ideaId: string | undefined) {
  return useQuery<StatusHistoryEntry[]>({
    queryKey: adminKeys.history(ideaId ?? ''),
    queryFn: ({ signal }) => apiClient.get<StatusHistoryEntry[]>(`/ideas/${ideaId}/history`, signal),
    enabled: Boolean(ideaId),
  });
}
