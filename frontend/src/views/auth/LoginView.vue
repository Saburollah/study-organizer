<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { useAuthStore } from '@/features/auth/authStore'
import { ApiError } from '@/services/api/apiClient'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const showPassword = ref(false)

const emailError = ref('')
const passwordError = ref('')
const requestError = ref('')
const isSubmitting = ref(false)

async function submitLogin(): Promise<void> {
  emailError.value = ''
  passwordError.value = ''
  requestError.value = ''

  const normalizedEmail = email.value.trim()

  if (!normalizedEmail) {
    emailError.value =
      'Bitte gib deine E-Mail-Adresse ein.'
  } else if (
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      normalizedEmail,
    )
  ) {
    emailError.value =
      'Bitte gib eine gültige E-Mail-Adresse ein.'
  }

  if (!password.value) {
    passwordError.value =
      'Bitte gib dein Passwort ein.'
  }

  if (emailError.value || passwordError.value) {
    return
  }

  isSubmitting.value = true

  try {
    await authStore.login(
      normalizedEmail,
      password.value,
    )

    const requestedRedirect =
      typeof route.query.redirect === 'string'
        ? route.query.redirect
        : ''

    const redirect =
      requestedRedirect.startsWith('/')
      && !requestedRedirect.startsWith('//')
        ? requestedRedirect
        : '/dashboard'

    await router.push(redirect)
  } catch (error: unknown) {
    requestError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

function getErrorMessage(error: unknown): string {
  if (
    error instanceof ApiError
    && error.status === 401
  ) {
    return 'E-Mail-Adresse oder Passwort ist falsch.'
  }

  if (error instanceof ApiError) {
    return error.message
  }

  return 'Die Anmeldung ist unerwartet fehlgeschlagen.'
}
</script>

<template>
  <section class="login-page">
    <div class="login-card">
      <p class="eyebrow">WILLKOMMEN ZURÜCK</p>
      <h1>Anmelden</h1>

      <p class="introduction">
        Melde dich an, um deine Lernmodule und
        Aufgaben zu verwalten.
      </p>

      <form
        novalidate
        @submit.prevent="submitLogin"
      >
        <div class="form-field">
          <label for="login-email">
            E-Mail-Adresse
          </label>

          <div class="input-control">
            <svg
              class="input-icon"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                d="M4 6h16v12H4z M4 7l8 6 8-6"
              />
            </svg>

            <input
              id="login-email"
              v-model="email"
              type="email"
              autocomplete="email"
              placeholder="name@beispiel.de"
              :aria-invalid="Boolean(emailError)"
              :aria-describedby="
                emailError
                  ? 'login-email-error'
                  : undefined
              "
            />
          </div>

          <p
            v-if="emailError"
            id="login-email-error"
            class="field-error"
          >
            {{ emailError }}
          </p>
        </div>

        <div class="form-field">
          <label for="login-password">
            Passwort
          </label>

          <div class="input-control">
            <svg
              class="input-icon"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <rect
                x="5"
                y="10"
                width="14"
                height="10"
                rx="2"
              />
              <path d="M8 10V7a4 4 0 0 1 8 0v3" />
            </svg>

            <input
              id="login-password"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="current-password"
              placeholder="Passwort eingeben"
              :aria-invalid="Boolean(passwordError)"
              :aria-describedby="
                passwordError
                  ? 'login-password-error'
                  : undefined
              "
            />

            <button
              class="visibility-button"
              type="button"
              :aria-label="
                showPassword
                  ? 'Passwort ausblenden'
                  : 'Passwort anzeigen'
              "
              :aria-pressed="showPassword"
              @click="showPassword = !showPassword"
            >
              <svg
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12z"
                />
                <circle cx="12" cy="12" r="2.5" />
                <path
                  v-if="!showPassword"
                  d="M4 4l16 16"
                />
              </svg>
            </button>
          </div>

          <p
            v-if="passwordError"
            id="login-password-error"
            class="field-error"
          >
            {{ passwordError }}
          </p>
        </div>

        <p
          v-if="requestError"
          class="message-error"
          role="alert"
        >
          {{ requestError }}
        </p>

        <button
          class="submit-button"
          type="submit"
          :disabled="isSubmitting"
        >
          {{
            isSubmitting
              ? 'Anmeldung läuft …'
              : 'Anmelden'
          }}
        </button>
      </form>
    </div>
  </section>
</template>

<style scoped>
.login-page {
  display: grid;
  place-items: start center;
  min-height: calc(100vh - 4rem);
  padding: 4rem 1rem;
}

.login-card {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  width: min(100%, 32rem);
  padding: 2rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: linear-gradient(
    145deg,
    #ffffff 0%,
    #ffffff 54%,
    #f5f8fc 100%
  );
  box-shadow:
    0 1.15rem 2.4rem rgb(9 30 66 / 13%),
    0 0.25rem 0.7rem rgb(9 30 66 / 8%);
}

.login-card::before {
  position: absolute;
  z-index: -1;
  top: -25%;
  left: -12%;
  width: 78%;
  height: 54%;
  border-radius: 50%;
  background: rgb(255 255 255 / 76%);
  pointer-events: none;
  transform: rotate(-8deg);
  content: '';
}

.eyebrow {
  margin: 0 0 0.75rem;
  color: #0c66e4;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.12em;
}

h1 {
  margin: 0;
  color: #172b4d;
  font-size: 2.25rem;
}

.introduction {
  margin: 0.75rem 0 2rem;
  color: #626f86;
  line-height: 1.6;
}

form,
.form-field {
  display: grid;
  gap: 0.5rem;
}

form {
  gap: 1.25rem;
}

label {
  color: #172b4d;
  font-weight: 600;
}

.input-control {
  position: relative;
  display: flex;
  align-items: center;
}

.input-control input {
  width: 100%;
  padding: 0.75rem 2.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: linear-gradient(
    145deg,
    #ffffff 0%,
    #ffffff 58%,
    #f3f6fa 100%
  );
  color: #172b4d;
  box-shadow: 0 0.3rem 0.7rem rgb(9 30 66 / 7%);
  transition:
    transform 150ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease;
}

.input-control input:hover {
  border-color: #8fa6c0;
  box-shadow: 0 0.55rem 1rem rgb(9 30 66 / 12%);
  transform: translateY(-0.08rem);
}

.input-control input:focus {
  border-color: #0c66e4;
  outline: 3px solid rgb(12 102 228 / 15%);
  box-shadow: 0 0.55rem 1rem rgb(12 102 228 / 13%);
  transform: translateY(-0.08rem);
}

.input-control input[aria-invalid="true"] {
  border-color: #c9372c;
}

.input-icon {
  position: absolute;
  left: 0.85rem;
  z-index: 1;
  width: 1.15rem;
  height: 1.15rem;
  fill: none;
  stroke: #7a869a;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.75;
  pointer-events: none;
}

.visibility-button {
  position: absolute;
  right: 0.35rem;
  width: 2.25rem;
  height: 2.25rem;
  padding: 0;
  border: 0;
  border-radius: 0.35rem;
  background: transparent;
  color: #7a869a;
  cursor: pointer;
  transition:
    transform 150ms ease,
    background-color 150ms ease,
    color 150ms ease;
}

.visibility-button:hover {
  background: #f1f2f4;
  color: #172b4d;
  transform: translateY(-0.08rem);
}

.visibility-button:focus-visible {
  outline: 3px solid rgb(12 102 228 / 25%);
}

.visibility-button svg {
  width: 1.2rem;
  height: 1.2rem;
  fill: none;
  stroke: currentColor;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.75;
}

.field-error {
  margin: 0;
  color: #ae2a19;
  font-size: 0.875rem;
}

.message-error {
  margin: 0;
  padding: 0.75rem;
  border-radius: 0.5rem;
  color: #ae2a19;
  background: #ffebe6;
}

.submit-button {
  padding: 0.8rem 1rem;
  border: 1px solid #0c66e4;
  border-radius: 0.5rem;
  background: linear-gradient(
    145deg,
    #1f7bf2 0%,
    #0c66e4 58%,
    #0754bd 100%
  );
  color: #ffffff;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 0.45rem 0.9rem rgb(9 30 66 / 15%);
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    border-color 150ms ease;
}

.submit-button:hover:not(:disabled) {
  border-color: #0055cc;
  box-shadow: 0 0.75rem 1.25rem rgb(9 30 66 / 20%);
  transform: translateY(-0.12rem);
}

.submit-button:active:not(:disabled) {
  transform: translateY(0.04rem);
}

.submit-button:focus-visible {
  outline: 0.18rem solid rgb(12 102 228 / 24%);
  outline-offset: 0.15rem;
}

.submit-button:disabled {
  cursor: wait;
  opacity: 0.65;
}

@media (max-width: 520px) {
  .login-page {
    padding: 2rem 1rem;
  }

  .login-card {
    padding: 1.5rem;
  }
}
</style>
