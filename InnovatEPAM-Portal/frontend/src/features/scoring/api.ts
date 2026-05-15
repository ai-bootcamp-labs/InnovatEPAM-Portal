import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { ideasKeys, type IdeaDetail, type IdeaScoreAggregate } from '@/features/ideas/api';

/** Phase 7 — submit/update a score (FR-006..009). */
export interface SubmitScorePayload {
  impact: number;
  feasibility: number;
  innovation: number;
  alignment: number;
  comment: string | null;
}

export function useUpsertScoreMutation(ideaId: string) {
  const qc = useQueryClient();
  return useMutation<IdeaScoreAggregate, unknown, SubmitScorePayload>({
    mutationFn: (payload) =>
      apiClient.post<IdeaScoreAggregate>(`/ideas/${ideaId}/scores`, payload),
    onSuccess: (aggregate) => {
      // Patch the detail cache so the panel updates instantly.
      qc.setQueryData<IdeaDetail | undefined>(ideasKeys.detail(ideaId), (prev) =>
        prev ? { ...prev, scores: aggregate } : prev,
      );
      qc.invalidateQueries({ queryKey: ideasKeys.all });
    },
  });
}
