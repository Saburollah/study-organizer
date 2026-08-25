import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import type { StudyModule } from '@/features/modules/moduleModels'
import { i18n, setLocale } from '@/i18n'
import { ApiError } from '@/services/api/apiClient'

import CourseRegistrationWizard from '../CourseRegistrationWizard.vue'
import { courseImportService } from '../courseImportService'
import type { CourseSubscription } from '../courseImportModels'

const modules: StudyModule[] = [
  {
    id: 'module-1',
    name: 'Software Engineering',
    code: 'SWE',
    description: null,
    color: '#0c66e4',
    createdAtUtc: '2026-08-25T08:00:00Z',
  },
]

function createSubscription(): CourseSubscription {
  return {
    moduleId: 'module-1',
    status: 'Pending',
    createdAtUtc: '2026-08-25T08:00:00Z',
    activatedAtUtc: null,
    course: {
      displayName: 'Software Engineering',
      sourceType: 'mock-moodle',
      sourceUrl: 'https://example.test/mock-moodle/course/software-engineering',
    },
    latestSnapshot: null,
    latestScan: null,
    recentScans: [],
  }
}

describe('CourseRegistrationWizard', () => {
  beforeEach(() => {
    setLocale('de')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('guides the user from course link through module selection to explicit confirmation', async () => {
    const subscription = createSubscription()
    const registerMock = vi.spyOn(courseImportService, 'register').mockResolvedValue({
      data: subscription,
      status: 202,
      location: '/api/modules/module-1/course-subscription/scans/scan-1',
      retryAfterMilliseconds: 1000,
    })

    const wrapper = mount(CourseRegistrationWizard, {
      props: { modules },
      global: { plugins: [i18n] },
      attachTo: document.body,
    })

    const courseUrl = 'https://example.test/mock-moodle/course/software-engineering'
    await wrapper.get('input[type="url"]').setValue(courseUrl)
    await wrapper.get('.registration-panel').trigger('submit')

    expect(wrapper.text()).toContain('Persönliches Lernmodul auswählen')
    expect(document.activeElement).toBe(wrapper.get('h3').element)

    await wrapper.get('input[type="radio"]').setValue('module-1')
    await wrapper.get('.registration-panel').trigger('submit')

    expect(wrapper.text()).toContain(courseUrl)
    expect(wrapper.text()).toContain('Software Engineering')
    expect(registerMock).not.toHaveBeenCalled()

    await wrapper.get('.registration-panel').trigger('submit')
    await flushPromises()

    expect(registerMock).toHaveBeenCalledWith('module-1', courseUrl)
    expect(wrapper.emitted('registered')).toEqual([[subscription]])
    wrapper.unmount()
  })

  it('keeps an invalid course link on its labeled field and announces the error', async () => {
    const registerMock = vi.spyOn(courseImportService, 'register')
    const wrapper = mount(CourseRegistrationWizard, {
      props: { modules },
      global: { plugins: [i18n] },
    })

    const input = wrapper.get('input[type="url"]')
    await input.setValue('/relative/course')
    await wrapper.get('.registration-panel').trigger('submit')

    expect(wrapper.get('label[for="course-registration-url"]').text()).toBe('Kurslink')
    expect(input.attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('#course-registration-url-error').attributes('role')).toBe('alert')
    expect(wrapper.get('#course-registration-url-error').text()).toContain('vollständigen Kurslink')
    expect(wrapper.text()).not.toContain('Persönliches Lernmodul auswählen')
    expect(registerMock).not.toHaveBeenCalled()
  })

  it('announces a missing module selection at the module choices', async () => {
    const wrapper = mount(CourseRegistrationWizard, {
      props: { modules },
      global: { plugins: [i18n] },
    })

    await wrapper
      .get('input[type="url"]')
      .setValue('https://example.test/mock-moodle/course/software-engineering')
    await wrapper.get('.registration-panel').trigger('submit')
    await wrapper.get('.registration-panel').trigger('submit')

    expect(wrapper.get('.module-options').attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('#course-registration-module-error').attributes('role')).toBe('alert')
    expect(wrapper.get('#course-registration-module-error').text()).toContain('Lernmodul')
  })

  it.each([
    ['unsupported-course-url', 422, 'wird nicht unterstützt'],
    ['module-already-subscribed', 409, 'bereits mit einem Kurs verbunden'],
    ['course-already-subscribed', 409, 'bereits in einem anderen Lernmodul abonniert'],
  ])('shows the %s API error without losing the entered choices', async (code, status, message) => {
    vi.spyOn(courseImportService, 'register').mockRejectedValue(
      new ApiError(status, { title: 'Backend message', code }),
    )
    const wrapper = mount(CourseRegistrationWizard, {
      props: { modules },
      global: { plugins: [i18n] },
    })
    const courseUrl = 'https://example.test/mock-moodle/course/software-engineering'

    await wrapper.get('input[type="url"]').setValue(courseUrl)
    await wrapper.get('.registration-panel').trigger('submit')
    await wrapper.get('input[type="radio"]').setValue('module-1')
    await wrapper.get('.registration-panel').trigger('submit')
    await wrapper.get('.registration-panel').trigger('submit')
    await flushPromises()

    expect(wrapper.get('.registration-error').text()).toContain(message)
    expect(wrapper.text()).toContain(courseUrl)
    expect(wrapper.text()).toContain('Software Engineering')
  })
})
