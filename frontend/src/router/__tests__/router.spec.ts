import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { createMemoryHistory } from 'vue-router'

import { useAuthStore } from '@/features/auth/authStore'

import { createAppRouter } from '../index'

describe('router authentication guard', () => {
  beforeEach(() => {
    sessionStorage.clear()
    setActivePinia(createPinia())
  })

  it('redirects unauthenticated users to login', async () => {
    const router = createAppRouter(
      createMemoryHistory(),
    )

    await router.push('/modules')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query).toEqual({
      redirect: '/modules',
    })
  })

  it('allows authenticated users to open modules', async () => {
    const authStore = useAuthStore()

    authStore.session = {
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
    }

    const router = createAppRouter(
      createMemoryHistory(),
    )

    await router.push('/modules')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('modules')
  })
})
