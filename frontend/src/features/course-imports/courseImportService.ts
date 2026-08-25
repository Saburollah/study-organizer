import { apiRequest, apiResponse, type ApiResponse } from '@/services/api/apiClient'

import type { CourseRequestResult, CourseScan, CourseSubscription } from './courseImportModels'

export interface CourseImportService {
  register(moduleId: string, courseUrl: string): Promise<CourseRequestResult<CourseSubscription>>
  get(moduleId: string): Promise<CourseSubscription>
  end(moduleId: string): Promise<void>
  startScan(moduleId: string): Promise<CourseRequestResult<CourseScan>>
  getScan(moduleId: string, scanRunId: string): Promise<CourseScan>
}

export class HttpCourseImportService implements CourseImportService {
  async register(
    moduleId: string,
    courseUrl: string,
  ): Promise<CourseRequestResult<CourseSubscription>> {
    const response = await apiResponse<CourseSubscription>(this.getSubscriptionPath(moduleId), {
      method: 'PUT',
      body: JSON.stringify({ courseUrl }),
    })

    return this.toRequestResult(response)
  }

  get(moduleId: string): Promise<CourseSubscription> {
    return apiRequest<CourseSubscription>(this.getSubscriptionPath(moduleId))
  }

  end(moduleId: string): Promise<void> {
    return apiRequest<void>(this.getSubscriptionPath(moduleId), { method: 'DELETE' })
  }

  async startScan(moduleId: string): Promise<CourseRequestResult<CourseScan>> {
    const response = await apiResponse<CourseScan>(`${this.getSubscriptionPath(moduleId)}/scans`, {
      method: 'POST',
    })

    return this.toRequestResult(response)
  }

  getScan(moduleId: string, scanRunId: string): Promise<CourseScan> {
    return apiRequest<CourseScan>(
      `${this.getSubscriptionPath(moduleId)}/scans/${encodeURIComponent(scanRunId)}`,
    )
  }

  private getSubscriptionPath(moduleId: string): string {
    return `/api/modules/${encodeURIComponent(moduleId)}/course-subscription`
  }

  private getRetryAfterMilliseconds(headers: Headers): number {
    const seconds = Number(headers.get('Retry-After'))

    return Number.isFinite(seconds) && seconds >= 0 ? seconds * 1000 : 1000
  }

  private toRequestResult<T>(response: ApiResponse<T>): CourseRequestResult<T> {
    return {
      data: response.data,
      status: response.status,
      location: response.headers.get('Location'),
      retryAfterMilliseconds: this.getRetryAfterMilliseconds(response.headers),
    }
  }
}

export const courseImportService: CourseImportService = new HttpCourseImportService()
