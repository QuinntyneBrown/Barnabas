import { InjectionToken } from '@angular/core';

import {
  DirectoryMember,
  EditMyProfile,
  MemberProfile,
  MyProfile,
} from '../models/member-profile';

/** Members: one's own profile, another's, and the congregation's list of them. */
export interface IMemberService {
  /** The caller's own. It names no member: the API takes that from the session. */
  me(): Promise<MyProfile>;

  edit(profile: EditMyProfile): Promise<MyProfile>;

  /** Leaves the congregation. The session ends with it. */
  leave(): Promise<void>;

  profile(memberId: string): Promise<MemberProfile>;

  directory(search?: { term?: string; helpTag?: string }): Promise<readonly DirectoryMember[]>;
}

export const MEMBER_SERVICE = new InjectionToken<IMemberService>('MEMBER_SERVICE');
