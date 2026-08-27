export type StudyTaskStatus = 'Open' | 'Completed'

export interface StudyTask {
  id: string
  moduleId: string
  title: string
  description: string | null
  dueDateUtc: string | null
  status: StudyTaskStatus
  createdAtUtc: string
  updatedAtUtc: string | null
  importSource: StudyTaskImportSource | null
}

export interface StudyTaskImportSource {
  status: 'Available' | 'Unavailable' | 'SubscriptionEnded' | 'MetadataPurged'
  contentType: string | null
  mediaType: string | null
  sourceUrl: string | null
  hasSourceUpdate: boolean
}

export interface SaveStudyTaskRequest {
  title: string
  description?: string | null
  dueDateUtc: string | null
}

export interface UpdateStudyTaskStatusRequest {
  status: StudyTaskStatus
}
