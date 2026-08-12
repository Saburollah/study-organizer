import { apiRequest } from '@/services/api/apiClient'

import type {
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
}

export const authService: AuthService =
  new HttpAuthService()