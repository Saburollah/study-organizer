import { afterEach, describe, expect, it, vi } from 'vitest'

import { HttpTaskService } from '../taskService'

const moduleId = 'e6ab31a1-292b-4b31-b65b-dab568512b40'
const taskId = '90c69198-eccb-4c85-a1d6-ac6a93620b8f'

const task = {
  id: taskId,
  moduleId,
  title: 'Kapitel 4 wiederholen',
  description: 'Notizen lesen',
  dueDateUtc: '2026-09-01T18:00:00Z',
  status: 'Open' as const,
  createdAtUtc: '2026-08-13T08:00:00Z',
  updatedAtUtc: null,
  importSource: null,
}

describe('HttpTaskService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads tasks for a module', async () => {
    const fetchMock = stubFetch([task], 200)
    const service = new HttpTaskService()

    const result = await service.getByModule(moduleId)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}/tasks/`,
      expect.any(Object),
    )
    expect(result).toEqual([task])
  })

  it('creates a task', async () => {
    const fetchMock = stubFetch(task, 201)
    const service = new HttpTaskService()
    const request = {
      title: task.title,
      description: task.description,
      dueDateUtc: task.dueDateUtc,
    }

    const result = await service.create(moduleId, request)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}/tasks/`,
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(request),
      }),
    )
    expect(result).toEqual(task)
  })

  it('updates a task', async () => {
    const fetchMock = stubFetch(task, 200)
    const service = new HttpTaskService()
    const request = {
      title: task.title,
      description: task.description,
      dueDateUtc: task.dueDateUtc,
    }

    const result = await service.update(moduleId, taskId, request)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}/tasks/${taskId}`,
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(request),
      }),
    )
    expect(result).toEqual(task)
  })

  it('updates a task status', async () => {
    const completedTask = {
      ...task,
      status: 'Completed' as const,
    }

    const fetchMock = stubFetch(completedTask, 200)
    const service = new HttpTaskService()

    const result = await service.updateStatus(moduleId, taskId, 'Completed')

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}/tasks/${taskId}/status`,
      expect.objectContaining({
        method: 'PATCH',
        body: JSON.stringify({ status: 'Completed' }),
      }),
    )
    expect(result).toEqual(completedTask)
  })

  it('deletes a task', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }))

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpTaskService()
    const result = await service.delete(moduleId, taskId)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}/tasks/${taskId}`,
      expect.objectContaining({ method: 'DELETE' }),
    )
    expect(result).toBeUndefined()
  })
})

function stubFetch(body: unknown, status: number) {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
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
