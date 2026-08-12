import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

import { authService } from './authService'

import type { AuthSession } from './authModels'

export const useAuthStore = defineStore(
  'auth',
  () => {
    const session = ref<AuthSession | null>(null)

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

      session.value = {
        email: normalizedEmail,
        accessToken: response.accessToken,
        expiresAtUtc: response.expiresAtUtc,
      }
    }

    function logout(): void {
      session.value = null
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