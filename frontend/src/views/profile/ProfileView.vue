<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import ChangePasswordForm from '@/features/profile/ChangePasswordForm.vue'
import type {
  ProfileGender,
  UserProfile,
} from '@/features/profile/profileModels'
import { profileService } from '@/features/profile/profileService'
import { ApiError } from '@/services/api/apiClient'

const { locale, t } = useI18n()

const profile = ref<UserProfile | null>(null)
const firstName = ref('')
const lastName = ref('')
const dateOfBirth = ref('')
const gender = ref<ProfileGender | ''>('')

const isLoading = ref(true)
const isSaving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const isBirthDatePickerOpen = ref(false)
const birthCalendarMonth = ref(startOfMonth(new Date()))

const dateLocale = computed(() =>
  locale.value === 'en' ? 'en-GB' : 'de-DE',
)

const weekDays = computed(() =>
  locale.value === 'en'
    ? ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
    : ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'],
)

const today = computed(() => formatDateOnly(new Date()))

const displayedDateOfBirth = computed(() => {
  const date = parseDateOnly(dateOfBirth.value)

  if (!date) {
    return t('profile.placeholders.birthDate')
  }

  return new Intl.DateTimeFormat(dateLocale.value, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
})

const birthCalendarMonthName = computed(() =>
  new Intl.DateTimeFormat(dateLocale.value, {
    month: 'long',
  }).format(birthCalendarMonth.value),
)

const birthCalendarDays = computed(() => {
  const firstDay = startOfMonth(birthCalendarMonth.value)
  const mondayOffset = (firstDay.getDay() + 6) % 7
  const firstVisibleDay = new Date(
    firstDay.getFullYear(),
    firstDay.getMonth(),
    firstDay.getDate() - mondayOffset,
  )
  const selectedDate = parseDateOnly(dateOfBirth.value)
  const currentDate = parseDateOnly(today.value)

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(
      firstVisibleDay.getFullYear(),
      firstVisibleDay.getMonth(),
      firstVisibleDay.getDate() + index,
    )

    return {
      date,
      key: formatDateOnly(date),
      isCurrentMonth:
        date.getMonth()
        === birthCalendarMonth.value.getMonth(),
      isSelected: isSameDay(date, selectedDate),
      isToday: isSameDay(date, currentDate),
      isDisabled: currentDate
        ? date.getTime() > currentDate.getTime()
        : false,
    }
  })
})

onMounted(loadProfile)

async function loadProfile(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''

  try {
    applyProfile(await profileService.get())
  } catch (error: unknown) {
    errorMessage.value = getErrorMessage(
      error,
      t('profile.errors.load'),
    )
  } finally {
    isLoading.value = false
  }
}

async function saveProfile(): Promise<void> {
  errorMessage.value = ''
  successMessage.value = ''

  if (
    dateOfBirth.value
    && dateOfBirth.value > today.value
  ) {
    errorMessage.value = t('profile.errors.birthDateFuture')
    return
  }

  isSaving.value = true

  try {
    const updatedProfile = await profileService.update({
      firstName: normalizeOptional(firstName.value),
      lastName: normalizeOptional(lastName.value),
      dateOfBirth: dateOfBirth.value || null,
      gender: gender.value || null,
    })

    applyProfile(updatedProfile)
    successMessage.value = t('profile.success.saved')
  } catch (error: unknown) {
    errorMessage.value = getErrorMessage(
      error,
      t('profile.errors.save'),
    )
  } finally {
    isSaving.value = false
  }
}

function applyProfile(value: UserProfile): void {
  profile.value = value
  firstName.value = value.firstName ?? ''
  lastName.value = value.lastName ?? ''
  dateOfBirth.value = value.dateOfBirth ?? ''
  gender.value = value.gender ?? ''
}

function openBirthDatePicker(): void {
  const selectedDate = parseDateOnly(dateOfBirth.value)
  birthCalendarMonth.value = startOfMonth(
    selectedDate ?? new Date(),
  )
  isBirthDatePickerOpen.value = true
}

function changeBirthCalendarMonth(offset: number): void {
  birthCalendarMonth.value = new Date(
    birthCalendarMonth.value.getFullYear(),
    birthCalendarMonth.value.getMonth() + offset,
    1,
  )
}

function changeBirthCalendarYear(event: Event): void {
  const input = event.target as HTMLInputElement
  const selectedYear = Number(
    input.value,
  )

  if (
    !Number.isInteger(selectedYear)
    || selectedYear < 1900
    || selectedYear > new Date().getFullYear()
  ) {
    input.value = String(
      birthCalendarMonth.value.getFullYear(),
    )
    return
  }

  birthCalendarMonth.value = new Date(
    selectedYear,
    birthCalendarMonth.value.getMonth(),
    1,
  )
}

function selectBirthDate(
  date: Date,
  isDisabled: boolean,
): void {
  if (isDisabled) {
    return
  }

  dateOfBirth.value = formatDateOnly(date)
  isBirthDatePickerOpen.value = false
  errorMessage.value = ''
}

function clearBirthDate(): void {
  dateOfBirth.value = ''
  isBirthDatePickerOpen.value = false
}

function parseDateOnly(value: string): Date | null {
  const parts = value.split('-').map(Number)

  if (
    parts.length !== 3
    || parts.some((part) => !Number.isInteger(part))
  ) {
    return null
  }

  const [year, month, day] = parts

  if (
    year === undefined
    || month === undefined
    || day === undefined
  ) {
    return null
  }

  const date = new Date(year, month - 1, day)

  return Number.isNaN(date.getTime()) ? null : date
}

function formatDateOnly(date: Date): string {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0'),
  ].join('-')
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

function isSameDay(
  first: Date,
  second: Date | null,
): boolean {
  return second !== null
    && first.getFullYear() === second.getFullYear()
    && first.getMonth() === second.getMonth()
    && first.getDate() === second.getDate()
}

function normalizeOptional(value: string): string | null {
  const normalized = value.trim()
  return normalized || null
}

function getErrorMessage(
  error: unknown,
  fallback: string,
): string {
  if (!(error instanceof ApiError)) {
    return fallback
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
  <section class="profile-page">
    <div class="page-heading">
      <p class="eyebrow">{{ t('profile.eyebrow') }}</p>
      <h1>{{ t('profile.title') }}</h1>
      <p>
        {{ t('profile.description') }}
      </p>
    </div>

    <p
      v-if="isLoading"
      class="status-card"
      role="status"
    >
      {{ t('profile.loading') }}
    </p>

    <div
      v-else-if="!profile"
      class="status-card error-card"
      role="alert"
    >
      <p>{{ errorMessage }}</p>
      <button type="button" @click="loadProfile">
        {{ t('profile.retry') }}
      </button>
    </div>

    <form
      v-else
      class="profile-card"
      novalidate
      @submit.prevent="saveProfile"
    >
      <div class="profile-card-heading">
        <div class="avatar" aria-hidden="true">
          {{ (firstName || profile.email).charAt(0).toUpperCase() }}
        </div>

        <div>
          <p class="card-eyebrow">
            {{ t('profile.personalData.eyebrow') }}
          </p>
          <h2>
            {{ firstName || lastName
              ? `${firstName} ${lastName}`.trim()
              : t('profile.personalData.defaultName') }}
          </h2>
        </div>
      </div>

      <p
        v-if="successMessage"
        class="message success-message"
        role="status"
      >
        {{ successMessage }}
      </p>

      <p
        v-if="errorMessage"
        class="message error-message"
        role="alert"
      >
        {{ errorMessage }}
      </p>

      <div class="form-grid">
        <div class="form-field">
          <label for="profile-first-name">
            {{ t('profile.fields.firstName') }}
          </label>
          <input
            id="profile-first-name"
            v-model="firstName"
            type="text"
            autocomplete="given-name"
            maxlength="100"
            :placeholder="t('profile.placeholders.firstName')"
          />
        </div>

        <div class="form-field">
          <label for="profile-last-name">
            {{ t('profile.fields.lastName') }}
          </label>
          <input
            id="profile-last-name"
            v-model="lastName"
            type="text"
            autocomplete="family-name"
            maxlength="100"
            :placeholder="t('profile.placeholders.lastName')"
          />
        </div>

        <div class="form-field form-field-wide">
          <label for="profile-email">
            {{ t('profile.fields.email') }}
          </label>
          <input
            id="profile-email"
            :value="profile.email"
            type="email"
            autocomplete="email"
            readonly
            aria-describedby="profile-email-help"
          />
          <small id="profile-email-help">
            {{ t('profile.fields.emailHelp') }}
          </small>
        </div>

        <div class="form-field birth-date-field">
          <label for="profile-date-of-birth-display">
            {{ t('profile.fields.birthDate') }}
          </label>
          <input
            id="profile-date-of-birth"
            v-model="dateOfBirth"
            type="hidden"
          />

          <button
            id="profile-date-of-birth-display"
            class="birth-date-trigger"
            :class="{
              'is-open': isBirthDatePickerOpen,
              'is-empty': !dateOfBirth,
            }"
            type="button"
            aria-haspopup="dialog"
            :aria-expanded="isBirthDatePickerOpen"
            @click="openBirthDatePicker"
          >
            <span>{{ displayedDateOfBirth }}</span>
            <span class="calendar-icon" aria-hidden="true">▦</span>
          </button>

          <div
            v-if="isBirthDatePickerOpen"
            class="date-picker-popover"
            role="dialog"
            aria-modal="false"
            :aria-label="t('profile.calendar.label')"
            @keydown.esc="isBirthDatePickerOpen = false"
          >
            <div class="calendar-header">
              <button
                class="month-button"
                type="button"
                :aria-label="t('profile.calendar.previousMonth')"
                @click="changeBirthCalendarMonth(-1)"
              >
                ‹
              </button>
              <div class="calendar-title-control">
                <strong class="calendar-month-name">
                  {{ birthCalendarMonthName }}
                </strong>
                <input
                  class="calendar-year-select"
                  :aria-label="t('profile.calendar.year')"
                  type="text"
                  inputmode="numeric"
                  maxlength="4"
                  pattern="[0-9]{4}"
                  :value="birthCalendarMonth.getFullYear()"
                  @change="changeBirthCalendarYear"
                />
              </div>
              <button
                class="month-button"
                type="button"
                :aria-label="t('profile.calendar.nextMonth')"
                @click="changeBirthCalendarMonth(1)"
              >
                ›
              </button>
            </div>

            <div
              class="calendar-grid calendar-weekdays"
              aria-hidden="true"
            >
              <span v-for="weekDay in weekDays" :key="weekDay">
                {{ weekDay }}
              </span>
            </div>

            <div class="calendar-grid calendar-days">
              <button
                v-for="day in birthCalendarDays"
                :key="day.key"
                class="calendar-day"
                :class="{
                  'outside-month': !day.isCurrentMonth,
                  selected: day.isSelected,
                  today: day.isToday,
                }"
                type="button"
                :disabled="day.isDisabled"
                :aria-label="day.date.toLocaleDateString(dateLocale)"
                :aria-pressed="day.isSelected"
                @click="selectBirthDate(day.date, day.isDisabled)"
              >
                {{ day.date.getDate() }}
              </button>
            </div>

            <div class="calendar-footer">
              <button
                class="clear-date-button"
                type="button"
                :disabled="!dateOfBirth"
                @click="clearBirthDate"
              >
                {{ t('profile.calendar.clear') }}
              </button>
              <button
                class="close-calendar-button"
                type="button"
                @click="isBirthDatePickerOpen = false"
              >
                {{ t('profile.calendar.close') }}
              </button>
            </div>
          </div>
        </div>

        <div class="form-field gender-field">
          <label for="profile-gender">
            {{ t('profile.fields.gender') }}
          </label>
          <select
            id="profile-gender"
            v-model="gender"
            :class="{ 'gender-placeholder': !gender }"
          >
            <option value="">{{ t('profile.gender.none') }}</option>
            <option value="Female">{{ t('profile.gender.female') }}</option>
            <option value="Male">{{ t('profile.gender.male') }}</option>
            <option value="PreferNotToSay">
              {{ t('profile.gender.preferNotToSay') }}
            </option>
          </select>
        </div>
      </div>

      <div class="form-actions">
        <button
          class="save-button"
          type="submit"
          :disabled="isSaving"
        >
          {{
            isSaving
              ? t('profile.actions.saving')
              : t('profile.actions.save')
          }}
        </button>
      </div>
    </form>

    <ChangePasswordForm v-if="profile" />
  </section>
</template>

<style scoped>
.profile-page {
  width: min(68rem, calc(100% - 2rem));
  margin: 0 auto;
  padding: 4rem 0 6rem;
}

.page-heading {
  margin-bottom: 2rem;
}

.eyebrow,
.card-eyebrow {
  margin: 0 0 0.5rem;
  color: #0c66e4;
  font-size: 0.82rem;
  font-weight: 800;
  letter-spacing: 0.16em;
}

h1 {
  margin: 0;
  color: #172b4d;
  font-size: clamp(2.5rem, 6vw, 4rem);
  line-height: 1;
}

.page-heading > p:last-child {
  margin: 1rem 0 0;
  color: #626f86;
  font-size: 1.15rem;
}

.profile-card,
.status-card {
  position: relative;
  overflow: hidden;
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

.profile-card::before {
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

.profile-card > * {
  position: relative;
}

.profile-card-heading {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 2rem;
}

.avatar {
  display: grid;
  width: 4.25rem;
  height: 4.25rem;
  flex: 0 0 auto;
  place-items: center;
  border: 1px solid #0755c7;
  border-radius: 1.2rem;
  background: linear-gradient(145deg, #2d82f7 0%, #0c66e4 55%, #0755c7 100%);
  color: #ffffff;
  font-size: 1.75rem;
  font-weight: 800;
  box-shadow:
    0 0.7rem 1.25rem rgb(12 102 228 / 25%),
    inset 0 1px rgb(255 255 255 / 55%);
}

h2 {
  margin: 0;
  color: #172b4d;
  font-size: 1.65rem;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.4rem;
}

.form-field {
  display: grid;
  gap: 1.0rem;
}

.gender-field {
  align-self: start;
}

.gender-field select {
  height: 2.7rem;
  min-height: 3.5rem;
  padding: 0.8rem 1rem;
}

.gender-field select.gender-placeholder {
  color: #8996aa;
  font-weight: 500;
}

.gender-field select option {
  color: #172b4d;
}

.form-field-wide {
  grid-column: 1 / -1;
}

label {
  color: #172b4d;
  font-weight: 750;
}

input,
select {
  position: relative;
  width: 100%;
  min-height: 3.5rem;
  padding: 0.8rem 1rem;
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
    inset 0 2px rgb(255 255 255 / 100%),
    inset 0 -2px rgb(9 30 66 / 9%);
  transition:
    transform 160ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease,
    background-position 420ms ease;
}

input:not([readonly]):hover,
select:hover {
  border-color: #82aee9;
  background-position: -70% 0, 0 0;
  box-shadow:
    0 1rem 1.65rem rgb(9 30 66 / 17%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 10%);
  transform: translateY(-0.12rem);
}

input:focus,
select:focus {
  border-color: #0c66e4;
  outline: none;
  box-shadow:
    0 0 0 0.2rem rgb(12 102 228 / 16%),
    0 1rem 1.6rem rgb(9 30 66 / 16%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 10%);
  transform: translateY(-0.08rem);
}

input[readonly] {
  border-color: #b8c4d3;
  background:
    linear-gradient(
      115deg,
      transparent 0%,
      transparent 38%,
      rgb(255 255 255 / 72%) 49%,
      transparent 60%,
      transparent 100%
    ) 100% 0 / 220% 100% no-repeat,
    linear-gradient(145deg, #f4f7fb 0%, #edf2f8 55%, #e2e9f1 100%);
  color: #626f86;
  box-shadow:
    0 0.7rem 1.2rem rgb(9 30 66 / 10%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(9 30 66 / 8%);
  cursor: not-allowed;
}

.birth-date-field {
  position: relative;
}

.birth-date-field:has(.date-picker-popover) {
  z-index: 2;
  margin-bottom: 22.5rem;
}

.birth-date-trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  min-height: 3.5rem;
  padding: 0.8rem 1rem;
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
  color: #44546f;
  font: inherit;
  font-weight: 500;
  text-align: left;
  box-shadow:
    0 0.75rem 1.25rem rgb(9 30 66 / 12%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(9 30 66 / 9%);
  cursor: pointer;
  transition:
    transform 160ms ease,
    color 150ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease,
    background-position 420ms ease;
}

.birth-date-trigger > span:first-child {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.birth-date-trigger.is-empty {
  color: #8993a4;
  font-weight: 400;
}

.birth-date-trigger:hover {
  border-color: #82aee9;
  background-position: -70% 0, 0 0;
  box-shadow:
    0 1rem 1.65rem rgb(9 30 66 / 17%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 10%);
  transform: translateY(-0.12rem);
}

.birth-date-trigger:focus-visible,
.birth-date-trigger.is-open {
  border-color: #0c66e4;
  outline: 0.15rem solid rgb(12 102 228 / 15%);
  color: #172b4d;
  font-weight: 650;
  box-shadow:
    0 0 0 0.2rem rgb(12 102 228 / 12%),
    0 1rem 1.6rem rgb(9 30 66 / 16%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 10%);
  transform: translateY(-0.08rem);
}

.birth-date-trigger.is-empty:focus-visible,
.birth-date-trigger.is-empty.is-open {
  color: #8993a4;
  font-weight: 400;
}

.calendar-icon {
  flex: 0 0 auto;
  color: #626f86;
  font-size: 1.25rem;
}

.date-picker-popover {
  position: absolute;
  z-index: 20;
  top: calc(100% + 0.5rem);
  left: 0;
  width: min(24rem, calc(100vw - 2rem));
  padding: 0.8rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.85rem;
  background: #ffffff;
  box-shadow:
    0 1.25rem 2.8rem rgb(9 30 66 / 23%),
    0 0.3rem 0.8rem rgb(9 30 66 / 12%);
}

.calendar-header {
  display: grid;
  grid-template-columns: 2.3rem 1fr 2.3rem;
  align-items: center;
  margin-bottom: 0.65rem;
  color: #172b4d;
  font-size: 1.05rem;
  text-align: center;
  text-transform: capitalize;
}

.calendar-title-control {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  min-width: 0;
}

.calendar-month-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.calendar-year-select {
  width: 4.3rem;
  min-width: 4.3rem;
  min-height: 2rem;
  padding: 0.25rem 0.4rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: #f1f5f9;
  color: #172b4d;
  font-size: 0.95rem;
  font-weight: 700;
  text-align: center;
  box-shadow: none;
  cursor: pointer;
  transform: none;
}

.calendar-year-select:hover {
  border-color: #8fb8f4;
  background: #e9f2ff;
  box-shadow: none;
  transform: none;
}

.calendar-year-select:focus {
  border-color: #0c66e4;
  outline: 0.15rem solid rgb(12 102 228 / 15%);
  box-shadow: none;
  transform: none;
}

.month-button {
  display: grid;
  width: 2.15rem;
  height: 2.15rem;
  padding: 0;
  place-items: center;
  border: 1px solid transparent;
  border-radius: 50%;
  background: #f1f5f9;
  color: #44546f;
  font-size: 1.45rem;
  cursor: pointer;
  transition:
    transform 140ms ease,
    border-color 140ms ease,
    background-color 140ms ease;
}

.month-button:hover {
  border-color: #8fb8f4;
  background: #e9f2ff;
  transform: scale(1.08);
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(2rem, 1fr));
  gap: 0.15rem;
}

.calendar-weekdays {
  padding-bottom: 0.45rem;
  border-bottom: 1px solid #dfe1e6;
  color: #626f86;
  font-size: 0.9rem;
  font-weight: 700;
  text-align: center;
}

.calendar-days {
  padding-top: 0.45rem;
}

.calendar-day {
  display: grid;
  min-width: 2.1rem;
  min-height: 2.1rem;
  padding: 0;
  place-items: center;
  border: 1px solid transparent;
  border-radius: 0.65rem;
  background: transparent;
  color: #172b4d;
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition:
    transform 130ms ease,
    color 130ms ease,
    border-color 130ms ease,
    background-color 130ms ease,
    box-shadow 130ms ease;
}

.calendar-day:not(:disabled):hover {
  z-index: 1;
  border-color: #8fb8f4;
  background: #e9f2ff;
  color: #0c66e4;
  font-size: 1.02rem;
  font-weight: 700;
  box-shadow: 0 0.35rem 0.75rem rgb(12 102 228 / 17%);
  transform: scale(1.08);
}

.calendar-day.outside-month {
  color: #a5adba;
}

.calendar-day.today {
  border-color: #8fa6c0;
}

.calendar-day.selected {
  border-color: #0c66e4;
  background: #0c66e4;
  color: #ffffff;
  font-weight: 750;
  box-shadow: 0 0.35rem 0.8rem rgb(12 102 228 / 25%);
}

.calendar-day:disabled {
  color: #c7cdd4;
  cursor: not-allowed;
  opacity: 0.62;
}

.calendar-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.55rem;
  margin-top: 0.65rem;
  padding-top: 0.65rem;
  border-top: 1px solid #dfe1e6;
}

.clear-date-button,
.close-calendar-button {
  min-height: 2.4rem;
  padding: 0.45rem 0.7rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: #ffffff;
  color: #44546f;
  font-weight: 650;
  cursor: pointer;
}

.close-calendar-button {
  border-color: #0c66e4;
  background: #0c66e4;
  color: #ffffff;
}

.clear-date-button:not(:disabled):hover,
.close-calendar-button:hover {
  box-shadow: 0 0.35rem 0.75rem rgb(9 30 66 / 15%);
  transform: translateY(-0.06rem);
}

.clear-date-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

small {
  color: #626f86;
}

.message {
  margin: 0 0 1.5rem;
  padding: 0.85rem 1rem;
  border-radius: 0.75rem;
}

.success-message {
  border: 1px solid #4bce97;
  background: #dcfff1;
  color: #216e4e;
}

.error-message,
.error-card {
  border-color: #f87168;
  background: #fff0ee;
  color: #ae2e24;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 2rem;
}

.save-button,
.status-card button {
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

.save-button:hover,
.status-card button:hover {
  box-shadow:
    0 0.9rem 1.5rem rgb(12 102 228 / 29%),
    inset 0 1px rgb(255 255 255 / 62%);
  transform: translateY(-0.12rem);
}

.save-button:disabled {
  cursor: wait;
  opacity: 0.65;
  transform: none;
}

@media (max-width: 42rem) {
  .profile-page {
    padding-top: 2.5rem;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-field-wide {
    grid-column: auto;
  }

  .save-button {
    width: 100%;
  }

  .calendar-grid {
    grid-template-columns: repeat(7, minmax(1.75rem, 1fr));
  }
}
</style>
