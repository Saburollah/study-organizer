export type ProfileGender =
  | 'Female'
  | 'Male'
  | 'PreferNotToSay'

export interface UserProfile {
  userId: string
  email: string
  firstName: string | null
  lastName: string | null
  dateOfBirth: string | null
  gender: ProfileGender | null
}

export interface UpdateProfileRequest {
  firstName: string | null
  lastName: string | null
  dateOfBirth: string | null
  gender: ProfileGender | null
}
