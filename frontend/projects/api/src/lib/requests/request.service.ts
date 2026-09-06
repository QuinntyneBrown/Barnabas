import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { AcceptedRequest } from '../models/accepted-request';
import { DeclinedRequest } from '../models/declined-request';
import { IncomingRequest } from '../models/incoming-request';
import { MadeRequest } from '../models/made-request';
import { MakeLoanRequest } from '../models/make-loan-request';
import { MyRequest } from '../models/my-request';
import { IRequestService } from './request.service.contract';

/** @inheritdoc */
@Injectable()
export class RequestService implements IRequestService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  askToBorrow(listingId: string, request: MakeLoanRequest): Promise<MadeRequest> {
    return firstValueFrom(
      this.http.post<MadeRequest>(`${this.baseUrl}/listings/${listingId}/requests/loan`, request),
    );
  }

  incoming(): Promise<readonly IncomingRequest[]> {
    return firstValueFrom(this.http.get<IncomingRequest[]>(`${this.baseUrl}/requests/incoming`));
  }

  mine(): Promise<readonly MyRequest[]> {
    return firstValueFrom(this.http.get<MyRequest[]>(`${this.baseUrl}/requests/mine`));
  }

  accept(requestId: string): Promise<AcceptedRequest> {
    return firstValueFrom(
      this.http.post<AcceptedRequest>(`${this.baseUrl}/requests/${requestId}/accept`, {}),
    );
  }

  decline(requestId: string): Promise<DeclinedRequest> {
    return firstValueFrom(
      this.http.post<DeclinedRequest>(`${this.baseUrl}/requests/${requestId}/decline`, {}),
    );
  }
}
