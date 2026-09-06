import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { JoinedCongregation, JoiningProfile, RedeemedInvite } from '../models/redeemed-invite';
import { IInvitationService } from './invitation.service.contract';

/** @inheritdoc */
@Injectable()
export class InvitationService implements IInvitationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  redeem(code: string): Promise<RedeemedInvite> {
    return firstValueFrom(this.http.post<RedeemedInvite>(`${this.baseUrl}/invites/redeem`, { code }));
  }

  join(profile: JoiningProfile): Promise<JoinedCongregation> {
    return firstValueFrom(
      this.http.post<JoinedCongregation>(`${this.baseUrl}/joining/profile`, profile),
    );
  }
}
