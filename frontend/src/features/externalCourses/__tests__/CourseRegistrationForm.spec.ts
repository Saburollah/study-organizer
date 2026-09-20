import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import CourseRegistrationForm from '../CourseRegistrationForm.vue'

describe('CourseRegistrationForm', () => {
  beforeEach(() => {
    setLocale('de')
  })

  function mountForm(isSubmitting = false) {
    return mount(CourseRegistrationForm, {
      props: { isSubmitting },
      global: { plugins: [i18n] },
    })
  }

  it('shows the localized required message for an empty URL', async () => {
    const wrapper = mountForm()

    await wrapper.get('.course-registration-form').trigger('submit')

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Bitte gib einen Kurslink ein.',
    )
    expect(wrapper.emitted('register')).toBeUndefined()
  })

  it('shows the localized invalid message for a malformed URL', async () => {
    const wrapper = mountForm()
    await wrapper.get('#course-url').setValue('kein Link')

    await wrapper.get('.course-registration-form').trigger('submit')

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Bitte gib einen gültigen HTTPS-Link ein.',
    )
    expect(wrapper.emitted('register')).toBeUndefined()
  })

  it('emits exactly the trimmed valid fixture URL', async () => {
    const wrapper = mountForm()
    await wrapper.get('#course-url').setValue(
      '  https://mock-moodle.local/courses/software-engineering-2026  ',
    )

    await wrapper.get('.course-registration-form').trigger('submit')

    expect(wrapper.emitted('register')).toEqual([[
      {
        courseUrl:
          'https://mock-moodle.local/courses/software-engineering-2026',
      },
    ]])
  })

  it('does not expose a cancel action', () => {
    const wrapper = mountForm()

    expect(wrapper.find('.cancel-button').exists()).toBe(false)
  })

  it('disables submit and shows a loading label while submitting', () => {
    const wrapper = mountForm(true)
    const button = wrapper.get('button[type="submit"]')

    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toContain('Wird registriert')
  })
})
