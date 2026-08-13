<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import {
  RouterLink,
  RouterView,
  useRouter,
} from 'vue-router'

import { useAuthStore } from '@/features/auth/authStore'
import {
  setLocale,
  type SupportedLocale,
} from '@/i18n'

const router = useRouter()
const authStore = useAuthStore()
const { locale, t } = useI18n()

function changeLocale(
  newLocale: SupportedLocale,
): void {
  setLocale(newLocale)
}

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

      <nav :aria-label="t('navigation.mainLabel')">
        <div
          class="language-switcher"
          role="group"
          :aria-label="t('navigation.language')"
        >
          <button
            class="language-button"
            :class="{ active: locale === 'de' }"
            type="button"
            :aria-pressed="locale === 'de'"
            :aria-label="t('navigation.german')"
            :title="t('navigation.german')"
            @click="changeLocale('de')"
          >
            <span aria-hidden="true">🇩🇪</span>
          </button>

          <button
            class="language-button"
            :class="{ active: locale === 'en' }"
            type="button"
            :aria-pressed="locale === 'en'"
            :aria-label="t('navigation.english')"
            :title="t('navigation.english')"
            @click="changeLocale('en')"
          >
            <span aria-hidden="true">🇬🇧</span>
          </button>
        </div>

        <RouterLink to="/">
          {{ t('navigation.home') }}
        </RouterLink>

        <template v-if="authStore.isAuthenticated">
          <RouterLink to="/dashboard">
            {{ t('navigation.dashboard') }}
          </RouterLink>

          <RouterLink to="/modules">
            {{ t('navigation.modules') }}
          </RouterLink>

          <RouterLink to="/profile">
            {{ t('navigation.profile') }}
          </RouterLink>

          <button
            class="logout-button"
            type="button"
            @click="logout"
          >
            {{ t('navigation.logout') }}
          </button>
        </template>

        <template v-else>
          <RouterLink to="/login">
            {{ t('navigation.login') }}
          </RouterLink>

          <RouterLink to="/register">
            {{ t('navigation.register') }}
          </RouterLink>
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
  flex: 0 0 auto;
  color: #172b4d;
  font-size: 1.25rem;
  font-weight: 700;
  text-decoration: none;
  white-space: nowrap;
}

nav {
  display: flex;
  align-items: center;
  gap: clamp(0.45rem, 1vw, 1rem);
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
  white-space: nowrap;
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
  white-space: nowrap;
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
.logout-button:focus-visible,
.language-button:focus-visible {
  outline: 0.18rem solid rgb(12 102 228 / 24%);
  outline-offset: 0.15rem;
}

.language-switcher {
  display: flex;
  gap: 0.1rem;
  padding: 0.14rem;
  border: 1px solid #c7d2e0;
  border-radius: 0.6rem;
  background: linear-gradient(145deg, #ffffff, #edf2f8);
  box-shadow:
    inset 0 1px #ffffff,
    0 0.3rem 0.75rem rgb(9 30 66 / 8%);
}

.language-button {
  display: grid;
  min-width: 1.85rem;
  min-height: 1.7rem;
  place-items: center;
  padding: 0.22rem 0.32rem;
  border: 0;
  border-radius: 0.5rem;
  background: transparent;
  color: #5e6c84;
  font-size: 0.95rem;
  line-height: 1;
  font-weight: 700;
  cursor: pointer;
  transition:
    background-color 150ms ease,
    color 150ms ease,
    box-shadow 150ms ease;
}

.language-button:hover {
  color: #0c66e4;
}

.language-button.active {
  background: linear-gradient(145deg, #2684ff, #0c66e4);
  color: #ffffff;
  box-shadow:
    0 0.25rem 0.6rem rgb(12 102 228 / 24%),
    inset 0 1px rgb(255 255 255 / 35%);
}
</style>
