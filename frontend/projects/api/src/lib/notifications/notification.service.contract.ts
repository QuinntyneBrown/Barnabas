import { InjectionToken } from '@angular/core';

import { Notification, NotificationPreference } from '../models/notification';

/** What has happened that concerns the caller. */
export interface INotificationService {
  mine(): Promise<readonly Notification[]>;

  /** Asked from every screen at every width, which is why it is its own call. */
  unreadCount(): Promise<number>;

  /** Marks one read, or all of them when none is named. */
  markRead(notificationId?: string): Promise<void>;

  preferences(): Promise<readonly NotificationPreference[]>;

  setPreferences(
    preferences: readonly NotificationPreference[],
  ): Promise<readonly NotificationPreference[]>;
}

export const NOTIFICATION_SERVICE = new InjectionToken<INotificationService>('NOTIFICATION_SERVICE');
