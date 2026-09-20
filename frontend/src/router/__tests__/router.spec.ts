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

  it('protects the dashboard route', async () => {
    const router = createAppRouter(
      createMemoryHistory(),
    )

    await router.push('/dashboard')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query).toEqual({
      redirect: '/dashboard',
    })
  })

  it('allows authenticated users to open the dashboard', async () => {
    const authStore = useAuthStore()

    authStore.session = {
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
    }

    const router = createAppRouter(
      createMemoryHistory(),
    )

    await router.push('/dashboard')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('dashboard')
  })

  it('protects the tasks route', async () => {
    const moduleId =
      'e6ab31a1-292b-4b31-b65b-dab568512b40'

    const router = createAppRouter(
      createMemoryHistory(),
    )

    await router.push(`/modules/${moduleId}/tasks`)
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query).toEqual({
      redirect: `/modules/${moduleId}/tasks`,
    })
  })

  it('allows authenticated users to open tasks', async () => {
    const authStore = useAuthStore()

    authStore.session = {
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
    }

    const moduleId =
      'e6ab31a1-292b-4b31-b65b-dab568512b40'

    const router = createAppRouter(
      createMemoryHistory(),
    )

    await router.push(`/modules/${moduleId}/tasks`)
    await router.isReady()

    expect(router.currentRoute.value.name).toBe(
      'module-tasks',
    )
    expect(router.currentRoute.value.params.moduleId).toBe(
      moduleId,
    )
  })

  it('protects the Moodle courses route', async () => {
    const router = createAppRouter(createMemoryHistory())

    await router.push('/moodle-courses')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query).toEqual({
      redirect: '/moodle-courses',
    })
  })

  it('allows authenticated users to open Moodle courses', async () => {
    const authStore = useAuthStore()
    authStore.session = {
      email: 'student@example.com',
      accessToken: 'test-access-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
    }
    const router = createAppRouter(createMemoryHistory())

    await router.push('/moodle-courses')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('moodle-courses')
  })
})
