import { afterEach, describe, expect, it, vi } from 'vitest'

import { HttpCourseImportService } from '../courseImportService'

describe('HttpCourseImportService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('registers a course and exposes polling metadata from an accepted response', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify({
          moduleId: 'module-1',
          status: 'Pending',
          latestScan: { scanRunId: 'scan-1', status: 'Running' },
        }),
        {
          status: 202,
          headers: {
            'Content-Type': 'application/json',
            Location: '/api/modules/module-1/course-subscription/scans/scan-1',
            'Retry-After': '1',
          },
        },
      ),
    )

    vi.stubGlobal('fetch', fetchMock)

    const result = await new HttpCourseImportService().register(
      'module-1',
      'https://example.test/mock-moodle/course/software-engineering',
    )

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/modules/module-1/course-subscription'),
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({
          courseUrl: 'https://example.test/mock-moodle/course/software-engineering',
        }),
      }),
    )
    expect(result).toMatchObject({
      status: 202,
      location: '/api/modules/module-1/course-subscription/scans/scan-1',
      retryAfterMilliseconds: 1000,
      data: {
        moduleId: 'module-1',
        status: 'Pending',
      },
    })
  })

  it('uses the personal subscription and scan resource endpoints', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(Response.json({ status: 'Active' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(Response.json({ scanRunId: 'scan-1', status: 'Succeeded' }))
      .mockResolvedValueOnce(Response.json({ scanRunId: 'scan-1', status: 'Succeeded' }))

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpCourseImportService()
    await service.get('module/1')
    await service.end('module/1')
    await service.startScan('module/1')
    await service.getScan('module/1', 'scan/1')

    expect(fetchMock.mock.calls.map(([url, options]) => [url, options?.method ?? 'GET'])).toEqual([
      [expect.stringContaining('/api/modules/module%2F1/course-subscription'), 'GET'],
      [expect.stringContaining('/api/modules/module%2F1/course-subscription'), 'DELETE'],
      [expect.stringContaining('/api/modules/module%2F1/course-subscription/scans'), 'POST'],
      [
        expect.stringContaining('/api/modules/module%2F1/course-subscription/scans/scan%2F1'),
        'GET',
      ],
    ])
  })
})
