import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { Message } from '../models/message';
import { ThreadDetail } from '../models/thread-detail';
import { ThreadSummary } from '../models/thread-summary';
import { IThreadService } from './thread.service.contract';

/** @inheritdoc */
@Injectable()
export class ThreadService implements IThreadService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  mine(): Promise<readonly ThreadSummary[]> {
    return firstValueFrom(this.http.get<ThreadSummary[]>(`${this.baseUrl}/threads`));
  }

  get(threadId: string): Promise<ThreadDetail> {
    return firstValueFrom(this.http.get<ThreadDetail>(`${this.baseUrl}/threads/${threadId}`));
  }

  send(threadId: string, body: string): Promise<Message> {
    return firstValueFrom(
      this.http.post<Message>(`${this.baseUrl}/threads/${threadId}/messages`, { body }),
    );
  }
}
