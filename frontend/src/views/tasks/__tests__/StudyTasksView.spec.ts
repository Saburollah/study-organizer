import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { moduleService } from '@/features/modules/moduleService'
import { courseImportService } from '@/features/course-imports/courseImportService'
import type { ScanRun, CourseSubscription } from '@/features/course-imports/courseImportModels'
import { taskService } from '@/features/tasks/taskService'
import type { StudyTask } from '@/features/tasks/taskModels'
import { i18n, setLocale } from '@/i18n'
import { ApiError } from '@/services/api/apiClient'

import StudyTasksView from '../StudyTasksView.vue'

vi.mock('vue-router', () => ({
  RouterLink: {
    template: '<a><slot /></a>',
  },
}))

const moduleId = 'e6ab31a1-292b-4b31-b65b-dab568512b40'

const studyModule = {
  id: moduleId,
  name: 'Sichere Systeme',
  code: 'SIS',
  description: 'Vorlesung im 4. Semester',
  color: '#3366FF',
  createdAtUtc: '2026-08-12T12:00:00Z',
}

function createTask(overrides: Partial<StudyTask> = {}): StudyTask {
  return {
    id: '90c69198-eccb-4c85-a1d6-ac6a93620b8f',
    moduleId,
    title: 'Kapitel 4 wiederholen',
    description: 'Notizen lesen',
    dueDateUtc: '2026-09-01T18:00:00Z',
    status: 'Open',
    createdAtUtc: '2026-08-13T08:00:00Z',
    updatedAtUtc: null,
    importSource: null,
    ...overrides,
  }
}

function mockPageLoad(tasks: StudyTask[] = []): void {
  vi.spyOn(moduleService, 'getAll').mockResolvedValue([studyModule])
  vi.spyOn(taskService, 'getByModule').mockResolvedValue(tasks)
}

function mountView() {
  return mount(StudyTasksView, {
    props: { moduleId },
    global: {
      plugins: [i18n],
    },
  })
}

describe('StudyTasksView', () => {
  beforeEach(() => {
    setLocale('de')
    vi.spyOn(courseImportService, 'get').mockRejectedValue(new ApiError(404))
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('shows the selected module and its tasks', async () => {
    mockPageLoad([createTask()])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Sichere Systeme')
    expect(wrapper.text()).toContain('Kapitel 4 wiederholen')
    expect(wrapper.findAll('.task-card')).toHaveLength(1)
  })

  it('shows an empty state when no tasks exist', async () => {
    mockPageLoad()

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Noch keine Aufgaben')
  })

  it('shows an imported task without inventing a due date', async () => {
    mockPageLoad([
      createTask({
        title: 'Kursankündigung',
        dueDateUtc: null,
      }),
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Kein Fälligkeitsdatum')
    expect(wrapper.text()).not.toContain('01.01.1970')
  })

  it('shows an error and retries loading', async () => {
    vi.spyOn(moduleService, 'getAll')
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce([studyModule])

    const getTasksMock = vi
      .spyOn(taskService, 'getByModule')
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('konnten nicht geladen werden')

    await wrapper.get('.retry-button').trigger('click')
    await flushPromises()

    expect(getTasksMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Noch keine Aufgaben')
  })

  it('creates a task and adds it to the list', async () => {
    mockPageLoad()

    const createdTask = createTask()
    const createMock = vi.spyOn(taskService, 'create').mockResolvedValue(createdTask)

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.add-task-button').trigger('click')
    await wrapper.get('#task-title').setValue(createdTask.title)
    await wrapper.get('#task-description').setValue(createdTask.description)
    await wrapper.get('#task-due-date').setValue('2026-09-01T20:00')
    await wrapper.get('.task-form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith(moduleId, {
      title: createdTask.title,
      description: createdTask.description,
      dueDateUtc: new Date('2026-09-01T20:00').toISOString(),
    })
    expect(wrapper.text()).toContain('erfolgreich erstellt')
    expect(wrapper.findAll('.task-card')).toHaveLength(1)
  })

  it('updates a task and replaces it in the list', async () => {
    const originalTask = createTask()
    const updatedTask = createTask({
      title: 'Alle Kapitel wiederholen',
      updatedAtUtc: '2026-08-13T09:00:00Z',
    })

    mockPageLoad([originalTask])

    const updateMock = vi.spyOn(taskService, 'update').mockResolvedValue(updatedTask)

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.edit-task-button').trigger('click')
    await wrapper.get('#task-title').setValue(updatedTask.title)
    await wrapper.get('.task-form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith(
      moduleId,
      originalTask.id,
      expect.objectContaining({
        title: updatedTask.title,
      }),
    )
    expect(wrapper.text()).toContain(updatedTask.title)
    expect(wrapper.text()).toContain('erfolgreich aktualisiert')
  })

  it('marks an open task as completed', async () => {
    const openTask = createTask()
    const completedTask = createTask({ status: 'Completed' })

    mockPageLoad([openTask])

    const updateStatusMock = vi.spyOn(taskService, 'updateStatus').mockResolvedValue(completedTask)

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.status-button').trigger('click')
    await flushPromises()

    expect(updateStatusMock).toHaveBeenCalledWith(moduleId, openTask.id, 'Completed')
    expect(wrapper.get('.status-label').text()).toBe('Erledigt')
    expect(wrapper.text()).toContain('als erledigt markiert')
  })

  it('deletes a confirmed task', async () => {
    const task = createTask()
    mockPageLoad([task])

    const deleteMock = vi.spyOn(taskService, 'delete').mockResolvedValue()

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.delete-task-button').trigger('click')

    expect(wrapper.get('[role="dialog"]').text()).toContain(task.title)

    await wrapper.get('.confirm-delete-button').trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith(moduleId, task.id)
    expect(wrapper.find('.task-card').exists()).toBe(false)
    expect(wrapper.text()).toContain('erfolgreich gelöscht')
  })

  it('shows the task page in English', async () => {
    setLocale('en')
    mockPageLoad()

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Back to study modules')
    expect(wrapper.text()).toContain('New task')
    expect(wrapper.text()).toContain('No tasks yet')
  })

  it('shows imported tasks automatically after the running course scan succeeds', async () => {
    vi.useFakeTimers()
    const runningScan: ScanRun = {
      scanRunId: 'scan-1',
      status: 'Running',
      startedAtUtc: '2026-08-25T08:00:00Z',
      completedAtUtc: null,
      contentCounts: { new: 0, updated: 0, unchanged: 0, unavailable: 0 },
      personalImpact: {
        tasksCreated: 0,
        pdfTasksCreated: 0,
        nonPdfTasksCreated: 0,
        sourceUpdatesCreated: 0,
      },
      errorCode: null,
      canRetry: false,
    }
    const succeededScan: ScanRun = {
      ...runningScan,
      status: 'Succeeded',
      completedAtUtc: '2026-08-25T08:00:01Z',
      contentCounts: { new: 1, updated: 0, unchanged: 0, unavailable: 0 },
      personalImpact: {
        tasksCreated: 1,
        pdfTasksCreated: 1,
        nonPdfTasksCreated: 0,
        sourceUpdatesCreated: 0,
      },
    }
    const createCourseSubscription = (
      status: CourseSubscription['status'],
      scan: ScanRun,
    ): CourseSubscription => ({
      moduleId,
      status,
      createdAtUtc: '2026-08-25T08:00:00Z',
      activatedAtUtc: status === 'Active' ? '2026-08-25T08:00:01Z' : null,
      course: {
        displayName: 'Software Engineering',
        sourceType: 'mock-moodle',
        sourceUrl: 'https://example.test/mock-moodle/course/software-engineering',
      },
      latestSnapshot:
        status === 'Active'
          ? { observedAtUtc: '2026-08-25T08:00:01Z', knownContentCount: 1 }
          : null,
      latestScan: scan,
      recentScans: [scan],
    })
    vi.mocked(courseImportService.get)
      .mockResolvedValueOnce(createCourseSubscription('Pending', runningScan))
      .mockResolvedValueOnce(createCourseSubscription('Active', succeededScan))
    vi.spyOn(courseImportService, 'getScan').mockResolvedValue(succeededScan)
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([studyModule])
    vi.spyOn(taskService, 'getByModule')
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([createTask({ title: 'Übungsblatt 05.pdf' })])

    const wrapper = mountView()
    await flushPromises()
    expect(wrapper.text()).toContain('Scan läuft')

    await vi.advanceTimersByTimeAsync(1000)
    await flushPromises()

    expect(wrapper.text()).toContain('Übungsblatt 05.pdf')
    expect(taskService.getByModule).toHaveBeenCalledTimes(2)
  })

  it('keeps imported tasks visible after the course connection is ended', async () => {
    const importedTask = createTask({
      title: 'Übungsblatt 05.pdf',
      importSource: {
        status: 'Available',
        contentType: 'File',
        mediaType: 'application/pdf',
        sourceUrl: 'https://example.test/mock-moodle/content/sheet-05',
        hasSourceUpdate: false,
      },
    })
    mockPageLoad([importedTask])
    vi.mocked(courseImportService.get).mockResolvedValue({
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
        knownContentCount: 1,
      },
      latestScan: null,
      recentScans: [],
    })
    vi.spyOn(courseImportService, 'end').mockResolvedValue()

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('.end-subscription-button').trigger('click')
    await wrapper.get('.confirm-end-subscription-button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Übungsblatt 05.pdf')
    expect(wrapper.text()).toContain('Importierte Aufgaben bleiben erhalten')
  })
})
