import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { ISessionsApi } from '../contracts/sessions-api';
import { SKIP_REFRESH } from '../interceptors/skip-refresh';
import { Session } from '../models/session';

/** @inheritdoc */
@Injectable()
export class SessionsApi extends ISessionsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /**
   * Every call here opts out of the refresh interceptor.
   *
   * Renewal is itself one of these calls, so letting the interceptor retry a 401 from `refresh`
   * would call `refresh` again, and again. Signing in has no session to renew in the first place.
   */
  private readonly withoutRetry = new HttpContext().set(SKIP_REFRESH, true);

  async requestLink(emailAddress: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(
        `${this.baseUrl}/sessions/link`,
        { emailAddress },
        { context: this.withoutRetry },
      ),
    );
  }

  exchange(token: string): Promise<Session> {
    return firstValueFrom(
      this.http.post<Session>(`${this.baseUrl}/sessions`, { token }, { context: this.withoutRetry }),
    );
  }

  refresh(): Promise<Session> {
    // No body. The refresh token is a cookie the browser sends on its own, which is the point
    // of it being one.
    return firstValueFrom(
      this.http.post<Session>(`${this.baseUrl}/sessions/refresh`, null, { context: this.withoutRetry }),
    );
  }

  async signOut(): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.baseUrl}/sessions`));
  }
}
