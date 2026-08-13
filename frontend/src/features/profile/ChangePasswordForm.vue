<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import {
  getPasswordRequirements,
  isPasswordValid,
} from '@/features/auth/passwordPolicy'
import { authService } from '@/features/auth/authService'
import { ApiError } from '@/services/api/apiClient'

const currentPassword = ref('')
const newPassword = ref('')
const newPasswordConfirmation = ref('')

const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showNewPasswordConfirmation = ref(false)

const errors = ref<Record<string, string>>({})
const requestError = ref('')
const successMessage = ref('')
const isSubmitting = ref(false)

const passwordRequirements = computed(() =>
  getPasswordRequirements(newPassword.value),
)

const invalidNewPasswordMessage =
  'Das neue Passwort erfüllt noch nicht alle Anforderungen.'
const unchangedPasswordMessage =
  'Das neue Passwort muss sich vom aktuellen Passwort unterscheiden.'

function setFieldError(
  field: string,
  message?: string,
): void {
  const nextErrors = { ...errors.value }

  if (message) {
    nextErrors[field] = message
  } else {
    delete nextErrors[field]
  }

  errors.value = nextErrors
}

function refreshNewPasswordError(): void {
  if (!errors.value.newPassword) {
    return
  }

  if (!isPasswordValid(newPassword.value)) {
    setFieldError('newPassword', invalidNewPasswordMessage)
  } else if (newPassword.value === currentPassword.value) {
    setFieldError('newPassword', unchangedPasswordMessage)
  } else {
    setFieldError('newPassword')
  }
}

watch([currentPassword, newPassword], refreshNewPasswordError)

watch(currentPassword, (password) => {
  if (password && errors.value.currentPassword) {
    setFieldError('currentPassword')
  }
})

watch(newPasswordConfirmation, (confirmation) => {
  if (
    errors.value.newPasswordConfirmation
    && confirmation === newPassword.value
  ) {
    setFieldError('newPasswordConfirmation')
  }
})

function validateForm(): boolean {
  const validationErrors: Record<string, string> = {}

  if (!currentPassword.value) {
    validationErrors.currentPassword =
      'Bitte gib dein aktuelles Passwort ein.'
  }

  if (!isPasswordValid(newPassword.value)) {
    validationErrors.newPassword = invalidNewPasswordMessage
  } else if (newPassword.value === currentPassword.value) {
    validationErrors.newPassword = unchangedPasswordMessage
  }

  if (newPasswordConfirmation.value !== newPassword.value) {
    validationErrors.newPasswordConfirmation =
      'Die neuen Passwörter stimmen nicht überein.'
  }

  errors.value = validationErrors
  return Object.keys(validationErrors).length === 0
}

async function submitPasswordChange(): Promise<void> {
  requestError.value = ''
  successMessage.value = ''

  if (!validateForm()) {
    return
  }

  isSubmitting.value = true

  try {
    await authService.changePassword({
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
    })

    currentPassword.value = ''
    newPassword.value = ''
    newPasswordConfirmation.value = ''
    errors.value = {}
    successMessage.value =
      'Dein Passwort wurde erfolgreich geändert.'
  } catch (error: unknown) {
    requestError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

function getErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Dein Passwort konnte nicht geändert werden.'
  }

  const validationMessage = Object.values(
    error.problem?.errors ?? {},
  )
    .flat()
    .find((message) => message.trim().length > 0)

  return validationMessage ?? error.message
}
</script>

<template>
  <section class="password-card" aria-labelledby="password-heading">
    <div class="password-card-heading">
      <div class="security-icon" aria-hidden="true">
        <svg viewBox="0 0 24 24">
          <rect x="5" y="10" width="14" height="10" rx="2" />
          <path d="M8 10V7a4 4 0 0 1 8 0v3" />
        </svg>
      </div>

      <div>
        <p class="card-eyebrow">SICHERHEIT</p>
        <h2 id="password-heading">Passwort ändern</h2>
        <p class="introduction">
          Verwende ein neues, einzigartiges Passwort für dein Konto.
        </p>
      </div>
    </div>

    <form
      class="password-form"
      novalidate
      @submit.prevent="submitPasswordChange"
    >
      <div class="password-grid">
        <div class="form-field current-password-field">
          <label for="current-password">Aktuelles Passwort</label>
          <div class="input-control">
            <input
              id="current-password"
              v-model="currentPassword"
              :type="showCurrentPassword ? 'text' : 'password'"
              autocomplete="current-password"
              placeholder="Aktuelles Passwort eingeben"
              :aria-invalid="Boolean(errors.currentPassword)"
              :aria-describedby="
                errors.currentPassword
                  ? 'current-password-error'
                  : undefined
              "
            />
            <button
              class="visibility-button"
              type="button"
              :aria-label="
                showCurrentPassword
                  ? 'Aktuelles Passwort ausblenden'
                  : 'Aktuelles Passwort anzeigen'
              "
              :aria-pressed="showCurrentPassword"
              @click="showCurrentPassword = !showCurrentPassword"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12z" />
                <circle cx="12" cy="12" r="2.5" />
                <path v-if="!showCurrentPassword" d="M4 4l16 16" />
              </svg>
            </button>
          </div>
          <p
            v-if="errors.currentPassword"
            id="current-password-error"
            class="field-error"
          >
            {{ errors.currentPassword }}
          </p>
        </div>

        <div class="form-field">
          <label for="new-password">Neues Passwort</label>
          <div class="input-control">
            <input
              id="new-password"
              v-model="newPassword"
              :type="showNewPassword ? 'text' : 'password'"
              autocomplete="new-password"
              placeholder="Neues Passwort eingeben"
              :aria-invalid="Boolean(errors.newPassword)"
              :aria-describedby="
                errors.newPassword
                  ? 'new-password-error'
                  : undefined
              "
            />
            <button
              class="visibility-button"
              type="button"
              :aria-label="
                showNewPassword
                  ? 'Neues Passwort ausblenden'
                  : 'Neues Passwort anzeigen'
              "
              :aria-pressed="showNewPassword"
              @click="showNewPassword = !showNewPassword"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12z" />
                <circle cx="12" cy="12" r="2.5" />
                <path v-if="!showNewPassword" d="M4 4l16 16" />
              </svg>
            </button>
          </div>

          <ul
            class="password-requirements"
            aria-label="Anforderungen an das neue Passwort"
          >
            <li
              v-for="requirement in passwordRequirements"
              :key="requirement.key"
              :class="{
                'requirement-met': requirement.isMet,
                'requirement-missing': !requirement.isMet,
              }"
            >
              <span class="requirement-symbol" aria-hidden="true">
                {{ requirement.isMet ? '✓' : '✕' }}
              </span>
              {{ requirement.label }}
            </li>
          </ul>

          <p
            v-if="errors.newPassword"
            id="new-password-error"
            class="field-error"
          >
            {{ errors.newPassword }}
          </p>
        </div>

        <div class="form-field">
          <label for="new-password-confirmation">
            Neues Passwort bestätigen
          </label>
          <div class="input-control">
            <input
              id="new-password-confirmation"
              v-model="newPasswordConfirmation"
              :type="showNewPasswordConfirmation ? 'text' : 'password'"
              autocomplete="new-password"
              placeholder="Neues Passwort bestätigen"
              :aria-invalid="Boolean(errors.newPasswordConfirmation)"
              :aria-describedby="
                errors.newPasswordConfirmation
                  ? 'new-password-confirmation-error'
                  : undefined
              "
            />
            <button
              class="visibility-button"
              type="button"
              :aria-label="
                showNewPasswordConfirmation
                  ? 'Passwortbestätigung ausblenden'
                  : 'Passwortbestätigung anzeigen'
              "
              :aria-pressed="showNewPasswordConfirmation"
              @click="
                showNewPasswordConfirmation =
                  !showNewPasswordConfirmation
              "
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12z" />
                <circle cx="12" cy="12" r="2.5" />
                <path v-if="!showNewPasswordConfirmation" d="M4 4l16 16" />
              </svg>
            </button>
          </div>
          <p
            v-if="errors.newPasswordConfirmation"
            id="new-password-confirmation-error"
            class="field-error"
          >
            {{ errors.newPasswordConfirmation }}
          </p>
        </div>
      </div>

      <p
        v-if="requestError"
        class="message error-message"
        role="alert"
      >
        {{ requestError }}
      </p>

      <p
        v-if="successMessage"
        class="message success-message"
        role="status"
      >
        {{ successMessage }}
      </p>

      <div class="form-actions">
        <button
          class="submit-button"
          type="submit"
          :disabled="isSubmitting"
        >
          {{
            isSubmitting
              ? 'Passwort wird geändert …'
              : 'Passwort ändern'
          }}
        </button>
      </div>
    </form>
  </section>
</template>

<style scoped>
.password-card {
  position: relative;
  overflow: hidden;
  margin-top: 2rem;
  padding: clamp(1.5rem, 4vw, 2.5rem);
  border: 1px solid #cbd5e1;
  border-radius: 1.4rem;
  background:
    linear-gradient(145deg, #ffffff 0%, #fbfdff 56%, #edf4fc 100%);
  box-shadow:
    0 1.2rem 2.8rem rgb(9 30 66 / 13%),
    inset 0 1px #ffffff,
    inset 0 -1px rgb(9 30 66 / 9%);
}

.password-card::before {
  position: absolute;
  top: -10rem;
  left: -6rem;
  width: 32rem;
  height: 20rem;
  border-radius: 50%;
  background: rgb(255 255 255 / 72%);
  content: '';
  pointer-events: none;
  transform: rotate(-12deg);
}

.password-card > * {
  position: relative;
}

.password-card-heading {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 2rem;
}

.security-icon {
  display: grid;
  width: 4.25rem;
  height: 4.25rem;
  flex: 0 0 auto;
  place-items: center;
  border: 1px solid #0755c7;
  border-radius: 1.2rem;
  background: linear-gradient(145deg, #2d82f7 0%, #0c66e4 55%, #0755c7 100%);
  color: #ffffff;
  box-shadow:
    0 0.7rem 1.25rem rgb(12 102 228 / 25%),
    inset 0 1px rgb(255 255 255 / 55%);
}

.security-icon svg {
  width: 2rem;
  height: 2rem;
  fill: none;
  stroke: currentColor;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.8;
}

.card-eyebrow {
  margin: 0 0 0.5rem;
  color: #0c66e4;
  font-size: 0.82rem;
  font-weight: 800;
  letter-spacing: 0.16em;
}

h2 {
  margin: 0;
  color: #172b4d;
  font-size: 1.65rem;
}

.introduction {
  margin: 0.45rem 0 0;
  color: #626f86;
}

.password-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.4rem;
}

.current-password-field {
  grid-column: 1 / -1;
}

.form-field {
  display: grid;
  align-content: start;
  gap: 0.65rem;
}

label {
  color: #172b4d;
  font-weight: 750;
}

.input-control {
  position: relative;
  display: flex;
  align-items: center;
}

input {
  width: 100%;
  min-height: 3.5rem;
  padding: 0.8rem 3rem 0.8rem 1rem;
  border: 1px solid #aebdce;
  border-radius: 0.85rem;
  background:
    linear-gradient(
      115deg,
      transparent 0%,
      transparent 38%,
      rgb(255 255 255 / 92%) 49%,
      transparent 60%,
      transparent 100%
    ) 160% 0 / 220% 100% no-repeat,
    linear-gradient(145deg, #ffffff 0%, #f8fbff 52%, #eaf1f8 100%);
  color: #172b4d;
  box-shadow:
    0 0.75rem 1.25rem rgb(9 30 66 / 12%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(9 30 66 / 9%);
  transition:
    transform 160ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease,
    background-position 420ms ease;
}

input::placeholder {
  color: #8993a4;
}

input:hover {
  border-color: #82aee9;
  background-position: -70% 0, 0 0;
  box-shadow:
    0 1rem 1.65rem rgb(9 30 66 / 17%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 10%);
  transform: translateY(-0.12rem);
}

input:focus {
  border-color: #0c66e4;
  outline: none;
  box-shadow:
    0 0 0 0.2rem rgb(12 102 228 / 16%),
    0 1rem 1.6rem rgb(9 30 66 / 16%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 10%);
  transform: translateY(-0.08rem);
}

input[aria-invalid="true"] {
  border-color: #c9372c;
}

.visibility-button {
  position: absolute;
  right: 0.45rem;
  display: grid;
  width: 2.35rem;
  height: 2.35rem;
  padding: 0;
  place-items: center;
  border: 0;
  border-radius: 0.55rem;
  background: transparent;
  color: #7a869a;
  cursor: pointer;
}

.visibility-button:hover {
  background: #e9f2ff;
  color: #172b4d;
}

.visibility-button:focus-visible {
  outline: 3px solid rgb(12 102 228 / 25%);
}

.visibility-button svg {
  width: 1.25rem;
  height: 1.25rem;
  fill: none;
  stroke: currentColor;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.75;
}

.password-requirements {
  display: grid;
  gap: 0.35rem;
  margin: 0.2rem 0 0;
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

.requirement-missing,
.field-error,
.error-message {
  color: #ae2a19;
}

.requirement-met,
.success-message {
  color: #216e4e;
}

.field-error {
  margin: 0;
  font-size: 0.875rem;
}

.message {
  margin: 1.5rem 0 0;
  padding: 0.85rem 1rem;
  border-radius: 0.75rem;
}

.success-message {
  border: 1px solid #4bce97;
  background: #dcfff1;
}

.error-message {
  border: 1px solid #f87168;
  background: #fff0ee;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 2rem;
}

.submit-button {
  padding: 0.85rem 1.25rem;
  border: 1px solid #0755c7;
  border-radius: 0.8rem;
  background: linear-gradient(145deg, #2d82f7 0%, #0c66e4 58%, #0755c7 100%);
  color: #ffffff;
  font-weight: 750;
  box-shadow:
    0 0.65rem 1.2rem rgb(12 102 228 / 23%),
    inset 0 1px rgb(255 255 255 / 52%),
    inset 0 -2px rgb(5 55 130 / 22%);
  cursor: pointer;
  transition:
    transform 150ms ease,
    box-shadow 150ms ease;
}

.submit-button:hover:not(:disabled) {
  box-shadow:
    0 0.9rem 1.5rem rgb(12 102 228 / 29%),
    inset 0 1px rgb(255 255 255 / 62%);
  transform: translateY(-0.12rem);
}

.submit-button:disabled {
  cursor: wait;
  opacity: 0.65;
}

@media (max-width: 42rem) {
  .password-grid {
    grid-template-columns: 1fr;
  }

  .current-password-field {
    grid-column: auto;
  }

  .submit-button {
    width: 100%;
  }
}
</style>
