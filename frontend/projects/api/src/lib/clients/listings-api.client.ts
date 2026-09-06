import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { IListingsApi } from '../contracts/listings-api';
import { BoardPage } from '../models/board-page';
import { ClosedOutListing } from '../models/closed-out-listing';
import { ListingDetail } from '../models/listing-detail';
import { ListingKind } from '../models/listing-kind';
import { MyListing } from '../models/my-listing';
import { PostLendListing } from '../models/post-lend-listing';
import { PostedListing } from '../models/posted-listing';

/** @inheritdoc */
@Injectable()
export class ListingsApi extends IListingsApi {
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

  closeOut(listingId: string): Promise<ClosedOutListing> {
    return firstValueFrom(
      this.http.post<ClosedOutListing>(`${this.baseUrl}/listings/${listingId}/close-out`, {}),
    );
  }
}
