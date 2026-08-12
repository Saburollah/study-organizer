import { createPinia, setActivePinia } from 'pinia'
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { authService } from '../authService'
import { useAuthStore } from '../authStore'
import {
  loadAuthSession,
  saveAuthSession,
} from '../authSessionStorage'

describe('useAuthStore', () => {
  beforeEach(() => {
    sessionStorage.clear()
    setActivePinia(createPinia())
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('stores a valid session after login', async () => {
    const loginSpy = vi
      .spyOn(authService, 'login')
      .mockResolvedValue({
        accessToken: 'test-access-token',
        expiresAtUtc: '2099-08-12T15:00:00Z',
      })

    const store = useAuthStore()

    await store.login(
      '  student@example.com  ',
      'Sicheres-Passwort-2026!',
    )

    expect(loginSpy).toHaveBeenCalledWith({
      email: 'student@example.com',
      password: 'Sicheres-Passwort-2026!',
    })
    expect(store.isAuthenticated).toBe(true)
    expect(store.accessToken).toBe('test-access-token')
    expect(store.userEmail).toBe('student@example.com')
    expect(loadAuthSession()).toEqual({
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2099-08-12T15:00:00Z',
    })
  })

  it('restores a valid stored session', () => {
    saveAuthSession({
      email: 'student@example.com',
      accessToken: 'stored-access-token',
      expiresAtUtc: '2099-08-12T15:00:00Z',
    })

    const store = useAuthStore()

    expect(store.isAuthenticated).toBe(true)
    expect(store.accessToken).toBe('stored-access-token')
    expect(store.userEmail).toBe('student@example.com')
  })

  it('treats an expired session as unauthenticated', () => {
    const store = useAuthStore()

    store.session = {
      email: 'student@example.com',
      accessToken: 'expired-access-token',
      expiresAtUtc: '2020-01-01T00:00:00Z',
    }

    expect(store.isAuthenticated).toBe(false)
    expect(store.accessToken).toBeNull()
    expect(store.userEmail).toBeNull()
  })

  it('removes the session on logout', () => {
    saveAuthSession({
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2099-08-12T15:00:00Z',
    })

    const store = useAuthStore()

    store.logout()

    expect(store.session).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(loadAuthSession()).toBeNull()
  })
})
