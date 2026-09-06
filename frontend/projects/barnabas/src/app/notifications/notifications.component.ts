import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Notification } from '@barnabas/api';
import { NotificationStore, destinationOf, wordsForNotification } from '@barnabas/domain';

/**
 * What has happened that concerns this member.
 *
 * Every row leads somewhere. That is L2-074, and it is kept by the shape of the data rather than
 * by care here: a notification cannot be created without the identifiers its destination needs,
 * so there is no row that could fall through to nothing.
 *
 * Read, not deleted. A member looking back should still find what they were told.
 */
@Component({
  selector: 'bar-notifications',
  imports: [RouterLink],
  templateUrl: './notifications.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsComponent {
  private readonly store = inject(NotificationStore);

  readonly notifications = this.store.mine;
  readonly loading = this.store.loading;
  readonly hasUnread = this.store.hasUnread;

  constructor() {
    void this.store.load();
  }

  wordsFor(notification: Notification): string {
    return wordsForNotification(notification);
  }

  destinationFor(notification: Notification): readonly string[] {
    return destinationOf(notification);
  }

  async open(notification: Notification): Promise<void> {
    if (notification.unread) {
      await this.store.markRead(notification.notificationId);
    }
  }

  async markAllRead(): Promise<void> {
    await this.store.markAllRead();
  }
}
