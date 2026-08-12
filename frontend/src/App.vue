<script setup lang="ts">
import {
  RouterLink,
  RouterView,
  useRouter,
} from 'vue-router'

import { useAuthStore } from '@/features/auth/authStore'

const router = useRouter()
const authStore = useAuthStore()

async function logout(): Promise<void> {
  authStore.logout()
  await router.push('/login')
}
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">
        Study Organizer
      </RouterLink>

      <nav aria-label="Hauptnavigation">
        <RouterLink to="/">Startseite</RouterLink>

        <template v-if="authStore.isAuthenticated">
          <span class="user-email">
            {{ authStore.userEmail }}
          </span>

          <button
            class="logout-button"
            type="button"
            @click="logout"
          >
            Abmelden
          </button>
        </template>

        <template v-else>
          <RouterLink to="/login">Anmelden</RouterLink>
          <RouterLink to="/register">Registrieren</RouterLink>
        </template>
      </nav>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
}

.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 2rem;
  border-bottom: 1px solid #dfe3e8;
}

.brand {
  color: #172b4d;
  font-size: 1.25rem;
  font-weight: 700;
  text-decoration: none;
}

nav {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.user-email {
  color: #626f86;
  font-size: 0.9rem;
}

.logout-button {
  padding: 0.45rem 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.4rem;
  background: #ffffff;
  color: #172b4d;
  cursor: pointer;
}

.logout-button:hover {
  border-color: #0c66e4;
  color: #0c66e4;
}

nav a {
  color: #44546f;
  text-decoration: none;
}

nav a.router-link-active {
  color: #0c66e4;
}
</style>
