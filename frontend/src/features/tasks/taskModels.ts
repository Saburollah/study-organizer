export type StudyTaskStatus = 'Open' | 'Completed'

export interface ExternalTaskSource {
  providerKey: string
  courseName: string
  sourceUrl: string
}

export interface StudyTask {
  id: string
  moduleId: string
  title: string
  description: string | null
  dueDateUtc: string
  status: StudyTaskStatus
  createdAtUtc: string
  updatedAtUtc: string | null
  externalSource: ExternalTaskSource | null
}

export interface SaveStudyTaskRequest {
  title: string
  description?: string | null
  dueDateUtc: string
}

export interface UpdateStudyTaskStatusRequest {
  status: StudyTaskStatus
}
