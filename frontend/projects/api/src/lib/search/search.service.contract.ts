import { InjectionToken } from '@angular/core';

import { SearchFilters, SearchPage } from '../models/search-page';

/** Finding something on the board. */
export interface ISearchService {
  /**
   * Searches the caller's own congregation.
   *
   * It names no congregation: the API takes that from the session, so a search can never reach
   * another parish's board.
   */
  search(term: string, filters?: SearchFilters): Promise<SearchPage>;
}

export const SEARCH_SERVICE = new InjectionToken<ISearchService>('SEARCH_SERVICE');
