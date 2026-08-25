export type CourseSubscriptionStatus = 'Pending' | 'Active'

export type CourseScanStatus = 'Running' | 'Succeeded' | 'Failed' | 'Cancelled' | 'Expired'

export interface CourseScanContentCounts {
  new: number
  updated: number
  unchanged: number
  unavailable: number
}

export interface CourseScanPersonalImpact {
  tasksCreated: number
  pdfTasksCreated: number
  nonPdfTasksCreated: number
  sourceUpdatesCreated: number
}

export interface CourseScan {
  scanRunId: string
  status: CourseScanStatus
  startedAtUtc: string
  completedAtUtc: string | null
  contentCounts: CourseScanContentCounts
  personalImpact: CourseScanPersonalImpact
  errorCode: string | null
  canRetry: boolean
}

export interface CourseSubscription {
  moduleId: string
  status: CourseSubscriptionStatus
  createdAtUtc: string
  activatedAtUtc: string | null
  course: {
    displayName: string
    sourceType: string
    sourceUrl: string | null
  }
  latestSnapshot: {
    observedAtUtc: string
    knownContentCount: number
  } | null
  latestScan: CourseScan | null
  recentScans: CourseScan[]
}

export interface CourseRequestResult<T> {
  data: T
  status: number
  location: string | null
  retryAfterMilliseconds: number
}
