import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

import { authService } from '@/features/auth/authService'
import { i18n, setLocale } from '@/i18n'
import RegisterView from '../RegisterView.vue'

describe('RegisterView', () => {
  beforeEach(() => {
    setLocale('de')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountView() {
    return mount(RegisterView, {
      global: {
        plugins: [i18n],
      },
    })
  }

  it('does not submit invalid form data', async () => {
    const registerSpy = vi.spyOn(authService, 'register')

    const wrapper = mountView()

    await wrapper.get('form').trigger('submit')

    expect(registerSpy).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Bitte gib deine E-Mail-Adresse ein.')
    expect(wrapper.text()).toContain('Das Passwort erfüllt noch nicht alle Anforderungen.')
  })

  it('updates the password requirements while typing', async () => {
    const wrapper = mountView()

    expect(wrapper.findAll('.requirement-missing')).toHaveLength(5)

    await wrapper.get('#password').setValue('Registration-Test-2026!')

    expect(wrapper.findAll('.requirement-met')).toHaveLength(5)

    expect(wrapper.findAll('.requirement-missing')).toHaveLength(0)
  })

  it('toggles the password visibility', async () => {
    const wrapper = mountView()
    const passwordInput = wrapper.get('#password')
    const visibilityButton = wrapper.get('button[aria-label="Passwort anzeigen"]')

    expect(passwordInput.attributes('type')).toBe('password')

    await visibilityButton.trigger('click')

    expect(passwordInput.attributes('type')).toBe('text')
    expect(wrapper.get('button[aria-label="Passwort ausblenden"]').attributes('aria-pressed')).toBe(
      'true',
    )
  })

  it('registers a user with valid form data', async () => {
    const registerSpy = vi.spyOn(authService, 'register').mockResolvedValue({
      userId: '6e8b7921-b311-4f14-8ee2-a15b51e9578a',
      email: 'student@example.com',
    })

    const wrapper = mountView()

    await wrapper.get('#email').setValue('student@example.com')

    await wrapper.get('#password').setValue('Registration-Test-2026')

    await wrapper.get('#password-confirmation').setValue('Registration-Test-2026')

    await wrapper.get('form').trigger('submit')

    await flushPromises()

    expect(registerSpy).toHaveBeenCalledWith({
      email: 'student@example.com',
      password: 'Registration-Test-2026',
    })

    expect(wrapper.text()).toContain('student@example.com wurde erfolgreich registriert.')
  })

  it('shows the registration form in English', () => {
    setLocale('en')

    const wrapper = mountView()

    expect(wrapper.get('h1').text()).toBe('Sign up')
    expect(wrapper.text()).toContain('Create your account and start organizing your studies.')
    expect(wrapper.text()).toContain('at least 15 characters')
    expect(wrapper.get('.submit-button').text()).toBe('Create account')
  })
})
