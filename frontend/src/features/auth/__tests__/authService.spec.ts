import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { HttpAuthService } from '../authService'

describe('HttpAuthService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends registration data to the API', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify({
          userId: '6e8b7921-b311-4f14-8ee2-a15b51e9578a',
          email: 'student@example.com',
        }),
        {
          status: 201,
          headers: {
            'Content-Type': 'application/json',
          },
        },
      ),
    )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpAuthService()

    const result = await service.register({
      email: 'student@example.com',
      password: 'Registration-Test-2026',
    })

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith('http://localhost:5101/api/auth/register', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          email: 'student@example.com',
          password: 'Registration-Test-2026',
        }),
      }))

    expect(result).toEqual({
      userId: '6e8b7921-b311-4f14-8ee2-a15b51e9578a',
      email: 'student@example.com',
    })
  })

  it('sends login data to the API', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify({
            accessToken: 'test-access-token',
            expiresAtUtc: '2026-08-12T15:00:00Z',
          }),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)

    const service = new HttpAuthService()

    const result = await service.login({
      email: 'student@example.com',
      password: 'Sicheres-Passwort-2026!',
    })

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith('http://localhost:5101/api/auth/login', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          email: 'student@example.com',
          password: 'Sicheres-Passwort-2026!',
        }),
      }))

    expect(result).toEqual({
      accessToken: 'test-access-token',
      expiresAtUtc: '2026-08-12T15:00:00Z',
    })
  })
})
