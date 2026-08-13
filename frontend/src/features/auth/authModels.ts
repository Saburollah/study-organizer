export interface RegisterUserRequest {
  email: string
  password: string
}

export interface RegisterUserResponse {
  userId: string
  email: string
}

export interface LoginUserRequest {
  email: string
  password: string
}

export interface LoginUserResponse {
  accessToken: string
  expiresAtUtc: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AuthSession {
  email: string
  accessToken: string
  expiresAtUtc: string
}
