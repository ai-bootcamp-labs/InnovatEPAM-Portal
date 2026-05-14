import { useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatRelative } from '@/lib/date';
import { Button, LoadingSkeleton } from '@/components/ui';
import { cn } from '@/lib/ui/cn';
import {
  NOTIFICATION_KIND_IDEA_STATUS_CHANGED,
  parseNotificationPayload,
  useMarkNotificationReadMutation,
  useNotificationsQuery,
  type IdeaStatusChangedPayload,
  type NotificationItem,
} from './api';

interface NotificationsPanelProps {
  /** Controls visibility of the slide-over sheet. */
  open: boolean;
  /** Closes the panel (overlay click, ESC, or item navigation). */
  onClose: () => void;
}

/**
 * Side-sheet listing the caller's recent notifications (T106, FR-022).
 *
 * - Triggered by the bell icon in {@link AppShell}.
 * - Item timestamps go through `formatRelative` per R10 / X4.
 * - Clicking an item marks it read and navigates to the related idea.
 * - Hand-rolled sheet (the project does not depend on shadcn primitives yet);
 *   matches the markup style of `DecisionDialog`.
 */
export function NotificationsPanel({ open, onClose }: NotificationsPanelProps): JSX.Element | null {
  const navigate = useNavigate();
  const query = useNotificationsQuery(open);
  const markRead = useMarkNotificationReadMutation();
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);

  useEffect(() => {
    if (!open) return;
    requestAnimationFrame(() => closeButtonRef.current?.focus());
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  if (!open) return null;

  function handleSelect(item: NotificationItem) {
    if (item.readAt === null) {
      markRead.mutate(item.id);
    }
    if (item.ideaId) {
      navigate(`/ideas/${item.ideaId}`);
    }
    onClose();
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="notifications-panel-title"
      className="fixed inset-0 z-50 flex justify-end"
    >
      {/* ui-polish-exception: invisible overlay click-catcher behind the sheet;
          a Button primitive would carry visible styling we explicitly don't want here. */}
      <button
        type="button"
        aria-label="Close notifications"
        onClick={onClose}
        className="absolute inset-0 h-full w-full cursor-default bg-black/40"
      />
      <aside className="relative z-10 flex h-full w-full max-w-sm flex-col border-l border-border bg-card shadow-xl">
        <header className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 id="notifications-panel-title" className="text-base font-semibold text-foreground">
            Notifications
          </h2>
          <Button
            ref={closeButtonRef}
            variant="ghost"
            size="sm"
            onClick={onClose}
          >
            Close
          </Button>
        </header>

        <div className="flex-1 overflow-y-auto">
          {query.isLoading ? (
            <div className="px-4 py-6">
              <LoadingSkeleton rows={3} rowClassName="h-14" />
            </div>
          ) : query.isError ? (
            <p role="alert" className="px-4 py-6 text-sm text-destructive">
              Could not load notifications.
            </p>
          ) : query.data && query.data.length > 0 ? (
            <ul className="divide-y divide-border" aria-label="Notifications">
              {query.data.map((item) => {
                const unread = item.readAt === null;
                return (
                  <li key={item.id}>
                    {/* ui-polish-exception: card-style row is a full-width <button> so a
                       single click both marks the notification as read and navigates to
                       the related idea (T046, FR-008 — behaviour preserved). The visual
                       treatment mirrors a `Card hoverable` row and `Button variant="ghost"`. */}
                    <button
                      type="button"
                      onClick={() => handleSelect(item)}
                      className={cn(
                        'flex w-full flex-col gap-1 px-4 py-3 text-left',
                        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset',
                        'hover:bg-accent hover:text-accent-foreground',
                        'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
                        unread && 'bg-accent/30',
                      )}
                    >
                      <span className="flex items-center gap-2 text-sm font-medium text-foreground">
                        {unread ? (
                          <span
                            aria-hidden="true"
                            className="inline-block h-2 w-2 rounded-full bg-primary"
                          />
                        ) : null}
                        <NotificationTitle item={item} />
                      </span>
                      <NotificationBody item={item} />
                      <span className="text-xs text-muted-foreground">{formatRelative(item.createdAt)}</span>
                    </button>
                  </li>
                );
              })}
            </ul>
          ) : (
            <p className="px-4 py-6 text-sm text-muted-foreground">You have no notifications yet.</p>
          )}
        </div>
      </aside>
    </div>
  );
}

function NotificationTitle({ item }: { item: NotificationItem }): JSX.Element {
  if (item.kind === NOTIFICATION_KIND_IDEA_STATUS_CHANGED) {
    return <span>Idea status updated</span>;
  }
  return <span>{item.kind}</span>;
}

function NotificationBody({ item }: { item: NotificationItem }): JSX.Element | null {
  if (item.kind === NOTIFICATION_KIND_IDEA_STATUS_CHANGED) {
    const payload = parseNotificationPayload<IdeaStatusChangedPayload>(item.payload);
    if (!payload) return null;
    const title = payload.ideaTitle ?? 'Your idea';
    const to = payload.toStatus ?? 'a new status';
    const by = payload.decidedByDisplayName ? ` by ${payload.decidedByDisplayName}` : '';
    return (
      <span className="text-sm text-muted-foreground">
        “{title}” moved to {to}
        {by}.
      </span>
    );
  }
  return null;
}
