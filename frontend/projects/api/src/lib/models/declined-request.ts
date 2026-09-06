import { RequestStatus } from './request-status';

/** The decision, so the screen can show the request as settled. */
export interface DeclinedRequest {
  readonly requestId: string;
  readonly status: RequestStatus;
}
