import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { dashboardService } from '@/features/dashboard/dashboardService'
import { i18n, setLocale } from '@/i18n'

import DashboardView from '../DashboardView.vue'

vi.mock('vue-router', () => ({
  RouterLink: {
    props: ['to'],
    template: '<a><slot /></a>',
  },
}))

describe('DashboardView', () => {
  beforeEach(() => {
    setLocale('de')
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  function mountView() {
    return mount(DashboardView, {
      global: {
        plugins: [i18n],
      },
    })
  }

  it('shows task summaries and the next open tasks', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-08-13T12:00:00Z'))

    vi.spyOn(dashboardService, 'getDashboard').mockResolvedValue({
      moduleCount: 2,
      tasks: [
        {
          id: 'task-overdue',
          moduleId: 'module-1',
          moduleName: 'Sichere Systeme',
          moduleCode: 'SIS',
          title: 'Projekt abgeben',
          dueDateUtc: '2026-08-12T12:00:00Z',
          status: 'Open',
        },
        {
          id: 'task-next',
          moduleId: 'module-2',
          moduleName: 'Datenbanken',
          moduleCode: 'DB',
          title: 'SQL üben',
          dueDateUtc: '2026-08-15T12:00:00Z',
          status: 'Open',
        },
        {
          id: 'task-completed',
          moduleId: 'module-1',
          moduleName: 'Sichere Systeme',
          moduleCode: 'SIS',
          title: 'Kapitel lesen',
          dueDateUtc: '2026-08-11T12:00:00Z',
          status: 'Completed',
        },
      ],
    })

    const wrapper = mountView()
    await flushPromises()

    const summaries = wrapper.findAll('.summary-card').map((card) => card.text())

    expect(summaries).toEqual(['Lernmodule2', 'Offene Aufgaben2', 'Überfällig1', 'Erledigt1'])
    expect(wrapper.text()).toContain('Projekt abgeben')
    expect(wrapper.text()).toContain('SQL üben')
    expect(wrapper.text()).not.toContain('Kapitel lesen')
    expect(wrapper.findAll('.task-row')).toHaveLength(2)
    expect(wrapper.findAll('.task-row.overdue')).toHaveLength(1)
  })

  it('shows an empty state without open tasks', async () => {
    vi.spyOn(dashboardService, 'getDashboard').mockResolvedValue({
      moduleCount: 0,
      tasks: [],
    })

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Keine offenen Aufgaben')
  })

  it('shows an open task without a due date using a translated label', async () => {
    vi.spyOn(dashboardService, 'getDashboard').mockResolvedValue({
      moduleCount: 1,
      tasks: [
        {
          id: 'task-without-due-date',
          moduleId: 'module-1',
          moduleName: 'Dashboard Diagnose',
          moduleCode: null,
          title: 'Aufgabe ohne Fälligkeit',
          dueDateUtc: null,
          status: 'Open',
        },
      ],
    })

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Ohne Fälligkeit')
    expect(wrapper.text()).not.toContain('dashboard.next.noDueDate')
  })

  it('shows an error and retries loading', async () => {
    const getDashboardMock = vi
      .spyOn(dashboardService, 'getDashboard')
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce({
        moduleCount: 0,
        tasks: [],
      })

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('konnte nicht geladen werden')

    await wrapper.get('.error-state button').trigger('click')
    await flushPromises()

    expect(getDashboardMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Keine offenen Aufgaben')
  })

  it('shows the dashboard in English', async () => {
    setLocale('en')
    vi.spyOn(dashboardService, 'getDashboard').mockResolvedValue({
      moduleCount: 0,
      tasks: [],
    })

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('YOUR OVERVIEW')
    expect(wrapper.text()).toContain('Manage study modules')
    expect(wrapper.text()).toContain('Open tasks')
    expect(wrapper.text()).toContain('No open tasks')
    expect(wrapper.text()).not.toContain('Lernmodule verwalten')
  })
})
