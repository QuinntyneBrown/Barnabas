import { Injectable, computed, inject, signal } from '@angular/core';
import { NOTIFICATION_SERVICE, Notification, NotificationKind } from '@barnabas/api';

/**
 * What has happened that concerns the signed-in member.
 *
 * The unread count is held here rather than fetched by each screen that shows it, because
 * `L2-073` puts it on every screen at every width — several copies asking independently would
 * disagree with each other the moment one of them was stale.
 */
@Injectable({ providedIn: 'root' })
export class NotificationStore {
  private readonly notifications = inject(NOTIFICATION_SERVICE);

  private readonly all = signal<readonly Notification[]>([]);

  readonly mine = this.all.asReadonly();
  readonly unread = signal(0);
  readonly loading = signal(false);

  readonly hasUnread = computed(() => this.unread() > 0);

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      this.all.set(await this.notifications.mine());
      await this.refreshCount();
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Asks only for the count.
   *
   * The shell wants the number and not the list, and a member who never opens notifications
   * should not be paying for fifty rows on every navigation.
   */
  async refreshCount(): Promise<void> {
    try {
      this.unread.set(await this.notifications.unreadCount());
    } catch {
      // Signed out, or waiting on a moderator. A badge that guesses is worse than no badge.
      this.unread.set(0);
    }
  }

  async markAllRead(): Promise<void> {
    await this.notifications.markRead();
    await this.load();
  }

  async markRead(notificationId: string): Promise<void> {
    await this.notifications.markRead(notificationId);
    await this.load();
  }

  clear(): void {
    this.all.set([]);
    this.unread.set(0);
  }
}

/**
 * Where a notification leads.
 *
 * The routing lives here rather than on the server, because where a thing lives is the client's
 * business — a path stored in a row would be wrong the first time a route changed. Every kind
 * carries the identifiers its destination needs, so none of these can fall through to nothing.
 */
export function destinationOf(notification: Notification): readonly string[] {
  switch (notification.kind) {
    case 'RequestReceived':
      return ['/inbox/requests'];
    case 'RequestAccepted':
      return notification.threadId ? ['/threads', notification.threadId] : ['/inbox/my-requests'];
    case 'RequestDeclined':
      return ['/inbox/my-requests'];
    case 'MessageReceived':
      return notification.threadId ? ['/threads', notification.threadId] : ['/inbox/messages'];
    case 'ListingRemoved':
      return ['/my-listings'];
  }
}

/** What a notification says, in the words of its kind. */
export function wordsForNotification(notification: Notification): string {
  const who = notification.subjectMemberDisplayName ?? 'Somebody';
  const what = notification.listingTitle ?? 'your listing';

  const said: Record<NotificationKind, string> = {
    RequestReceived: `${who} asked about ${what}`,
    RequestAccepted: `${who} said yes to ${what}`,
    RequestDeclined: `${who} could not manage ${what}`,
    MessageReceived: `${who} wrote to you about ${what}`,
    ListingRemoved: `A moderator took ${what} off the board`,
  };

  return said[notification.kind];
}
