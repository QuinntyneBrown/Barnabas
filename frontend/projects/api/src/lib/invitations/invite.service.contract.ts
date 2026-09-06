import { InjectionToken } from '@angular/core';

/** A code a moderator has issued, as they have to read it out. */
export interface IssuedInvite {
  readonly inviteCodeId: string;
  readonly code: string;
  readonly expiresAt: string;
}

/** Issuing codes for one's own congregation. */
export interface IInviteService {
  /**
   * Issues a code.
   *
   * It names no congregation: the API takes it from the verified session, so a moderator cannot
   * issue a code into a parish they do not belong to.
   */
  issue(): Promise<IssuedInvite>;

  revoke(inviteCodeId: string): Promise<void>;
}

export const INVITE_SERVICE = new InjectionToken<IInviteService>('INVITE_SERVICE');
