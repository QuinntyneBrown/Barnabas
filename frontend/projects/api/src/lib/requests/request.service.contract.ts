import { InjectionToken } from '@angular/core';

import { AcceptedRequest } from '../models/accepted-request';
import { DeclinedRequest } from '../models/declined-request';
import { IncomingRequest } from '../models/incoming-request';
import { MadeRequest } from '../models/made-request';
import { MakeGiftRequest } from '../models/make-gift-request';
import { MakeHelpRequest } from '../models/make-help-request';
import { MakeLoanRequest } from '../models/make-loan-request';
import { MakePurchaseRequest } from '../models/make-purchase-request';
import { MyRequest } from '../models/my-request';

/** Asking for a listing, and deciding what was asked. */
export interface IRequestService {
  /**
   * One method per kind, named for what the member is actually doing.
   *
   * The four are not interchangeable: borrowing needs a return date, a gift and a purchase need
   * a pickup, and help needs one of the windows the offer declared. A single `ask()` taking a
   * union would let a Help request travel with loan terms, which the API would then refuse -
   * better that it does not compile.
   */
  askToBorrow(listingId: string, request: MakeLoanRequest): Promise<MadeRequest>;

  askForGift(listingId: string, request: MakeGiftRequest): Promise<MadeRequest>;

  askToBuy(listingId: string, request: MakePurchaseRequest): Promise<MadeRequest>;

  askForHelp(listingId: string, request: MakeHelpRequest): Promise<MadeRequest>;

  incoming(): Promise<readonly IncomingRequest[]>;

  mine(): Promise<readonly MyRequest[]>;

  accept(requestId: string): Promise<AcceptedRequest>;

  decline(requestId: string): Promise<DeclinedRequest>;
}

export const REQUEST_SERVICE = new InjectionToken<IRequestService>('REQUEST_SERVICE');
