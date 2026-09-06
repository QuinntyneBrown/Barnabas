import { InjectionToken } from '@angular/core';

import { Message } from '../models/message';
import { ThreadDetail } from '../models/thread-detail';
import { ThreadSummary } from '../models/thread-summary';

/**
 * Where a handoff is arranged.
 *
 * There is nothing here that creates a thread. One exists only as a consequence of an accepted
 * request, which is what guarantees every conversation has a subject.
 */
export interface IThreadService {
  mine(): Promise<readonly ThreadSummary[]>;

  /** Opening a thread marks it read, for the member opening it alone. */
  get(threadId: string): Promise<ThreadDetail>;

  send(threadId: string, body: string): Promise<Message>;
}

export const THREAD_SERVICE = new InjectionToken<IThreadService>('THREAD_SERVICE');
