import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import { Congregation } from '../models/congregation';
import { ICongregationService } from './congregation.service.contract';

/** @inheritdoc */
@Injectable()
export class CongregationService implements ICongregationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  current(): Promise<Congregation> {
    return firstValueFrom(this.http.get<Congregation>(`${this.baseUrl}/congregation`));
  }
}
