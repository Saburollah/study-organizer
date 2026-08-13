import { apiRequest } from '@/services/api/apiClient'

import type {
  UpdateProfileRequest,
  UserProfile,
} from './profileModels'

export interface ProfileService {
  get(): Promise<UserProfile>

  update(
    request: UpdateProfileRequest,
  ): Promise<UserProfile>
}

export class HttpProfileService
implements ProfileService {
  get(): Promise<UserProfile> {
    return apiRequest<UserProfile>(
      '/api/profile/',
    )
  }

  update(
    request: UpdateProfileRequest,
  ): Promise<UserProfile> {
    return apiRequest<UserProfile>(
      '/api/profile/',
      {
        method: 'PUT',
        body: JSON.stringify(request),
      },
    )
  }
}

export const profileService: ProfileService =
  new HttpProfileService()
