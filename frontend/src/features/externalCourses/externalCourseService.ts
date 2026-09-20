import { apiRequest } from '@/services/api/apiClient'

import type {
  CourseScanSummary,
  CourseSubscription,
  ExternalCourseContent,
  RegisterCourseRequest,
} from './externalCourseModels'

export interface ExternalCourseService {
  register(request: RegisterCourseRequest): Promise<CourseSubscription>
  getAll(): Promise<CourseSubscription[]>
  getContents(subscriptionId: string): Promise<ExternalCourseContent[]>
  scan(subscriptionId: string): Promise<CourseScanSummary>
}

export class HttpExternalCourseService implements ExternalCourseService {
  register(request: RegisterCourseRequest): Promise<CourseSubscription> {
    return apiRequest<CourseSubscription>(
      '/api/course-subscriptions',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
    )
  }

  getAll(): Promise<CourseSubscription[]> {
    return apiRequest<CourseSubscription[]>(
      '/api/course-subscriptions',
    )
  }

  getContents(subscriptionId: string): Promise<ExternalCourseContent[]> {
    return apiRequest<ExternalCourseContent[]>(
      `${this.getSubscriptionPath(subscriptionId)}/contents`,
    )
  }

  scan(subscriptionId: string): Promise<CourseScanSummary> {
    return apiRequest<CourseScanSummary>(
      `${this.getSubscriptionPath(subscriptionId)}/scan`,
      { method: 'POST' },
    )
  }

  private getSubscriptionPath(subscriptionId: string): string {
    return `/api/course-subscriptions/${encodeURIComponent(subscriptionId)}`
  }
}

export const externalCourseService: ExternalCourseService =
  new HttpExternalCourseService()
