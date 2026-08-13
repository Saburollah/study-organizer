import { apiRequest } from '@/services/api/apiClient'

import type {
  ChangePasswordRequest,
  LoginUserRequest,
  LoginUserResponse,
  RegisterUserRequest,
  RegisterUserResponse,
} from './authModels'

export interface AuthService {
  register(
    request: RegisterUserRequest,
  ): Promise<RegisterUserResponse>

  login(
    request: LoginUserRequest,
  ): Promise<LoginUserResponse>

  changePassword(
    request: ChangePasswordRequest,
  ): Promise<void>
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

  login(
    request: LoginUserRequest,
  ): Promise<LoginUserResponse> {
    return apiRequest<LoginUserResponse>(
      '/api/auth/login',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
    )
  }

  changePassword(
    request: ChangePasswordRequest,
  ): Promise<void> {
    return apiRequest<void>(
      '/api/auth/password',
      {
        method: 'PUT',
        body: JSON.stringify(request),
      },
    )
  }
}

export const authService: AuthService =
  new HttpAuthService()
