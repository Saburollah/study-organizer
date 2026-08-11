import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import HomeView from '../HomeView.vue'

describe('HomeView', () => {
  it('zeigt die Überschrift der Anwendung', () => {
    const wrapper = mount(HomeView)

    expect(wrapper.get('h1').text()).toBe(
      'Organisiere dein Studium an einem Ort.',
    )
  })

  it('zeigt die drei Werkzeuge', () => {
    const wrapper = mount(HomeView)

    expect(wrapper.findAll('.feature-card')).toHaveLength(3)
    expect(wrapper.text()).toContain('Lernmodule')
    expect(wrapper.text()).toContain('Aufgaben')
    expect(wrapper.text()).toContain('Fortschritt')
  })
})