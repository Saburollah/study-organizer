import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import {
  apiRequest,
  configureAccessTokenProvider,
} from './apiClient'

describe('apiRequest', () => {
  afterEach(() => {
    configureAccessTokenProvider(() => null)
    vi.unstubAllGlobals()
  })

  it('adds the configured bearer token', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response(
          JSON.stringify({ value: 'ok' }),
          {
            status: 200,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      )

    vi.stubGlobal('fetch', fetchMock)
    configureAccessTokenProvider(
      () => 'test-access-token',
    )

    await apiRequest<{ value: string }>('/api/modules')

    expect(fetchMock).toHaveBeenCalledOnce()

    const requestOptions =
      fetchMock.mock.calls[0]?.[1]
    const headers = new Headers(requestOptions?.headers)

    expect(headers.get('Authorization')).toBe(
      'Bearer test-access-token',
    )
  })
})
