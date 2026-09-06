import { AcceptedRequest } from '../models/accepted-request';
import { DeclinedRequest } from '../models/declined-request';
import { IncomingRequest } from '../models/incoming-request';
import { MadeRequest } from '../models/made-request';
import { MakeLoanRequest } from '../models/make-loan-request';
import { MyRequest } from '../models/my-request';

/** Asking for a listing, and deciding what was asked. */
export abstract class IRequestsApi {
  abstract askToBorrow(listingId: string, request: MakeLoanRequest): Promise<MadeRequest>;

  abstract incoming(): Promise<readonly IncomingRequest[]>;

  abstract mine(): Promise<readonly MyRequest[]>;

  abstract accept(requestId: string): Promise<AcceptedRequest>;

  abstract decline(requestId: string): Promise<DeclinedRequest>;
}
