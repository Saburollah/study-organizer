import {
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest'

import {
  loadAuthSession,
  removeAuthSession,
  saveAuthSession,
} from '../authSessionStorage'

const storageKey = 'study-organizer.auth-session'

describe('authSessionStorage', () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  it('saves and restores a valid session', () => {
    const session = {
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
    }

    saveAuthSession(session)

    expect(loadAuthSession()).toEqual(session)
  })

  it('removes an expired session', () => {
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        email: 'student@example.com',
        accessToken: 'expired-token',
        expiresAtUtc: '2020-01-01T00:00:00Z',
      }),
    )

    expect(loadAuthSession()).toBeNull()
    expect(
      sessionStorage.getItem(storageKey),
    ).toBeNull()
  })

  it('removes malformed session data', () => {
    sessionStorage.setItem(
      storageKey,
      '{not-valid-json',
    )

    expect(loadAuthSession()).toBeNull()
    expect(
      sessionStorage.getItem(storageKey),
    ).toBeNull()
  })

  it('removes a stored session explicitly', () => {
    saveAuthSession({
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
    })

    removeAuthSession()

    expect(loadAuthSession()).toBeNull()
  })
})