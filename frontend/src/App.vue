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
          <RouterLink to="/dashboard">
            Dashboard
          </RouterLink>

          <RouterLink to="/modules">
            Lernmodule
          </RouterLink>

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
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  box-shadow:
    0 0.35rem 0.9rem rgb(9 30 66 / 7%),
    inset 0 1px rgb(255 255 255 / 90%);
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
  padding: 0.55rem 0.75rem;
  border: 1px solid #d8dee8;
  border-radius: 0.65rem;
  background: linear-gradient(145deg, #ffffff 0%, #f3f6fa 100%);
  color: #626f86;
  font-size: 0.9rem;
  box-shadow:
    0 0.3rem 0.65rem rgb(9 30 66 / 7%),
    inset 0 1px rgb(255 255 255 / 90%);
}

.logout-button {
  position: relative;
  overflow: hidden;
  padding: 0.55rem 0.85rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.65rem;
  background: linear-gradient(145deg, #ffffff 0%, #edf2f8 100%);
  color: #172b4d;
  box-shadow:
    0 0.4rem 0.85rem rgb(9 30 66 / 10%),
    inset 0 1px rgb(255 255 255 / 95%),
    inset 0 -1px rgb(9 30 66 / 9%);
  cursor: pointer;
  transition:
    transform 150ms ease,
    border-color 150ms ease,
    color 150ms ease,
    box-shadow 150ms ease;
}

.logout-button:hover {
  border-color: #0c66e4;
  color: #0c66e4;
  box-shadow:
    0 0.65rem 1.15rem rgb(12 102 228 / 16%),
    inset 0 1px rgb(255 255 255 / 100%),
    inset 0 -1px rgb(12 102 228 / 10%);
  transform: translateY(-0.12rem);
}

.logout-button:active {
  transform: translateY(0.03rem);
}

nav a {
  position: relative;
  overflow: hidden;
  padding: 0.55rem 0.75rem;
  border: 1px solid transparent;
  border-radius: 0.65rem;
  background: linear-gradient(145deg, rgb(255 255 255 / 78%), rgb(242 246 251 / 78%));
  color: #44546f;
  box-shadow:
    0 0.25rem 0.6rem rgb(9 30 66 / 6%),
    inset 0 1px rgb(255 255 255 / 85%);
  text-decoration: none;
  transition:
    transform 150ms ease,
    border-color 150ms ease,
    color 150ms ease,
    box-shadow 150ms ease;
}

nav a:hover {
  border-color: #b8d3f7;
  color: #0c66e4;
  box-shadow:
    0 0.55rem 1rem rgb(12 102 228 / 13%),
    inset 0 1px #ffffff;
  transform: translateY(-0.1rem);
}

nav a.router-link-active {
  border-color: #8fb8f4;
  background: linear-gradient(
    145deg,
    #eef6ff 0%,
    #e5f0ff 52%,
    #d9eaff 100%
  );
  color: #0c66e4;
  box-shadow:
    0 0.45rem 0.9rem rgb(12 102 228 / 14%),
    inset 0 1px #ffffff,
    inset 0 -1px rgb(12 102 228 / 10%);
}

nav a:focus-visible,
.logout-button:focus-visible {
  outline: 0.18rem solid rgb(12 102 228 / 24%);
  outline-offset: 0.15rem;
}
</style>
