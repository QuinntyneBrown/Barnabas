import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { Notification, NotificationPreference } from '../models/notification';
import { INotificationService } from './notification.service.contract';

/** @inheritdoc */
@Injectable()
export class NotificationService implements INotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  mine(): Promise<readonly Notification[]> {
    return firstValueFrom(this.http.get<Notification[]>(`${this.baseUrl}/notifications`));
  }

  async unreadCount(): Promise<number> {
    const answer = await firstValueFrom(
      this.http.get<{ unread: number }>(`${this.baseUrl}/notifications/unread-count`),
    );

    return answer.unread;
  }

  markRead(notificationId?: string): Promise<void> {
    let params = new HttpParams();

    if (notificationId) {
      params = params.set('notificationId', notificationId);
    }

    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/notifications/read`, {}, { params }));
  }

  preferences(): Promise<readonly NotificationPreference[]> {
    return firstValueFrom(
      this.http.get<NotificationPreference[]>(`${this.baseUrl}/notifications/preferences`),
    );
  }

  setPreferences(
    preferences: readonly NotificationPreference[],
  ): Promise<readonly NotificationPreference[]> {
    return firstValueFrom(
      this.http.put<NotificationPreference[]>(`${this.baseUrl}/notifications/preferences`, {
        preferences,
      }),
    );
  }
}
