import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { notificationsKeys, type UnreadCountResponse } from './api';

/**
 * Polls the unread-notifications count every 30 seconds (T105, FR-022, R14).
 *
 * - `refetchInterval: 30_000` — the cadence specified by the spec; matches the
 *   30-second poll target in `quickstart.md` checklist M2.10.
 * - `refetchOnWindowFocus: true` — refresh as soon as the user returns to the tab.
 * - `enabled` — pass `false` (e.g., when the user is signed out) to suspend
 *   polling and avoid pinging the API while unauthenticated.
 */
export function useUnreadCount(enabled: boolean = true) {
  return useQuery<UnreadCountResponse, unknown, number>({
    queryKey: notificationsKeys.unreadCount(),
    queryFn: ({ signal }) => apiClient.get<UnreadCountResponse>('/notifications/unread-count', signal),
    enabled,
    refetchInterval: enabled ? 30_000 : false,
    refetchOnWindowFocus: true,
    staleTime: 15_000,
    select: (data) => data.count,
  });
}
