import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

import { useAuthStore } from '@/features/auth/authStore'
import { i18n, setLocale } from '@/i18n'
import { ApiError } from '@/services/api/apiClient'
import LoginView from '../LoginView.vue'

const pushMock = vi.fn<(path: string) => Promise<void>>()
const routeQuery: Record<string, string> = {}

vi.mock('vue-router', () => ({
  useRoute: () => ({
    query: routeQuery,
  }),
  useRouter: () => ({
    push: pushMock,
  }),
}))

describe('LoginView', () => {
  beforeEach(() => {
    sessionStorage.clear()
    setActivePinia(createPinia())
    setLocale('de')
    pushMock.mockReset()
    for (const key of Object.keys(routeQuery)) {
      delete routeQuery[key]
    }
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountView() {
    return mount(LoginView, {
      global: {
        plugins: [i18n],
      },
    })
  }

  it('does not submit empty form data', async () => {
    const store = useAuthStore()
    const loginSpy = vi.spyOn(store, 'login')

    const wrapper = mountView()

    await wrapper.get('form').trigger('submit')

    expect(loginSpy).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Bitte gib deine E-Mail-Adresse ein.')
    expect(wrapper.text()).toContain('Bitte gib dein Passwort ein.')
  })

  it('logs in with valid credentials', async () => {
    const store = useAuthStore()
    const loginSpy = vi.spyOn(store, 'login').mockResolvedValue()

    const wrapper = mountView()

    await wrapper.get('#login-email').setValue(' student@example.com ')

    await wrapper.get('#login-password').setValue('Sicheres-Passwort-2026!')

    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(loginSpy).toHaveBeenCalledWith('student@example.com', 'Sicheres-Passwort-2026!')
    expect(pushMock).toHaveBeenCalledWith('/dashboard')
  })

  it('returns to the requested protected page after login', async () => {
    routeQuery.redirect = '/modules/module-1/tasks'

    const store = useAuthStore()
    vi.spyOn(store, 'login').mockResolvedValue()

    const wrapper = mountView()

    await wrapper.get('#login-email').setValue('student@example.com')
    await wrapper.get('#login-password').setValue('Sicheres-Passwort-2026!')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(pushMock).toHaveBeenCalledWith('/modules/module-1/tasks')
  })

  it('shows a message for invalid credentials', async () => {
    const store = useAuthStore()

    vi.spyOn(store, 'login').mockRejectedValue(
      new ApiError(401, {
        title: 'Authentication failed.',
      }),
    )

    const wrapper = mountView()

    await wrapper.get('#login-email').setValue('student@example.com')

    await wrapper.get('#login-password').setValue('Falsches-Passwort-2026!')

    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('E-Mail-Adresse oder Passwort ist falsch.')
    expect(pushMock).not.toHaveBeenCalled()
  })

  it('toggles password visibility', async () => {
    const wrapper = mountView()
    const passwordInput = wrapper.get('#login-password')

    expect(passwordInput.attributes('type')).toBe('password')

    await wrapper.get('button[aria-label="Passwort anzeigen"]').trigger('click')

    expect(passwordInput.attributes('type')).toBe('text')

    expect(wrapper.get('button[aria-label="Passwort ausblenden"]').attributes('aria-pressed')).toBe(
      'true',
    )
  })

  it('shows the login form in English', () => {
    setLocale('en')

    const wrapper = mountView()

    expect(wrapper.get('h1').text()).toBe('Sign in')
    expect(wrapper.text()).toContain('Sign in to manage your study modules and tasks.')
    expect(wrapper.get('#login-email').attributes('placeholder')).toBe('name@example.com')
    expect(wrapper.get('.submit-button').text()).toBe('Sign in')
  })
})
