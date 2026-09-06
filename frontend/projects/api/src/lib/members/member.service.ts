import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../api-base-url';
import {
  DirectoryMember,
  EditMyProfile,
  MemberProfile,
  MyProfile,
} from '../models/member-profile';
import { IMemberService } from './member.service.contract';

/** @inheritdoc */
@Injectable()
export class MemberService implements IMemberService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  me(): Promise<MyProfile> {
    return firstValueFrom(this.http.get<MyProfile>(`${this.baseUrl}/members/me`));
  }

  edit(profile: EditMyProfile): Promise<MyProfile> {
    return firstValueFrom(this.http.put<MyProfile>(`${this.baseUrl}/members/me`, profile));
  }

  leave(): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/members/me/leave`, {}));
  }

  profile(memberId: string): Promise<MemberProfile> {
    return firstValueFrom(this.http.get<MemberProfile>(`${this.baseUrl}/members/${memberId}`));
  }

  directory(search?: { term?: string; helpTag?: string }): Promise<readonly DirectoryMember[]> {
    let params = new HttpParams();

    if (search?.term) {
      params = params.set('term', search.term);
    }

    if (search?.helpTag) {
      params = params.set('helpTag', search.helpTag);
    }

    return firstValueFrom(this.http.get<DirectoryMember[]>(`${this.baseUrl}/directory`, { params }));
  }
}
