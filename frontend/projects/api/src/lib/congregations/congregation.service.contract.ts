import { InjectionToken } from '@angular/core';

import { Congregation } from '../models/congregation';

/** The congregation a member belongs to. */
export interface ICongregationService {
  /**
   * The caller's own congregation.
   *
   * It names none: the API takes the identifier from the verified session, so there is nothing
   * here that could ask about another parish.
   */
  current(): Promise<Congregation>;
}

export const CONGREGATION_SERVICE = new InjectionToken<ICongregationService>('CONGREGATION_SERVICE');
