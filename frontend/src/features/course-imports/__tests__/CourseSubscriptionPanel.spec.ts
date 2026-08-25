import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import CourseSubscriptionPanel from '../CourseSubscriptionPanel.vue'
import { courseImportService } from '../courseImportService'
import type { CourseScan, CourseSubscription } from '../courseImportModels'

const moduleId = 'module-1'

function createScan(overrides: Partial<CourseScan> = {}): CourseScan {
  return {
    scanRunId: 'scan-1',
    status: 'Succeeded',
    startedAtUtc: '2026-08-25T08:00:00Z',
    completedAtUtc: '2026-08-25T08:00:01Z',
    contentCounts: { new: 3, updated: 1, unchanged: 2, unavailable: 0 },
    personalImpact: {
      tasksCreated: 3,
      pdfTasksCreated: 1,
      nonPdfTasksCreated: 2,
      sourceUpdatesCreated: 1,
    },
    errorCode: null,
    canRetry: false,
    ...overrides,
  }
}

function createSubscription(overrides: Partial<CourseSubscription> = {}): CourseSubscription {
  const latestScan = createScan()
  return {
    moduleId,
    status: 'Active',
    createdAtUtc: '2026-08-25T08:00:00Z',
    activatedAtUtc: '2026-08-25T08:00:01Z',
    course: {
      displayName: 'Software Engineering',
      sourceType: 'mock-moodle',
      sourceUrl: 'https://example.test/mock-moodle/course/software-engineering',
    },
    latestSnapshot: {
      observedAtUtc: '2026-08-25T08:00:01Z',
      knownContentCount: 6,
    },
    latestScan,
    recentScans: [latestScan],
    ...overrides,
  }
}

function mountPanel() {
  return mount(CourseSubscriptionPanel, {
    props: { moduleId },
    global: { plugins: [i18n] },
  })
}

describe('CourseSubscriptionPanel', () => {
  beforeEach(() => {
    setLocale('de')
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('shows the active course, scan status, metrics and newest-first history', async () => {
    vi.spyOn(courseImportService, 'get').mockResolvedValue(createSubscription())

    const wrapper = mountPanel()
    await flushPromises()

    expect(wrapper.get('.subscription-status').text()).toContain('Aktiv')
    expect(wrapper.text()).toContain('Software Engineering')
    expect(wrapper.get('.known-content-count').text()).toContain('6')
    expect(wrapper.get('.latest-scan-result').text()).toContain('3 neue Aufgaben')
    expect(wrapper.findAll('.scan-history-item')).toHaveLength(1)
    expect(wrapper.get('.scan-history-item').text()).toContain('Erfolgreich')
  })

  it('polls one running setup scan until completion and refreshes personal tasks', async () => {
    vi.useFakeTimers()
    const runningScan = createScan({
      status: 'Running',
      completedAtUtc: null,
      contentCounts: { new: 0, updated: 0, unchanged: 0, unavailable: 0 },
      personalImpact: {
        tasksCreated: 0,
        pdfTasksCreated: 0,
        nonPdfTasksCreated: 0,
        sourceUpdatesCreated: 0,
      },
    })
    const pendingSubscription = createSubscription({
      status: 'Pending',
      activatedAtUtc: null,
      latestSnapshot: null,
      latestScan: runningScan,
      recentScans: [runningScan],
    })
    const activeSubscription = createSubscription()
    const getMock = vi
      .spyOn(courseImportService, 'get')
      .mockResolvedValueOnce(pendingSubscription)
      .mockResolvedValueOnce(activeSubscription)
    const getScanMock = vi
      .spyOn(courseImportService, 'getScan')
      .mockResolvedValueOnce(runningScan)
      .mockResolvedValueOnce(createScan())

    const wrapper = mountPanel()
    await flushPromises()

    expect(wrapper.get('.subscription-status').text()).toContain('Ausstehend')
    expect(wrapper.get('[role="status"]').text()).toContain('Scan läuft')

    await vi.advanceTimersByTimeAsync(1000)
    await flushPromises()
    expect(getScanMock).toHaveBeenCalledTimes(1)

    await vi.advanceTimersByTimeAsync(1000)
    await flushPromises()

    expect(getScanMock).toHaveBeenCalledTimes(2)
    expect(getMock).toHaveBeenCalledTimes(2)
    expect(wrapper.get('.subscription-status').text()).toContain('Aktiv')
    expect(wrapper.emitted('scanCompleted')).toHaveLength(1)
  })

  it('keeps a failed setup scan visible and retries it without registering again', async () => {
    vi.useFakeTimers()
    const failedScan = createScan({
      status: 'Failed',
      errorCode: 'source-unreachable',
      canRetry: true,
      personalImpact: {
        tasksCreated: 0,
        pdfTasksCreated: 0,
        nonPdfTasksCreated: 0,
        sourceUpdatesCreated: 0,
      },
    })
    const runningScan = createScan({
      scanRunId: 'scan-2',
      status: 'Running',
      completedAtUtc: null,
      errorCode: null,
      canRetry: false,
    })
    vi.spyOn(courseImportService, 'get').mockResolvedValue(
      createSubscription({
        status: 'Pending',
        activatedAtUtc: null,
        latestSnapshot: null,
        latestScan: failedScan,
        recentScans: [failedScan],
      }),
    )
    const registerMock = vi.spyOn(courseImportService, 'register')
    const startScanMock = vi.spyOn(courseImportService, 'startScan').mockResolvedValue({
      data: runningScan,
      status: 202,
      location: '/api/modules/module-1/course-subscription/scans/scan-2',
      retryAfterMilliseconds: 2500,
    })
    const getScanMock = vi.spyOn(courseImportService, 'getScan').mockResolvedValue(runningScan)

    const wrapper = mountPanel()
    await flushPromises()

    expect(wrapper.get('.scan-failure[role="alert"]').text()).toContain('nicht erreichbar')
    await wrapper.get('.retry-scan-button').trigger('click')
    await flushPromises()

    expect(startScanMock).toHaveBeenCalledWith(moduleId)
    expect(registerMock).not.toHaveBeenCalled()
    expect(wrapper.get('[role="status"]').text()).toContain('Scan läuft')

    await vi.advanceTimersByTimeAsync(2499)
    expect(getScanMock).not.toHaveBeenCalled()
    await vi.advanceTimersByTimeAsync(1)
    await flushPromises()
    expect(getScanMock).toHaveBeenCalledWith(moduleId, 'scan-2')

    wrapper.unmount()
  })

  it('ends the subscription only after confirmation and leaves the task owner informed', async () => {
    vi.spyOn(courseImportService, 'get').mockResolvedValue(createSubscription())
    const endMock = vi.spyOn(courseImportService, 'end').mockResolvedValue()
    const wrapper = mountPanel()
    await flushPromises()

    await wrapper.get('.end-subscription-button').trigger('click')
    expect(wrapper.get('[role="dialog"]').text()).toContain('Software Engineering')

    await wrapper.get('.confirm-end-subscription-button').trigger('click')
    await flushPromises()

    expect(endMock).toHaveBeenCalledWith(moduleId)
    expect(wrapper.find('.course-subscription-panel').exists()).toBe(false)
    expect(wrapper.emitted('ended')).toHaveLength(1)
  })
})
