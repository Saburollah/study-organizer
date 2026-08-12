import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { HttpModuleService } from '../moduleService'

describe('HttpModuleService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads all modules from the API', async () => {
    const modules = [
      {
        id: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
        name: 'Sichere Systeme',
        code: 'SIS',
        description: 'Vorlesung',
        color: '#3366FF',
        createdAtUtc: '2026-08-12T12:00:00Z',
      },
    ]

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify(modules),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpModuleService()
    const result = await service.getAll()

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/modules/',
      expect.any(Object),
    )

    expect(result).toEqual(modules)
  })

  it('creates a module through the API', async () => {
    const createdModule = {
      id: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
      name: 'Datenbanken',
      code: 'DB',
      description: 'SQL und PostgreSQL',
      color: '#FF8800',
      createdAtUtc: '2026-08-12T12:00:00Z',
    }

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify(createdModule),
          {
            status: 201,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpModuleService()

    const result = await service.create({
      name: 'Datenbanken',
      code: 'DB',
      description: 'SQL und PostgreSQL',
      color: '#FF8800',
    })

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/modules/',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          name: 'Datenbanken',
          code: 'DB',
          description: 'SQL und PostgreSQL',
          color: '#FF8800',
        }),
      }),
    )

    expect(result).toEqual(createdModule)
  })

  it('updates a module through the API', async () => {
    const moduleId =
      'e6ab31a1-292b-4b31-b65b-dab568512b40'

    const updatedModule = {
      id: moduleId,
      name: 'Datenbanken 2',
      code: 'DB2',
      description: 'Fortgeschrittenes SQL',
      color: '#3366FF',
      createdAtUtc: '2026-08-12T12:00:00Z',
    }

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify(updatedModule),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpModuleService()

    const request = {
      name: 'Datenbanken 2',
      code: 'DB2',
      description: 'Fortgeschrittenes SQL',
      color: '#3366FF',
    }

    const result =
      await service.update(moduleId, request)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}`,
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(request),
      }),
    )

    expect(result).toEqual(updatedModule)
  })

  it('deletes a module through the API', async () => {
    const moduleId =
      'e6ab31a1-292b-4b31-b65b-dab568512b40'

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(null, {
          status: 204,
        }),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpModuleService()
    const result = await service.delete(moduleId)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      `http://localhost:5101/api/modules/${moduleId}`,
      expect.objectContaining({
        method: 'DELETE',
      }),
    )

    expect(result).toBeUndefined()
  })
})
