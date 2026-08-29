import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { HttpExternalCourseService } from '../externalCourseService'

const subscriptionId = 'course/subscription?one'

const subscription = {
  id: subscriptionId,
  moduleId: '4bf5215a-5bf8-4a3c-9d86-b21fd54aa54e',
  courseName: 'Software Engineering',
  providerKey: 'mock-moodle',
  externalCourseId: 'software-engineering-2026',
  lastScanStatus: 'NeverScanned',
  lastSuccessfulScanAtUtc: null,
}

const content = {
  id: '0b9233c0-d6df-483b-9188-22d38475f487',
  providerContentId: 'exercise-1',
  title: 'Exercise 1',
  description: null,
  sourceUrl: 'https://mock-moodle.local/content/exercise-1',
  dueDateUtc: '2026-09-12T12:00:00Z',
  status: 'TaskCreated' as const,
  reviewReason: null,
  taskId: 'e35ca212-b8a7-42a6-b2c5-b67d5962c5fd',
}

const scanSummary = {
  status: 'Succeeded',
  newContentCount: 2,
  changedContentCount: 1,
  reviewRequiredCount: 1,
  notVisibleCount: 0,
  newTaskEligibleCount: 1,
}

describe('HttpExternalCourseService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('registers a course link', async () => {
    const request = {
      courseUrl: 'https://mock-moodle.local/courses/software-engineering-2026',
    }
    const fetchMock = stubFetch(subscription, 201)

    const result = await new HttpExternalCourseService().register(request)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/course-subscriptions',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(request),
      }),
    )
    expect(result).toEqual(subscription)
  })

  it('loads all subscriptions', async () => {
    const fetchMock = stubFetch([subscription], 200)

    const result = await new HttpExternalCourseService().getAll()

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/course-subscriptions',
      expect.any(Object),
    )
    expect(result).toEqual([subscription])
  })

  it('loads subscription contents with an encoded id', async () => {
    const fetchMock = stubFetch([content], 200)

    const result = await new HttpExternalCourseService()
      .getContents(subscriptionId)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/course-subscriptions/course%2Fsubscription%3Fone/contents',
      expect.any(Object),
    )
    expect(result).toEqual([content])
  })

  it('scans a subscription with an encoded id', async () => {
    const fetchMock = stubFetch(scanSummary, 200)

    const result = await new HttpExternalCourseService().scan(subscriptionId)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/course-subscriptions/course%2Fsubscription%3Fone/scan',
      expect.objectContaining({ method: 'POST' }),
    )
    expect(result).toEqual(scanSummary)
  })
})

function stubFetch(body: unknown, status: number) {
  const fetchMock = vi
    .fn<typeof fetch>()
    .mockResolvedValue(
      new Response(JSON.stringify(body), {
        status,
        headers: {
          'Content-Type': 'application/json',
        },
      }),
    )

  vi.stubGlobal('fetch', fetchMock)

  return fetchMock
}
