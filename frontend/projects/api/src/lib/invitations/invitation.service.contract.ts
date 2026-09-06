import { InjectionToken } from '@angular/core';

import { JoinedCongregation, JoiningProfile, RedeemedInvite } from '../models/redeemed-invite';

/** Redeeming a code, and joining with it. */
export interface IInvitationService {
  /**
   * Spends a code and begins joining.
   *
   * Anonymous: nobody is signed in, and the code is what reveals which congregation is being
   * joined.
   */
  redeem(code: string): Promise<RedeemedInvite>;

  /** Finishes joining, leaving the member waiting on a moderator. */
  join(profile: JoiningProfile): Promise<JoinedCongregation>;
}

export const INVITATION_SERVICE = new InjectionToken<IInvitationService>('INVITATION_SERVICE');
