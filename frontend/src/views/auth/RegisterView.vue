<script setup lang="ts">
import { computed, ref } from 'vue'

import { authService } from '@/features/auth/authService'
import {
  getPasswordRequirements,
  isPasswordValid,
} from '@/features/auth/passwordPolicy'
import { ApiError } from '@/services/api/apiClient'

const email = ref('')
const password = ref('')
const passwordConfirmation = ref('')
const showPassword = ref(false)
const showPasswordConfirmation = ref(false)

const errors = ref<Record<string, string>>({})
const requestError = ref('')
const successMessage = ref('')
const isSubmitting = ref(false)

const passwordRequirements = computed(() =>
  getPasswordRequirements(password.value),
)

function validateForm(): boolean {
  const validationErrors: Record<string, string> = {}
  const normalizedEmail = email.value.trim()

  if (!normalizedEmail) {
    validationErrors.email =
      'Bitte gib deine E-Mail-Adresse ein.'
  } else if (
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      normalizedEmail,
    )
  ) {
    validationErrors.email =
      'Bitte gib eine gültige E-Mail-Adresse ein.'
  }

  if (!isPasswordValid(password.value)) {
    validationErrors.password =
      'Das Passwort erfüllt noch nicht alle Anforderungen.'
  }

  if (
    passwordConfirmation.value
    !== password.value
  ) {
    validationErrors.passwordConfirmation =
      'Die Passwörter stimmen nicht überein.'
  }

  errors.value = validationErrors

  return Object.keys(validationErrors).length === 0
}

async function submitRegistration(): Promise<void> {
  requestError.value = ''
  successMessage.value = ''

  if (!validateForm()) {
    return
  }

  isSubmitting.value = true

  try {
    const registeredUser =
      await authService.register({
        email: email.value.trim(),
        password: password.value,
      })

    successMessage.value =
      `${registeredUser.email} wurde erfolgreich registriert.`

    email.value = ''
    password.value = ''
    passwordConfirmation.value = ''
    errors.value = {}
  } catch (error: unknown) {
    requestError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

function getErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Die Registrierung ist unerwartet fehlgeschlagen.'
  }

  const validationMessage =
    Object.values(error.problem?.errors ?? {})
      .flat()
      .find((message) => message.trim().length > 0)

  return validationMessage ?? error.message
}
</script>

<template>
  <section class="register-page">
    <div class="register-card">
      <p class="eyebrow">NEUES KONTO</p>
      <h1>Registrieren</h1>

      <p class="introduction">
        Erstelle dein Konto und beginne damit,
        dein Studium zu organisieren.
      </p>

      <form
        novalidate
        @submit.prevent="submitRegistration"
      >
        <div class="form-field">
          <label for="email">E-Mail-Adresse</label>
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
              id="email"
              v-model="email"
              type="email"
              autocomplete="email"
              placeholder="name@beispiel.de"
              :aria-invalid="Boolean(errors.email)"
              :aria-describedby="
                errors.email ? 'email-error' : undefined
              "
            />
          </div>

          <p
            v-if="errors.email"
            id="email-error"
            class="field-error"
          >
            {{ errors.email }}
          </p>
        </div>

        <div class="form-field">
          <label for="password">Passwort</label>
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
              id="password"
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="new-password"
              placeholder="Passwort eingeben"
              :aria-invalid="Boolean(errors.password)"
              :aria-describedby="
                errors.password
                  ? 'password-error'
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

          <ul
            class="password-requirements"
            aria-label="Passwortanforderungen"
          >
            <li
              v-for="requirement in passwordRequirements"
              :key="requirement.key"
              :class="{
                'requirement-met': requirement.isMet,
                'requirement-missing': !requirement.isMet,
              }"
            >
              <span
                class="requirement-symbol"
                aria-hidden="true"
              >
                {{ requirement.isMet ? '✓' : '✕' }}
              </span>

              {{ requirement.label }}
            </li>
          </ul>

          <p
            v-if="errors.password"
            id="password-error"
            class="field-error"
          >
            {{ errors.password }}
          </p>
        </div>

        <div class="form-field">
          <label for="password-confirmation">
            Passwort bestätigen
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
              id="password-confirmation"
              v-model="passwordConfirmation"
              :type="
                showPasswordConfirmation
                  ? 'text'
                  : 'password'
              "
              autocomplete="new-password"
              placeholder="Passwort bestätigen"
              :aria-invalid="
                Boolean(errors.passwordConfirmation)
              "
              :aria-describedby="
                errors.passwordConfirmation
                  ? 'password-confirmation-error'
                  : undefined
              "
            />

            <button
              class="visibility-button"
              type="button"
              :aria-label="
                showPasswordConfirmation
                  ? 'Passwortbestätigung ausblenden'
                  : 'Passwortbestätigung anzeigen'
              "
              :aria-pressed="showPasswordConfirmation"
              @click="
                showPasswordConfirmation =
                  !showPasswordConfirmation
              "
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
                  v-if="!showPasswordConfirmation"
                  d="M4 4l16 16"
                />
              </svg>
            </button>
          </div>

          <p
            v-if="errors.passwordConfirmation"
            id="password-confirmation-error"
            class="field-error"
          >
            {{ errors.passwordConfirmation }}
          </p>
        </div>

        <p
          v-if="requestError"
          class="message message-error"
          role="alert"
        >
          {{ requestError }}
        </p>

        <p
          v-if="successMessage"
          class="message message-success"
          role="status"
        >
          {{ successMessage }}
        </p>

        <button
          class="submit-button"
          type="submit"
          :disabled="isSubmitting"
        >
          {{
            isSubmitting
              ? 'Registrierung läuft …'
              : 'Konto erstellen'
          }}
        </button>
      </form>
    </div>
  </section>
</template>

<style scoped>
.register-page {
  display: grid;
  place-items: start center;
  min-height: calc(100vh - 4rem);
  padding: 4rem 1rem;
}

.register-card {
  width: min(100%, 32rem);
  padding: 2rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
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
  background: #ffffff;
  color: #172b4d;
}

.input-control input::placeholder {
  color: #97a0af;
}

.input-control input:focus {
  border-color: #0c66e4;
  outline: 3px solid rgb(12 102 228 / 15%);
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
  display: grid;
  width: 2.25rem;
  height: 2.25rem;
  padding: 0;
  place-items: center;
  border: 0;
  border-radius: 0.35rem;
  background: transparent;
  color: #7a869a;
  cursor: pointer;
}

.visibility-button:hover {
  background: #f1f2f4;
  color: #172b4d;
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

.field-hint,
.field-error,
.message {
  margin: 0;
  font-size: 0.875rem;
}

.password-requirements {
  display: grid;
  gap: 0.4rem;
  margin: 0.25rem 0 0;
  padding: 0;
  list-style: none;
  font-size: 0.875rem;
}

.password-requirements li {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  line-height: 1.4;
}

.requirement-symbol {
  width: 1rem;
  flex: 0 0 1rem;
  font-weight: 800;
  text-align: center;
}

.requirement-missing {
  color: #ae2a19;
}

.requirement-met {
  color: #216e4e;
}

.field-hint {
  color: #626f86;
}

.field-error,
.message-error {
  color: #ae2a19;
}

.message {
  padding: 0.75rem;
  border-radius: 0.5rem;
}

.message-error {
  background: #ffebe6;
}

.message-success {
  color: #216e4e;
  background: #dcfff1;
}

.submit-button {
  padding: 0.8rem 1rem;
  border: 0;
  border-radius: 0.5rem;
  background: #0c66e4;
  color: #ffffff;
  font-weight: 700;
  cursor: pointer;
}

.submit-button:hover:not(:disabled) {
  background: #0055cc;
}

.submit-button:disabled {
  cursor: wait;
  opacity: 0.65;
}

@media (max-width: 520px) {
  .register-page {
    padding: 2rem 1rem;
  }

  .register-card {
    padding: 1.5rem;
  }
}
</style>
