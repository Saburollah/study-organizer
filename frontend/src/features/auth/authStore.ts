import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

import { authService } from './authService'

import type { AuthSession } from './authModels'

import {
  loadAuthSession,
  removeAuthSession,
  saveAuthSession,
} from './authSessionStorage'

export const useAuthStore = defineStore(
  'auth',
  () => {
    const session = ref<AuthSession | null>(
      loadAuthSession(),
    )

    const isAuthenticated = computed(() => {
      if (!session.value) {
        return false
      }

      const expiresAt =
        Date.parse(session.value.expiresAtUtc)

      return (
        Number.isFinite(expiresAt)
        && expiresAt > Date.now()
      )
    })

    const accessToken = computed(() =>
      isAuthenticated.value
        ? session.value?.accessToken ?? null
        : null,
    )

    const userEmail = computed(() =>
      isAuthenticated.value
        ? session.value?.email ?? null
        : null,
    )

    async function login(
      email: string,
      password: string,
    ): Promise<void> {
      const normalizedEmail = email.trim()

      const response = await authService.login({
        email: normalizedEmail,
        password,
      })

      const newSession: AuthSession = {
        email: normalizedEmail,
        accessToken: response.accessToken,
        expiresAtUtc: response.expiresAtUtc,
      }

      session.value = newSession
      saveAuthSession(newSession)
    }

    function logout(): void {
      session.value = null
      removeAuthSession()
    }

    return {
      session,
      isAuthenticated,
      accessToken,
      userEmail,
      login,
      logout,
    }
  },
)
