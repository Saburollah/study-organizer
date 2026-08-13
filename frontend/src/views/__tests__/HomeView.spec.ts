import { createPinia, setActivePinia } from 'pinia'
import {
  mount,
  RouterLinkStub,
} from '@vue/test-utils'
import {
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest'

import { useAuthStore } from '@/features/auth/authStore'
import { i18n, setLocale } from '@/i18n'
import HomeView from '../HomeView.vue'

describe('HomeView', () => {
  beforeEach(() => {
    sessionStorage.clear()
    setActivePinia(createPinia())
    setLocale('de')
  })

  function mountView() {
    return mount(HomeView, {
      global: {
        plugins: [i18n],
        stubs: {
          RouterLink: RouterLinkStub,
        },
      },
    })
  }

  it('zeigt die Überschrift der Anwendung', () => {
    const wrapper = mountView()

    expect(wrapper.get('h1').text()).toBe(
      'Mehr Überblick. Weniger Stress.',
    )
  })

  it('zeigt die drei Werkzeuge', () => {
    const wrapper = mountView()

    expect(wrapper.findAll('.feature-card')).toHaveLength(3)
    expect(wrapper.text()).toContain('Lernmodule')
    expect(wrapper.text()).toContain('Aufgaben')
    expect(wrapper.text()).toContain('Fortschritt')
  })

  it('führt Gäste zur Registrierung und Anmeldung', () => {
    const wrapper = mountView()
    const links = wrapper.findAllComponents(RouterLinkStub)

    expect(links.some((link) => link.props('to') === '/register'))
      .toBe(true)
    expect(links.some((link) => link.props('to') === '/login'))
      .toBe(true)
    expect(wrapper.text()).toContain('Kostenlos starten')
  })

  it('führt angemeldete Benutzer zum Dashboard', () => {
    const authStore = useAuthStore()

    authStore.session = {
      email: 'student@example.com',
      accessToken: 'valid-access-token',
      expiresAtUtc: '2099-08-13T12:00:00Z',
    }

    const wrapper = mountView()
    const links = wrapper.findAllComponents(RouterLinkStub)

    expect(links.some((link) => link.props('to') === '/dashboard'))
      .toBe(true)
    expect(wrapper.text()).toContain('Zum Dashboard')
    expect(wrapper.text()).not.toContain('Ich habe schon ein Konto')
  })

  it('zeigt die Startseite auf Englisch', () => {
    setLocale('en')

    const wrapper = mountView()

    expect(wrapper.get('h1').text()).toBe(
      'More clarity. Less stress.',
    )
    expect(wrapper.text()).toContain('Study modules')
    expect(wrapper.text()).toContain('Get started for free')
  })
})
