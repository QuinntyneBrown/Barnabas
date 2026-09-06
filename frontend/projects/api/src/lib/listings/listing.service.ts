import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { BoardPage } from '../models/board-page';
import { ClosedOutListing } from '../models/closed-out-listing';
import { EditListing } from '../models/edit-listing';
import { ListingDetail } from '../models/listing-detail';
import { ListingKind } from '../models/listing-kind';
import { MyListing } from '../models/my-listing';
import { PostGiveListing } from '../models/post-give-listing';
import { PostHelpListing } from '../models/post-help-listing';
import { PostLendListing } from '../models/post-lend-listing';
import { PostSellListing } from '../models/post-sell-listing';
import { PostedListing } from '../models/posted-listing';
import { AttachedPhoto } from '../models/attached-photo';
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

  mine(includeClosed = false): Promise<readonly MyListing[]> {
    const params = new HttpParams().set('includeClosed', includeClosed);

    return firstValueFrom(this.http.get<MyListing[]>(`${this.baseUrl}/listings/mine`, { params }));
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

  edit(listingId: string, listing: EditListing): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${this.baseUrl}/listings/${listingId}`, listing));
  }

  archive(listingId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/listings/${listingId}/archive`, {}));
  }

  restore(listingId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/listings/${listingId}/restore`, {}));
  }

  remove(listingId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/listings/${listingId}`));
  }

  /**
   * Sends the file as multipart, which is the one place in the client that does.
   *
   * No Content-Type is set by hand: the browser writes it, and the boundary it has to carry is
   * something only the browser knows.
   */
  attachPhoto(listingId: string, photo: File): Promise<AttachedPhoto> {
    const body = new FormData();

    body.append('file', photo, photo.name);

    return firstValueFrom(
      this.http.post<AttachedPhoto>(`${this.baseUrl}/listings/${listingId}/photo`, body),
    );
  }

  closeOut(listingId: string): Promise<ClosedOutListing> {
    return firstValueFrom(
      this.http.post<ClosedOutListing>(`${this.baseUrl}/listings/${listingId}/close-out`, {}),
    );
  }
}
