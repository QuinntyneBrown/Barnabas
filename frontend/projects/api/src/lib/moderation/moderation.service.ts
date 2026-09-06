import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { FlaggedListing, ReportedListing } from '../models/listing-report';
import { PendingMember } from '../models/pending-member';
import { ReportReason } from '../models/report-reason';
import { IModerationService } from './moderation.service.contract';

/** @inheritdoc */
@Injectable()
export class ModerationService implements IModerationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  report(listingId: string, reason: ReportReason, note: string | null): Promise<ReportedListing> {
    return firstValueFrom(
      this.http.post<ReportedListing>(`${this.baseUrl}/listings/${listingId}/reports`, {
        reason,
        note,
      }),
    );
  }

  flaggedListings(): Promise<readonly FlaggedListing[]> {
    return firstValueFrom(this.http.get<FlaggedListing[]>(`${this.baseUrl}/moderation/listings`));
  }

  async approveListing(listingId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.baseUrl}/moderation/listings/${listingId}/approve`, {}),
    );
  }

  async removeListing(listingId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.baseUrl}/moderation/listings/${listingId}/remove`, {}),
    );
  }

  pendingMembers(): Promise<readonly PendingMember[]> {
    return firstValueFrom(this.http.get<PendingMember[]>(`${this.baseUrl}/moderation/members`));
  }

  async approveMember(memberId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.baseUrl}/moderation/members/${memberId}/approve`, {}),
    );
  }

  async declineMember(memberId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.baseUrl}/moderation/members/${memberId}/decline`, {}),
    );
  }
}
