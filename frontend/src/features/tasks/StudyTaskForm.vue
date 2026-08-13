<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import type { SaveStudyTaskRequest } from './taskModels'

const props = withDefaults(
  defineProps<{
    initialValues?: SaveStudyTaskRequest
    isSubmitting?: boolean
    title?: string
    submitLabel?: string
  }>(),
  {
    initialValues: () => ({
      title: '',
      dueDateUtc: '',
    }),
    isSubmitting: false,
    title: 'Neue Aufgabe',
    submitLabel: 'Aufgabe speichern',
  },
)

const emit = defineEmits<{
  save: [request: SaveStudyTaskRequest]
  cancel: []
}>()

const taskTitle = ref('')
const description = ref('')
const dueDate = ref('')
const titleError = ref('')
const descriptionError = ref('')
const dueDateError = ref('')
const isDatePickerOpen = ref(false)
const calendarMonth = ref(startOfMonth(new Date()))

const weekDays = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']

const displayedDueDate = computed(() => {
  const date = parseLocalDateTime(dueDate.value)

  if (!date) {
    return 'Datum und Uhrzeit auswählen'
  }

  return new Intl.DateTimeFormat('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
})

const calendarTitle = computed(() =>
  new Intl.DateTimeFormat('de-DE', {
    month: 'long',
    year: 'numeric',
  }).format(calendarMonth.value),
)

const calendarDays = computed(() => {
  const firstDay = startOfMonth(calendarMonth.value)
  const mondayOffset = (firstDay.getDay() + 6) % 7
  const firstVisibleDay = new Date(
    firstDay.getFullYear(),
    firstDay.getMonth(),
    firstDay.getDate() - mondayOffset,
  )

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(
      firstVisibleDay.getFullYear(),
      firstVisibleDay.getMonth(),
      firstVisibleDay.getDate() + index,
    )

    return {
      date,
      key: toDateKey(date),
      isCurrentMonth:
        date.getMonth() === calendarMonth.value.getMonth(),
      isSelected: isSameDay(
        date,
        parseLocalDateTime(dueDate.value),
      ),
      isToday: isSameDay(date, new Date()),
    }
  })
})

const selectedTime = computed(() => {
  const date = parseLocalDateTime(dueDate.value)

  if (!date) {
    return '12:00'
  }

  return `${pad(date.getHours())}:${pad(date.getMinutes())}`
})

watch(
  () => props.initialValues,
  (values) => {
    taskTitle.value = values.title
    description.value = values.description ?? ''
    dueDate.value = toLocalDateTime(values.dueDateUtc)
    clearErrors()
  },
  {
    immediate: true,
    deep: true,
  },
)

function submit(): void {
  clearErrors()

  const normalizedTitle = taskTitle.value.trim()
  const normalizedDescription = description.value.trim()
  const parsedDueDate = new Date(dueDate.value)

  if (!normalizedTitle) {
    titleError.value = 'Bitte gib einen Titel ein.'
  } else if (normalizedTitle.length > 200) {
    titleError.value =
      'Der Titel darf höchstens 200 Zeichen enthalten.'
  }

  if (normalizedDescription.length > 2000) {
    descriptionError.value =
      'Die Beschreibung darf höchstens 2000 Zeichen enthalten.'
  }

  if (!dueDate.value) {
    dueDateError.value = 'Bitte gib ein Fälligkeitsdatum ein.'
  } else if (Number.isNaN(parsedDueDate.getTime())) {
    dueDateError.value = 'Das Fälligkeitsdatum ist ungültig.'
  }

  if (
    titleError.value
    || descriptionError.value
    || dueDateError.value
  ) {
    return
  }

  emit('save', {
    title: normalizedTitle,
    description: normalizedDescription || null,
    dueDateUtc: parsedDueDate.toISOString(),
  })
}

function openDatePicker(): void {
  const selectedDate = parseLocalDateTime(dueDate.value)
  calendarMonth.value = startOfMonth(selectedDate ?? new Date())
  isDatePickerOpen.value = true
}

function changeMonth(offset: number): void {
  calendarMonth.value = new Date(
    calendarMonth.value.getFullYear(),
    calendarMonth.value.getMonth() + offset,
    1,
  )
}

function selectDay(date: Date): void {
  const previousDate = parseLocalDateTime(dueDate.value)
  const nextDate = new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
    previousDate?.getHours() ?? 12,
    previousDate?.getMinutes() ?? 0,
  )

  dueDate.value = formatLocalDateTime(nextDate)
  dueDateError.value = ''
  isDatePickerOpen.value = false
}

function updateTime(event: Event): void {
  const value = (event.target as HTMLInputElement).value
  const [hoursValue, minutesValue] = value.split(':')

  if (hoursValue === undefined || minutesValue === undefined) {
    return
  }

  const hours = Number(hoursValue)
  const minutes = Number(minutesValue)

  if (!Number.isInteger(hours) || !Number.isInteger(minutes)) {
    return
  }

  const previousDate = parseLocalDateTime(dueDate.value)
  const nextDate = previousDate ?? new Date()
  nextDate.setHours(hours, minutes, 0, 0)
  dueDate.value = formatLocalDateTime(nextDate)
  calendarMonth.value = startOfMonth(nextDate)
  dueDateError.value = ''
}

function parseLocalDateTime(value: string): Date | null {
  if (!value) {
    return null
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

function formatLocalDateTime(date: Date): string {
  return [
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`,
    `${pad(date.getHours())}:${pad(date.getMinutes())}`,
  ].join('T')
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

function toDateKey(date: Date): string {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
}

function isSameDay(first: Date, second: Date | null): boolean {
  return Boolean(
    second
      && first.getFullYear() === second.getFullYear()
      && first.getMonth() === second.getMonth()
      && first.getDate() === second.getDate(),
  )
}

function pad(value: number): string {
  return value.toString().padStart(2, '0')
}

function toLocalDateTime(value: string): string {
  if (!value) {
    return ''
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const offsetInMilliseconds =
    date.getTimezoneOffset() * 60_000

  return new Date(date.getTime() - offsetInMilliseconds)
    .toISOString()
    .slice(0, 16)
}

function clearErrors(): void {
  titleError.value = ''
  descriptionError.value = ''
  dueDateError.value = ''
}
</script>

<template>
  <form class="task-form" novalidate @submit.prevent="submit">
    <div class="form-heading">
      <div>
        <p class="form-eyebrow">AUFGABE</p>
        <h2>{{ title }}</h2>
      </div>

      <button
        class="close-button"
        type="button"
        aria-label="Formular schließen"
        :disabled="isSubmitting"
        @click="emit('cancel')"
      >
        ×
      </button>
    </div>

    <div class="form-grid">
      <div class="form-field title-field">
        <label for="task-title">Titel *</label>
        <input
          id="task-title"
          v-model="taskTitle"
          maxlength="200"
          autocomplete="off"
          placeholder="z. B. Kapitel 4 wiederholen"
          :aria-invalid="Boolean(titleError)"
          :aria-describedby="titleError ? 'task-title-error' : undefined"
        />
        <p v-if="titleError" id="task-title-error" class="field-error">
          {{ titleError }}
        </p>
      </div>

      <div class="form-field due-date-field">
        <label for="task-due-date-display">Fällig am *</label>
        <input
          id="task-due-date"
          v-model="dueDate"
          type="hidden"
          :aria-invalid="Boolean(dueDateError)"
          :aria-describedby="
            dueDateError ? 'task-due-date-error' : undefined
          "
        />
        <button
          id="task-due-date-display"
          class="date-picker-trigger"
          :class="{
            'is-open': isDatePickerOpen,
            'is-empty': !dueDate,
            'has-value': Boolean(dueDate),
            'has-error': dueDateError,
          }"
          type="button"
          aria-haspopup="dialog"
          :aria-expanded="isDatePickerOpen"
          :aria-describedby="
            dueDateError ? 'task-due-date-error' : undefined
          "
          @click="openDatePicker"
        >
          <span>{{ displayedDueDate }}</span>
          <span class="calendar-icon" aria-hidden="true">▦</span>
        </button>

        <div
          v-if="isDatePickerOpen"
          class="date-picker-popover"
          role="dialog"
          aria-modal="false"
          aria-label="Fälligkeitsdatum auswählen"
          @keydown.esc="isDatePickerOpen = false"
        >
          <div class="calendar-header">
            <button
              class="month-button"
              type="button"
              aria-label="Vorheriger Monat"
              @click="changeMonth(-1)"
            >
              ‹
            </button>
            <strong>{{ calendarTitle }}</strong>
            <button
              class="month-button"
              type="button"
              aria-label="Nächster Monat"
              @click="changeMonth(1)"
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
              v-for="day in calendarDays"
              :key="day.key"
              class="calendar-day"
              :class="{
                'outside-month': !day.isCurrentMonth,
                selected: day.isSelected,
                today: day.isToday,
              }"
              type="button"
              :aria-label="day.date.toLocaleDateString('de-DE')"
              :aria-pressed="day.isSelected"
              @click="selectDay(day.date)"
            >
              {{ day.date.getDate() }}
            </button>
          </div>

          <div class="calendar-footer">
            <label for="task-due-time">Uhrzeit</label>
            <input
              id="task-due-time"
              type="time"
              :value="selectedTime"
              @input="updateTime"
            />
            <button
              class="date-picker-apply"
              type="button"
              :disabled="!dueDate"
              @click="isDatePickerOpen = false"
            >
              Übernehmen
            </button>
          </div>
        </div>
        <p
          v-if="dueDateError"
          id="task-due-date-error"
          class="field-error"
        >
          {{ dueDateError }}
        </p>
      </div>

      <div class="form-field description-field">
        <label for="task-description">Beschreibung</label>
        <textarea
          id="task-description"
          v-model="description"
          maxlength="2000"
          rows="4"
          placeholder="Was möchtest du für diese Aufgabe erledigen?"
          :aria-invalid="Boolean(descriptionError)"
          :aria-describedby="
            descriptionError
              ? 'task-description-error'
              : undefined
          "
        />
        <p
          v-if="descriptionError"
          id="task-description-error"
          class="field-error"
        >
          {{ descriptionError }}
        </p>
      </div>
    </div>

    <div class="form-actions">
      <button
        class="secondary-button"
        type="button"
        :disabled="isSubmitting"
        @click="emit('cancel')"
      >
        Abbrechen
      </button>
      <button
        class="primary-button"
        type="submit"
        :disabled="isSubmitting"
      >
        {{ isSubmitting ? 'Wird gespeichert …' : submitLabel }}
      </button>
    </div>
  </form>
</template>

<style scoped>
.task-form {
  position: relative;
  isolation: isolate;
  overflow: visible;
  margin-bottom: 2rem;
  padding: 1.5rem;
  border: 1px solid #b6c2cf;
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

.form-heading,
.form-actions {
  display: flex;
  align-items: center;
}

.form-heading {
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.form-eyebrow {
  margin: 0 0 0.25rem;
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.1em;
}

h2 {
  margin: 0;
  color: #172b4d;
}

.close-button {
  padding: 0.25rem 0.6rem;
  border: 0;
  background: transparent;
  color: #626f86;
  font-size: 1.75rem;
  cursor: pointer;
}

.form-grid {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(14rem, 1fr);
  gap: 1.25rem;
}

.form-field {
  display: grid;
  align-content: start;
  gap: 0.45rem;
}

.description-field {
  grid-column: 1 / -1;
}

label {
  color: #172b4d;
  font-size: 1rem;
  font-weight: 650;
  line-height: 1.25;
}

input,
textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: linear-gradient(
    145deg,
    #ffffff 0%,
    #ffffff 58%,
    #f3f6fa 100%
  );
  color: #172b4d;
  font-size: 1rem;
  line-height: 1.5;
  box-shadow: 0 0.3rem 0.7rem rgb(9 30 66 / 7%);
  transition:
    transform 150ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease;
}

.title-field input,
.date-picker-trigger {
  height: 3rem;
}

.description-field textarea {
  min-height: 7.5rem;
  font-size: 1rem;
  font-weight: 400;
  line-height: 1.5;
}

input::placeholder,
textarea::placeholder {
  color: #8993a4;
  font-size: 1rem;
  font-weight: 400;
  opacity: 1;
}

.due-date-field {
  position: relative;
}

.date-picker-trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  min-height: 0;
  padding: 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: linear-gradient(
    145deg,
    #ffffff 0%,
    #ffffff 58%,
    #f3f6fa 100%
  );
  color: #44546f;
  font: inherit;
  font-size: 1rem;
  font-weight: 500;
  line-height: 1.5;
  text-align: left;
  box-shadow: 0 0.3rem 0.7rem rgb(9 30 66 / 7%);
  cursor: pointer;
  transition:
    color 150ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease;
}

.date-picker-trigger > span:first-child {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.date-picker-trigger.is-empty {
  color: #8993a4;
  font-size: 1rem;
  font-weight: 400;
}

.date-picker-trigger.has-value {
  color: #44546f;
  font-weight: 500;
}

.date-picker-trigger:hover {
  border-color: #8fa6c0;
  box-shadow: 0 0.45rem 0.9rem rgb(9 30 66 / 10%);
}

.date-picker-trigger:focus-visible,
.date-picker-trigger.is-open {
  border-color: #0c66e4;
  outline: 0.15rem solid rgb(12 102 228 / 15%);
  color: #172b4d;
  font-weight: 650;
  box-shadow: 0 0.55rem 1rem rgb(12 102 228 / 13%);
}

.date-picker-trigger.is-empty:focus-visible,
.date-picker-trigger.is-empty.is-open {
  color: #8993a4;
  font-weight: 400;
}

.date-picker-trigger.has-error {
  border-color: #ae2e24;
}

.calendar-icon {
  color: #626f86;
  font-size: 1.25rem;
}

.date-picker-popover {
  position: absolute;
  z-index: 20;
  top: calc(100% + 0.5rem);
  right: 0;
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

.calendar-day:hover {
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

.calendar-footer {
  display: grid;
  grid-template-columns: auto minmax(7rem, 1fr) auto;
  align-items: center;
  gap: 0.55rem;
  margin-top: 0.65rem;
  padding-top: 0.65rem;
  border-top: 1px solid #dfe1e6;
}

.calendar-footer input {
  min-height: 2.65rem;
  padding: 0.45rem 0.65rem;
  font-size: 1rem;
  font-weight: 600;
}

.date-picker-apply {
  min-height: 2.65rem;
  padding: 0.5rem 0.9rem;
  border: 1px solid #0c66e4;
  border-radius: 0.5rem;
  background: #0c66e4;
  color: #ffffff;
  font-weight: 650;
  cursor: pointer;
}

.date-picker-apply:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

input:hover,
textarea:hover {
  border-color: #8fa6c0;
  box-shadow: 0 0.55rem 1rem rgb(9 30 66 / 12%);
  transform: translateY(-0.08rem);
}

input:focus,
textarea:focus {
  border-color: #0c66e4;
  outline: 0.15rem solid rgb(12 102 228 / 15%);
  box-shadow: 0 0.55rem 1rem rgb(12 102 228 / 13%);
  transform: translateY(-0.08rem);
}

.field-error {
  margin: 0;
  color: #ae2e24;
  font-size: 0.875rem;
}

.form-actions {
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.5rem;
}

.primary-button,
.secondary-button {
  padding: 0.7rem 1rem;
  border-radius: 0.5rem;
  font-weight: 650;
  cursor: pointer;
  box-shadow: 0 0.45rem 0.9rem rgb(9 30 66 / 13%);
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    border-color 150ms ease;
}

.primary-button {
  border: 1px solid #0c66e4;
  background: linear-gradient(
    145deg,
    #1f7bf2 0%,
    #0c66e4 58%,
    #0754bd 100%
  );
  color: #ffffff;
}

.secondary-button {
  border: 1px solid #b6c2cf;
  background:
    linear-gradient(
      115deg,
      transparent 0%,
      transparent 42%,
      rgb(255 255 255 / 90%) 50%,
      transparent 59%,
      transparent 100%
    ),
    linear-gradient(145deg, #ffffff 0%, #edf2f7 100%);
  color: #172b4d;
}

.primary-button:hover:not(:disabled),
.secondary-button:hover:not(:disabled),
.close-button:hover:not(:disabled) {
  box-shadow: 0 0.75rem 1.25rem rgb(9 30 66 / 18%);
  transform: translateY(-0.12rem);
}

.primary-button:active:not(:disabled),
.secondary-button:active:not(:disabled),
.close-button:active:not(:disabled) {
  transform: translateY(0.04rem);
}

.primary-button:focus-visible,
.secondary-button:focus-visible,
.close-button:focus-visible {
  outline: 0.18rem solid rgb(12 102 228 / 24%);
  outline-offset: 0.15rem;
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

@media (max-width: 40rem) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .description-field {
    grid-column: auto;
  }

  .date-picker-popover {
    right: auto;
    left: 0;
  }

  .calendar-grid {
    grid-template-columns: repeat(7, minmax(2.25rem, 1fr));
  }

  .calendar-footer {
    grid-template-columns: 1fr 1fr;
  }

  .calendar-footer label {
    grid-column: 1 / -1;
  }

  .form-actions {
    align-items: stretch;
    flex-direction: column-reverse;
  }
}
</style>
