import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { SearchFilters, SearchPage } from '../models/search-page';
import { ISearchService } from './search.service.contract';

/** @inheritdoc */
@Injectable()
export class SearchService implements ISearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  search(term: string, filters?: SearchFilters): Promise<SearchPage> {
    let params = new HttpParams().set('term', term);

    if (filters?.kind) {
      params = params.set('kind', filters.kind);
    }

    if (filters?.neighbourhood) {
      params = params.set('neighbourhood', filters.neighbourhood);
    }

    if (filters?.maxPrice !== undefined) {
      params = params.set('maxPrice', filters.maxPrice);
    }

    return firstValueFrom(this.http.get<SearchPage>(`${this.baseUrl}/search`, { params }));
  }
}
