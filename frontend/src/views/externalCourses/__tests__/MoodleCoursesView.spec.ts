import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { externalCourseService } from '@/features/externalCourses/externalCourseService'
import type {
  CourseSubscription,
  ExternalCourseContent,
} from '@/features/externalCourses/externalCourseModels'
import { i18n, setLocale } from '@/i18n'
import { ApiError } from '@/services/api/apiClient'

import MoodleCoursesView from '../MoodleCoursesView.vue'

const subscription: CourseSubscription = {
  id: '557c704a-1106-413b-8d5b-dba66552a8d8',
  moduleId: '4bf5215a-5bf8-4a3c-9d86-b21fd54aa54e',
  courseName: 'Software Engineering',
  providerKey: 'mock-moodle',
  externalCourseId: 'software-engineering-2026',
  lastScanStatus: 'Succeeded',
  lastSuccessfulScanAtUtc: '2026-08-28T08:00:00Z',
}

const contents: ExternalCourseContent[] = [
  {
    id: 'content-task',
    providerContentId: 'exercise-1',
    title: 'Exercise 1',
    description: null,
    sourceUrl: 'https://mock-moodle.local/content/exercise-1',
    dueDateUtc: '2026-09-12T12:00:00Z',
    status: 'TaskCreated',
    reviewReason: null,
    taskId: 'task-1',
  },
  {
    id: 'content-review',
    providerContentId: 'announcement-1',
    title: 'Important announcement',
    description: null,
    sourceUrl: 'https://mock-moodle.local/content/announcement-1',
    dueDateUtc: null,
    status: 'ReviewRequired',
    reviewReason: 'MissingStructuredDueDate',
    taskId: null,
  },
  {
    id: 'content-hidden',
    providerContentId: 'exercise-old',
    title: 'Old exercise',
    description: null,
    sourceUrl: 'https://mock-moodle.local/content/exercise-old',
    dueDateUtc: '2026-09-01T12:00:00Z',
    status: 'NotVisible',
    reviewReason: null,
    taskId: 'task-old',
  },
]

describe('MoodleCoursesView', () => {
  beforeEach(() => {
    setLocale('de')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountView() {
    return mount(MoodleCoursesView, {
      global: {
        plugins: [i18n],
        stubs: {
          RouterLink: {
            name: 'RouterLink',
            props: ['to'],
            template: '<a><slot /></a>',
          },
        },
      },
    })
  }

  it('shows a loading state while subscriptions are loaded', () => {
    vi.spyOn(externalCourseService, 'getAll')
      .mockReturnValue(new Promise(() => undefined))

    const wrapper = mountView()

    expect(wrapper.text()).toContain('Moodle-Kurse werden geladen')
  })

  it('shows an empty state when no subscriptions exist', async () => {
    vi.spyOn(externalCourseService, 'getAll').mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Noch keine Moodle-Kurse')
  })

  it('registers a course, loads its contents, and shows success', async () => {
    vi.spyOn(externalCourseService, 'getAll').mockResolvedValue([])
    const registerMock = vi.spyOn(externalCourseService, 'register')
      .mockResolvedValue(subscription)
    const contentsMock = vi.spyOn(externalCourseService, 'getContents')
      .mockResolvedValue([])
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('#course-url').setValue(
      'https://mock-moodle.local/courses/software-engineering-2026',
    )
    await wrapper.get('.course-registration-form').trigger('submit')
    await flushPromises()

    expect(registerMock).toHaveBeenCalledWith({
      courseUrl:
        'https://mock-moodle.local/courses/software-engineering-2026',
    })
    expect(contentsMock).toHaveBeenCalledWith(subscription.id)
    expect(wrapper.text()).toContain('Software Engineering')
    expect(wrapper.text()).toContain('erfolgreich registriert')
  })

  it('scans and reloads contents before showing summary counts', async () => {
    vi.spyOn(externalCourseService, 'getAll').mockResolvedValue([subscription])
    const contentsMock = vi.spyOn(externalCourseService, 'getContents')
      .mockResolvedValueOnce(contents.slice(0, 1))
      .mockResolvedValueOnce(contents)
    const scanMock = vi.spyOn(externalCourseService, 'scan').mockResolvedValue({
      status: 'Succeeded',
      newContentCount: 2,
      changedContentCount: 1,
      reviewRequiredCount: 1,
      notVisibleCount: 1,
      newTaskEligibleCount: 1,
    })
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.scan-course-button').trigger('click')
    await flushPromises()

    expect(scanMock).toHaveBeenCalledWith(subscription.id)
    expect(contentsMock).toHaveBeenCalledTimes(2)
    expect(contentsMock).toHaveBeenLastCalledWith(subscription.id)
    const summary = wrapper.get('.scan-summary')
    expect(summary.text()).toContain('Neue Inhalte:')
    expect(summary.text()).toContain('Geändert:')
    expect(summary.text()).toContain('Prüfung:')
    expect(summary.text()).toContain('Nicht sichtbar:')
    expect(summary.text()).toContain('Neue Aufgaben:')
    expect(summary.findAll('dd').map((item) => item.text())).toEqual([
      '2',
      '1',
      '1',
      '1',
      '1',
    ])
  })

  it('renders statuses, safe external links, and the personal module link', async () => {
    vi.spyOn(externalCourseService, 'getAll').mockResolvedValue([subscription])
    vi.spyOn(externalCourseService, 'getContents').mockResolvedValue(contents)
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('.content-status-task-created').exists()).toBe(true)
    expect(wrapper.find('.content-status-review-required').exists()).toBe(true)
    expect(wrapper.find('.content-status-not-visible').exists()).toBe(true)
    const sourceLink = wrapper.get('.external-content-link')
    expect(sourceLink.attributes('target')).toBe('_blank')
    expect(sourceLink.attributes('rel')).toBe('noopener noreferrer')
    const moduleLink = wrapper.getComponent({ name: 'RouterLink' })
    expect(moduleLink.classes()).toContain('course-module-link')
    expect(moduleLink.props('to')).toEqual({
      name: 'module-tasks',
      params: { moduleId: subscription.moduleId },
    })
  })

  it('shows a safe API error detail', async () => {
    vi.spyOn(externalCourseService, 'getAll').mockRejectedValue(
      new ApiError(502, { detail: 'external_timeout' }),
    )

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('external_timeout')
  })

  it('shows the course page in English', async () => {
    setLocale('en')
    vi.spyOn(externalCourseService, 'getAll').mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Moodle courses')
    expect(wrapper.text()).toContain('Register course')
    expect(wrapper.text()).toContain('No Moodle courses yet')
  })
})
