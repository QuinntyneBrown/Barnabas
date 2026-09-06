import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { IInviteService, IssuedInvite } from './invite.service.contract';

/** @inheritdoc */
@Injectable()
export class InviteService implements IInviteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  issue(): Promise<IssuedInvite> {
    return firstValueFrom(this.http.post<IssuedInvite>(`${this.baseUrl}/invites`, {}));
  }

  revoke(inviteCodeId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/invites/${inviteCodeId}`));
  }
}
