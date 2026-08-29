export type ExternalContentStatus =
  | 'TaskCreated'
  | 'ReviewRequired'
  | 'NotVisible'

export interface CourseSubscription {
  id: string
  moduleId: string
  courseName: string
  providerKey: string
  externalCourseId: string
  lastScanStatus: string
  lastSuccessfulScanAtUtc: string | null
}

export interface ExternalCourseContent {
  id: string
  providerContentId: string
  title: string
  description: string | null
  sourceUrl: string
  dueDateUtc: string | null
  status: ExternalContentStatus
  reviewReason: string | null
  taskId: string | null
}

export interface CourseScanSummary {
  status: string
  newContentCount: number
  changedContentCount: number
  reviewRequiredCount: number
  notVisibleCount: number
  newTaskEligibleCount: number
}

export interface RegisterCourseRequest {
  courseUrl: string
}
