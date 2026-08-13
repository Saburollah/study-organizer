import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { HttpProfileService } from '../profileService'

describe('HttpProfileService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the current profile from the API', async () => {
    const profile = {
      userId: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
      email: 'test@example.com',
      firstName: 'Saburo',
      lastName: 'Safari',
      dateOfBirth: '2000-01-15',
      gender: 'Male',
    }

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify(profile),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpProfileService()
    const result = await service.get()

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/profile/',
      expect.any(Object),
    )

    expect(result).toEqual(profile)
  })

  it('updates editable profile fields', async () => {
    const request = {
      firstName: 'Saburo',
      lastName: 'Safari',
      dateOfBirth: '2000-01-15',
      gender: 'Male' as const,
    }

    const updatedProfile = {
      userId: 'e6ab31a1-292b-4b31-b65b-dab568512b40',
      email: 'test@example.com',
      ...request,
    }

    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify(updatedProfile),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpProfileService()
    const result = await service.update(request)

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
      'http://localhost:5101/api/profile/',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(request),
      }),
    )

    expect(result).toEqual(updatedProfile)
  })
})
