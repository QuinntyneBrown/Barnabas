import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiError, INVITATION_SERVICE, JoinedCongregation, RedeemedInvite } from '@barnabas/api';

/** Why a code did not work, in terms the screen can act on. */
export type InviteRefusal = 'unrecognised' | 'spent' | 'throttled' | 'unknown';

/**
 * The joining flow, held across the two screens it spans.
 *
 * Joining is two steps and cannot be one: redeeming spends the code and returns the congregation,
 * and the profile follows. What carries between them is a bearer token, so this holds it — a
 * member who reloads the profile page has lost their code, and the screen says so rather than
 * silently failing.
 */
@Injectable({ providedIn: 'root' })
export class JoiningStore {
  private readonly invitations = inject(INVITATION_SERVICE);

  private readonly invite = signal<RedeemedInvite | null>(null);
  private readonly refusal = signal<InviteRefusal | null>(null);

  readonly redeemed = this.invite.asReadonly();
  readonly refusedBecause = this.refusal.asReadonly();

  readonly congregationName = computed(() => this.invite()?.congregationName ?? '');
  readonly neighbourhoods = computed(() => this.invite()?.neighbourhoods ?? []);

  /** Whether the profile step can be reached at all. */
  readonly canSupplyAProfile = computed(() => this.invite() !== null);

  /**
   * Spends the code.
   *
   * The three refusals are told apart because the screens differ: an unrecognised code invites a
   * retry, a spent one sends the member back to whoever invited them, and a throttled one asks
   * them to wait. The API says only which of the three it was, never why a particular code is
   * dead.
   */
  async redeem(code: string): Promise<boolean> {
    this.refusal.set(null);

    try {
      this.invite.set(await this.invitations.redeem(code));

      return true;
    } catch (failure) {
      this.invite.set(null);
      this.refusal.set(refusalFrom(failure));

      return false;
    }
  }

  async join(profile: {
    emailAddress: string;
    displayName: string;
    neighbourhood: string;
    reasonForJoining: string | null;
  }): Promise<JoinedCongregation> {
    const invite = this.invite();

    if (invite === null) {
      throw new Error('There is no joining session to finish.');
    }

    return this.invitations.join({ joiningToken: invite.joiningToken, ...profile });
  }

  clear(): void {
    this.invite.set(null);
    this.refusal.set(null);
  }
}

function refusalFrom(failure: unknown): InviteRefusal {
  if (!(failure instanceof ApiError)) {
    return 'unknown';
  }

  switch (failure.status) {
    case 404:
      return 'unrecognised';
    case 410:
      return 'spent';
    case 429:
      return 'throttled';
    default:
      return 'unknown';
  }
}
