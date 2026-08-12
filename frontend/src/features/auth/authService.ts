import { apiRequest } from '@/services/api/apiClient'

import type {
  RegisterUserRequest,
  RegisterUserResponse,
} from './authModels'

export interface AuthService {
  register(
    request: RegisterUserRequest,
  ): Promise<RegisterUserResponse>
}

export class HttpAuthService implements AuthService {
  register(
    request: RegisterUserRequest,
  ): Promise<RegisterUserResponse> {
    return apiRequest<RegisterUserResponse>(
      '/api/auth/register',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
    )
  }
}

export const authService: AuthService =
  new HttpAuthService()