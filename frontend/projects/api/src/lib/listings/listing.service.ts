import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { BoardPage } from '../models/board-page';
import { ClosedOutListing } from '../models/closed-out-listing';
import { ListingDetail } from '../models/listing-detail';
import { ListingKind } from '../models/listing-kind';
import { MyListing } from '../models/my-listing';
import { PostGiveListing } from '../models/post-give-listing';
import { PostHelpListing } from '../models/post-help-listing';
import { PostLendListing } from '../models/post-lend-listing';
import { PostSellListing } from '../models/post-sell-listing';
import { PostedListing } from '../models/posted-listing';
import { IListingService } from './listing.service.contract';

/** @inheritdoc */
@Injectable()
export class ListingService implements IListingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  board(kind?: ListingKind, cursor?: string): Promise<BoardPage> {
    let params = new HttpParams();

    if (kind) {
      params = params.set('kind', kind);
    }

    if (cursor) {
      params = params.set('cursor', cursor);
    }

    return firstValueFrom(this.http.get<BoardPage>(`${this.baseUrl}/board`, { params }));
  }

  get(listingId: string): Promise<ListingDetail> {
    return firstValueFrom(this.http.get<ListingDetail>(`${this.baseUrl}/listings/${listingId}`));
  }

  mine(): Promise<readonly MyListing[]> {
    return firstValueFrom(this.http.get<MyListing[]>(`${this.baseUrl}/listings/mine`));
  }

  postLend(listing: PostLendListing): Promise<PostedListing> {
    return firstValueFrom(this.http.post<PostedListing>(`${this.baseUrl}/listings/lend`, listing));
  }

  postGive(listing: PostGiveListing): Promise<PostedListing> {
    return firstValueFrom(this.http.post<PostedListing>(`${this.baseUrl}/listings/give`, listing));
  }

  postSell(listing: PostSellListing): Promise<PostedListing> {
    return firstValueFrom(this.http.post<PostedListing>(`${this.baseUrl}/listings/sell`, listing));
  }

  postHelp(listing: PostHelpListing): Promise<PostedListing> {
    return firstValueFrom(this.http.post<PostedListing>(`${this.baseUrl}/listings/help`, listing));
  }

  closeOut(listingId: string): Promise<ClosedOutListing> {
    return firstValueFrom(
      this.http.post<ClosedOutListing>(`${this.baseUrl}/listings/${listingId}/close-out`, {}),
    );
  }
}
