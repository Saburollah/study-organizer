export interface RegisterUserRequest {
  email: string
  password: string
}

export interface RegisterUserResponse {
  userId: string
  email: string
}
