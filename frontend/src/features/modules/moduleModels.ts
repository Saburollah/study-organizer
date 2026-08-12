export interface StudyModule {
  id: string
  name: string
  code: string | null
  description: string | null
  color: string | null
  createdAtUtc: string
}

export interface SaveModuleRequest {
  name: string
  code?: string | null
  description?: string | null
  color?: string | null
}