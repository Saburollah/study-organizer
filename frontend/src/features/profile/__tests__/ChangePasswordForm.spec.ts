import { flushPromises, mount } from '@vue/test-utils'
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { authService } from '@/features/auth/authService'
import { i18n, setLocale } from '@/i18n'
import { ApiError } from '@/services/api/apiClient'

import ChangePasswordForm from '../ChangePasswordForm.vue'

const currentPassword = 'Bisheriges-Passwort-2026!'
const newPassword = 'Neues-Sicheres-Passwort-2026!'

describe('ChangePasswordForm', () => {
  beforeEach(() => {
    setLocale('de')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountForm() {
    return mount(ChangePasswordForm, {
      global: {
        plugins: [i18n],
      },
    })
  }

  it('changes the password and clears all fields', async () => {
    const changePasswordMock = vi
      .spyOn(authService, 'changePassword')
      .mockResolvedValue()

    const wrapper = mountForm()

    await wrapper.get('#current-password')
      .setValue(currentPassword)
    await wrapper.get('#new-password')
      .setValue(newPassword)
    await wrapper.get('#new-password-confirmation')
      .setValue(newPassword)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(changePasswordMock).toHaveBeenCalledWith({
      currentPassword,
      newPassword,
    })
    expect(wrapper.text()).toContain(
      'Passwort wurde erfolgreich geändert',
    )
    expect(wrapper.get('#current-password').element)
      .toHaveProperty('value', '')
    expect(wrapper.get('#new-password').element)
      .toHaveProperty('value', '')
    expect(wrapper.get('#new-password-confirmation').element)
      .toHaveProperty('value', '')
  })

  it('rejects a different password confirmation', async () => {
    const changePasswordMock = vi.spyOn(
      authService,
      'changePassword',
    )

    const wrapper = mountForm()

    await wrapper.get('#current-password')
      .setValue(currentPassword)
    await wrapper.get('#new-password')
      .setValue(newPassword)
    await wrapper.get('#new-password-confirmation')
      .setValue('Ein-Anderes-Passwort-2026!')
    await wrapper.get('form').trigger('submit')

    expect(changePasswordMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain(
      'Passwörter stimmen nicht überein',
    )
  })

  it('removes a stale validation error when the password becomes valid', async () => {
    const changePasswordMock = vi.spyOn(
      authService,
      'changePassword',
    )

    const wrapper = mountForm()

    await wrapper.get('#current-password')
      .setValue(currentPassword)
    await wrapper.get('#new-password').setValue('zu-kurz')
    await wrapper.get('#new-password-confirmation')
      .setValue('zu-kurz')
    await wrapper.get('form').trigger('submit')

    expect(changePasswordMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain(
      'Das neue Passwort erfüllt noch nicht alle Anforderungen.',
    )
    expect(wrapper.get('#new-password').attributes('aria-invalid'))
      .toBe('true')

    await wrapper.get('#new-password').setValue(newPassword)

    expect(wrapper.text()).not.toContain(
      'Das neue Passwort erfüllt noch nicht alle Anforderungen.',
    )
    expect(wrapper.get('#new-password').attributes('aria-invalid'))
      .toBe('false')
  })

  it('shows an error returned by the API', async () => {
    vi.spyOn(authService, 'changePassword').mockRejectedValue(
      new ApiError(400, {
        errors: {
          password: ['Das aktuelle Passwort ist falsch.'],
        },
      }),
    )

    const wrapper = mountForm()

    await wrapper.get('#current-password')
      .setValue(currentPassword)
    await wrapper.get('#new-password')
      .setValue(newPassword)
    await wrapper.get('#new-password-confirmation')
      .setValue(newPassword)
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Das aktuelle Passwort ist falsch.',
    )
  })

  it('shows the password form in English after changing the locale', () => {
    setLocale('en')

    const wrapper = mountForm()

    expect(wrapper.get('h2').text()).toBe('Change password')
    expect(wrapper.text()).toContain('Current password')
    expect(wrapper.text()).toContain('Confirm new password')
    expect(wrapper.text()).toContain('at least 15 characters')
  })
})
