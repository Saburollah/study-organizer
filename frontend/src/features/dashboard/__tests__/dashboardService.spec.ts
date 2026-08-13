import { beforeEach, describe, expect, it, vi } from 'vitest'

import { moduleService } from '@/features/modules/moduleService'
import { taskService } from '@/features/tasks/taskService'

import { dashboardService } from '../dashboardService'

describe('dashboardService', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('combines modules and their tasks', async () => {
    vi.spyOn(moduleService, 'getAll').mockResolvedValue([
      {
        id: 'module-1',
        name: 'Sichere Systeme',
        code: 'SIS',
        description: null,
        color: null,
        createdAtUtc: '2026-08-13T00:00:00Z',
      },
    ])

    vi.spyOn(taskService, 'getByModule').mockResolvedValue([
      {
        id: 'task-1',
        moduleId: 'module-1',
        title: 'Projekt abgeben',
        description: null,
        dueDateUtc: '2026-08-20T12:00:00Z',
        status: 'Open',
        createdAtUtc: '2026-08-13T00:00:00Z',
        updatedAtUtc: null,
      },
    ])

    const result = await dashboardService.getDashboard()

    expect(result.moduleCount).toBe(1)
    expect(result.tasks).toEqual([
      {
        id: 'task-1',
        moduleId: 'module-1',
        moduleName: 'Sichere Systeme',
        moduleCode: 'SIS',
        title: 'Projekt abgeben',
        dueDateUtc: '2026-08-20T12:00:00Z',
        status: 'Open',
      },
    ])

    expect(taskService.getByModule).toHaveBeenCalledWith(
      'module-1',
    )
  })
})