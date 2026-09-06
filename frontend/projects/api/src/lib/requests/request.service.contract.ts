import { InjectionToken } from '@angular/core';

import { AcceptedRequest } from '../models/accepted-request';
import { DeclinedRequest } from '../models/declined-request';
import { IncomingRequest } from '../models/incoming-request';
import { MadeRequest } from '../models/made-request';
import { MakeLoanRequest } from '../models/make-loan-request';
import { MyRequest } from '../models/my-request';

/** Asking for a listing, and deciding what was asked. */
export interface IRequestService {
  askToBorrow(listingId: string, request: MakeLoanRequest): Promise<MadeRequest>;

  incoming(): Promise<readonly IncomingRequest[]>;

  mine(): Promise<readonly MyRequest[]>;

  accept(requestId: string): Promise<AcceptedRequest>;

  decline(requestId: string): Promise<DeclinedRequest>;
}

export const REQUEST_SERVICE = new InjectionToken<IRequestService>('REQUEST_SERVICE');
