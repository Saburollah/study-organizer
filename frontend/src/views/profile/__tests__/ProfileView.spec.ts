import { flushPromises, mount } from '@vue/test-utils'
import {
  afterEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'

import { profileService } from '@/features/profile/profileService'

import ProfileView from '../ProfileView.vue'

const profile = {
  userId: '496661cb-03a4-4ae2-a325-d193e4edfb54',
  email: 'student@example.com',
  firstName: 'Max',
  lastName: 'Mustermann',
  dateOfBirth: '2001-04-12',
  gender: 'Male' as const,
}

describe('ProfileView', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('loads and displays the current profile', async () => {
    vi.spyOn(profileService, 'get').mockResolvedValue(profile)

    const wrapper = mount(ProfileView)
    await flushPromises()

    expect(wrapper.get('#profile-email').attributes('readonly'))
      .toBeDefined()
    expect(wrapper.get('#profile-email').element)
      .toHaveProperty('value', 'student@example.com')
    expect(wrapper.get('#profile-first-name').element)
      .toHaveProperty('value', 'Max')
    expect(wrapper.get('#profile-last-name').element)
      .toHaveProperty('value', 'Mustermann')
  })

  it('saves only the editable profile fields', async () => {
    vi.spyOn(profileService, 'get').mockResolvedValue(profile)
    const updateMock = vi
      .spyOn(profileService, 'update')
      .mockResolvedValue({
        ...profile,
        firstName: 'Maria',
        lastName: 'Musterfrau',
        gender: 'Female',
      })

    const wrapper = mount(ProfileView)
    await flushPromises()

    await wrapper.get('#profile-first-name').setValue(' Maria ')
    await wrapper.get('#profile-last-name').setValue('Musterfrau')
    await wrapper.get('#profile-date-of-birth').setValue('2002-06-15')
    await wrapper.get('#profile-gender').setValue('Female')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith({
      firstName: 'Maria',
      lastName: 'Musterfrau',
      dateOfBirth: '2002-06-15',
      gender: 'Female',
    })
    expect(wrapper.text()).toContain(
      'erfolgreich gespeichert',
    )
  })

  it('selects the date of birth with the custom calendar', async () => {
    vi.spyOn(profileService, 'get').mockResolvedValue(profile)

    const wrapper = mount(ProfileView)
    await flushPromises()

    await wrapper.get('.birth-date-trigger').trigger('click')

    expect(wrapper.get('.calendar-month-name').text())
      .toBe('April')
    expect(wrapper.get('.calendar-year-select').element)
      .toHaveProperty('value', '2001')
    expect(wrapper.get('.calendar-day.selected').text())
      .toBe('12')

    await wrapper.get('.calendar-year-select').setValue('1985')

    await wrapper
      .get('button[aria-label="13.4.1985"]')
      .trigger('click')

    expect(wrapper.get('#profile-date-of-birth').element)
      .toHaveProperty('value', '1985-04-13')
    expect(wrapper.find('.date-picker-popover').exists())
      .toBe(false)
  })

  it('rejects a date of birth in the future', async () => {
    vi.spyOn(profileService, 'get').mockResolvedValue(profile)
    const updateMock = vi.spyOn(profileService, 'update')

    const wrapper = mount(ProfileView)
    await flushPromises()

    await wrapper.get('#profile-date-of-birth')
      .setValue('2999-01-01')
    await wrapper.get('form').trigger('submit')

    expect(updateMock).not.toHaveBeenCalled()
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'nicht in der Zukunft',
    )
  })
})
