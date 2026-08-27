export type CourseSubscriptionStatus = 'Pending' | 'Active'

export type ScanRunStatus = 'Running' | 'Succeeded' | 'Failed' | 'Cancelled' | 'Expired'

export interface ScanRunContentCounts {
  new: number
  updated: number
  unchanged: number
  unavailable: number
}

export interface ScanRunPersonalImpact {
  tasksCreated: number
  pdfTasksCreated: number
  nonPdfTasksCreated: number
  sourceUpdatesCreated: number
}

export interface ScanRun {
  scanRunId: string
  status: ScanRunStatus
  startedAtUtc: string
  completedAtUtc: string | null
  contentCounts: ScanRunContentCounts
  personalImpact: ScanRunPersonalImpact
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
  latestScan: ScanRun | null
  recentScans: ScanRun[]
}

export interface CourseRequestResult<T> {
  data: T
  status: number
  location: string | null
  retryAfterMilliseconds: number
}
