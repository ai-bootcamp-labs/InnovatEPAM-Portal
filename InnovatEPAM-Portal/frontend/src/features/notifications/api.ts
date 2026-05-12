import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';

/**
 * Notifications feature API surface (T105, T106 — FR-022).
 *
 * The unread-count poll runs every 30 seconds — the polling cadence is
 * scoped here so the polling rule lives next to the hook that enforces it
 * (see [`useUnreadCount.ts`](./useUnreadCount.ts)).
 */

export const NOTIFICATION_KIND_IDEA_STATUS_CHANGED = 'idea_status_changed';

export interface NotificationItem {
  id: string;
  kind: string;
  ideaId: string | null;
  /** Raw JSON payload string returned by the API. */
  payload: string;
  readAt: string | null;
  createdAt: string;
}

export interface IdeaStatusChangedPayload {
  ideaId?: string;
  ideaTitle?: string;
  fromStatus?: string;
  toStatus?: string;
  decidedById?: string;
  decidedByDisplayName?: string;
}

export interface UnreadCountResponse {
  count: number;
}

export const notificationsKeys = {
  all: ['notifications'] as const,
  list: () => ['notifications', 'list'] as const,
  unreadCount: () => ['notifications', 'unread-count'] as const,
};

/** Lists the caller's most recent notifications, newest first. */
export function useNotificationsQuery(enabled: boolean = true) {
  return useQuery<NotificationItem[]>({
    queryKey: notificationsKeys.list(),
    queryFn: ({ signal }) => apiClient.get<NotificationItem[]>('/notifications', signal),
    enabled,
    staleTime: 15_000,
  });
}

/** Marks a notification as read; invalidates the list and unread-count queries. */
export function useMarkNotificationReadMutation() {
  const qc = useQueryClient();
  return useMutation<void, unknown, string>({
    mutationFn: (id) => apiClient.post<void>(`/notifications/${id}/read`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: notificationsKeys.all });
    },
  });
}

/** Safely parses a notification payload string; returns `null` on malformed JSON. */
export function parseNotificationPayload<T = IdeaStatusChangedPayload>(raw: string): T | null {
  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}
