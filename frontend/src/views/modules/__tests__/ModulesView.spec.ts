import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { moduleService } from '@/features/modules/moduleService'
import { courseImportService } from '@/features/course-imports/courseImportService'
import type { CourseSubscription } from '@/features/course-imports/courseImportModels'
import { i18n, setLocale } from '@/i18n'

import ModulesView from '../ModulesView.vue'

const { pushMock } = vi.hoisted(() => ({
  pushMock: vi.fn<(location: unknown) => Promise<void>>().mockResolvedValue(),
}))

vi.mock('vue-router', () => ({
  RouterLink: {
    template: '<a><slot /></a>',
  },
  useRouter: () => ({ push: pushMock }),
}))

describe('ModulesView', () => {
  beforeEach(() => {
    setLocale('de')
    pushMock.mockReset()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountView() {
    return mount(ModulesView, {
      global: {
        plugins: [i18n],
      },
    })
  }

  it('shows a loading state while modules are loaded', () => {
    vi.spyOn(moduleService, 'getAll').mockReturnValue(new Promise(() => undefined))

    const wrapper = mountView()

    expect(wrapper.text()).toContain('Lernmodule werden geladen')
  })

  it('shows an empty state when no modules exist', async () => {
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Noch keine Lernmodule')
  })

  it('shows the modules returned by the API', async () => {
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([
      {
        id: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
        name: 'Sichere Systeme',
        code: 'SIS',
        description: 'Vorlesung im 4. Semester',
        color: '#3366FF',
        createdAtUtc: '2026-08-12T12:00:00Z',
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Sichere Systeme')
    expect(wrapper.text()).toContain('SIS')
    expect(wrapper.findAll('.module-card')).toHaveLength(1)
  })

  it('starts course registration and opens the selected module after confirmation', async () => {
    const moduleId = 'e6ab31a1-292b-4b31-b65b-dab568512b40'
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([
      {
        id: moduleId,
        name: 'Software Engineering',
        code: 'SWE',
        description: null,
        color: '#0c66e4',
        createdAtUtc: '2026-08-25T08:00:00Z',
      },
    ])
    const subscription = {
      moduleId,
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
    } satisfies CourseSubscription
    vi.spyOn(courseImportService, 'register').mockResolvedValue({
      data: subscription,
      status: 202,
      location: null,
      retryAfterMilliseconds: 1000,
    })

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.connect-course-button').trigger('click')
    await wrapper.get('input[type="url"]').setValue(subscription.course.sourceUrl)
    await wrapper.get('.registration-panel').trigger('submit')
    await wrapper.get('input[type="radio"]').setValue(moduleId)
    await wrapper.get('.registration-panel').trigger('submit')
    await wrapper.get('.registration-panel').trigger('submit')
    await flushPromises()

    expect(pushMock).toHaveBeenCalledWith({
      name: 'module-tasks',
      params: { moduleId },
    })
  })

  it('shows an error and can retry loading', async () => {
    const getAllMock = vi
      .spyOn(moduleService, 'getAll')
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('konnten nicht geladen werden')

    await wrapper.get('.retry-button').trigger('click')
    await flushPromises()

    expect(getAllMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Noch keine Lernmodule')
  })

  it('creates a module and adds it to the list', async () => {
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([])

    const createMock = vi.spyOn(moduleService, 'create').mockResolvedValue({
      id: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
      name: 'Datenbanken',
      code: 'DB',
      description: 'SQL und PostgreSQL',
      color: '#FF8800',
      createdAtUtc: '2026-08-12T12:00:00Z',
    })

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.add-module-button').trigger('click')
    await wrapper.get('#module-name').setValue('Datenbanken')
    await wrapper.get('#module-code').setValue('DB')
    await wrapper.get('#module-description').setValue('SQL und PostgreSQL')
    await wrapper.get('#module-color').setValue('#ff8800')
    await wrapper.get('.module-form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith({
      name: 'Datenbanken',
      code: 'DB',
      description: 'SQL und PostgreSQL',
      color: '#ff8800',
    })

    expect(wrapper.text()).toContain('Datenbanken')
    expect(wrapper.text()).toContain('erfolgreich erstellt')
    expect(wrapper.find('.module-form').exists()).toBe(false)
  })

  it('updates a module and replaces it in the list', async () => {
    const moduleId = 'e6ab31a1-292b-4b31-b65b-dab568512b40'

    vi.spyOn(moduleService, 'getAll').mockResolvedValue([
      {
        id: moduleId,
        name: 'Datenbanken',
        code: 'DB',
        description: 'SQL',
        color: '#FF8800',
        createdAtUtc: '2026-08-12T12:00:00Z',
      },
    ])

    const updateMock = vi.spyOn(moduleService, 'update').mockResolvedValue({
      id: moduleId,
      name: 'Datenbanken 2',
      code: 'DB2',
      description: 'SQL und PostgreSQL',
      color: '#3366FF',
      createdAtUtc: '2026-08-12T12:00:00Z',
    })

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.edit-module-button').trigger('click')

    expect(wrapper.get('#module-name').element).toHaveProperty('value', 'Datenbanken')

    await wrapper.get('#module-name').setValue('Datenbanken 2')
    await wrapper.get('#module-code').setValue('DB2')
    await wrapper.get('#module-description').setValue('SQL und PostgreSQL')
    await wrapper.get('#module-color').setValue('#3366ff')
    await wrapper.get('.module-form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith(moduleId, {
      name: 'Datenbanken 2',
      code: 'DB2',
      description: 'SQL und PostgreSQL',
      color: '#3366ff',
    })

    expect(wrapper.text()).toContain('Datenbanken 2')
    expect(wrapper.text()).toContain('erfolgreich aktualisiert')
    expect(wrapper.find('.module-form').exists()).toBe(false)
  })

  it('does not delete a module when confirmation is cancelled', async () => {
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([
      {
        id: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
        name: 'Datenbanken',
        code: 'DB',
        description: null,
        color: '#FF8800',
        createdAtUtc: '2026-08-12T12:00:00Z',
      },
    ])

    const deleteMock = vi.spyOn(moduleService, 'delete')
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.delete-module-button').trigger('click')

    expect(wrapper.get('[role="dialog"]').text()).toContain('Datenbanken')

    await wrapper.get('.cancel-delete-button').trigger('click')

    expect(deleteMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Datenbanken')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })

  it('deletes a confirmed module and removes it from the list', async () => {
    const moduleId = 'e6ab31a1-292b-4b31-b65b-dab568512b40'

    vi.spyOn(moduleService, 'getAll').mockResolvedValue([
      {
        id: moduleId,
        name: 'Datenbanken',
        code: 'DB',
        description: null,
        color: '#FF8800',
        createdAtUtc: '2026-08-12T12:00:00Z',
      },
    ])

    const deleteMock = vi.spyOn(moduleService, 'delete').mockResolvedValue()

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.delete-module-button').trigger('click')
    await wrapper.get('.confirm-delete-button').trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith(moduleId)
    expect(wrapper.find('.module-card').exists()).toBe(false)
    expect(wrapper.text()).toContain('Noch keine Lernmodule')
    expect(wrapper.text()).toContain('erfolgreich gelöscht')
  })

  it('shows the module page in English', async () => {
    setLocale('en')
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Study modules')
    expect(wrapper.text()).toContain('New study module')
    expect(wrapper.text()).toContain('No study modules yet')
  })
})
