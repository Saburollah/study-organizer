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
import HomeView from '../HomeView.vue'

describe('HomeView', () => {
  beforeEach(() => {
    sessionStorage.clear()
    setActivePinia(createPinia())
  })

  function mountView() {
    return mount(HomeView, {
      global: {
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
})
