<script setup lang="ts">
import { ref, watch } from 'vue'

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
        <label for="task-due-date">Fällig am *</label>
        <input
          id="task-due-date"
          v-model="dueDate"
          type="datetime-local"
          :aria-invalid="Boolean(dueDateError)"
          :aria-describedby="
            dueDateError ? 'task-due-date-error' : undefined
          "
        />
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
  margin-bottom: 2rem;
  padding: 1.5rem;
  border: 1px solid #b6c2cf;
  border-radius: 1rem;
  background: #ffffff;
  box-shadow: 0 0.5rem 1.5rem rgb(9 30 66 / 8%);
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
  font-weight: 650;
}

input,
textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: #ffffff;
  color: #172b4d;
}

input:focus,
textarea:focus {
  border-color: #0c66e4;
  outline: 0.15rem solid rgb(12 102 228 / 15%);
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
}

.primary-button {
  border: 1px solid #0c66e4;
  background: #0c66e4;
  color: #ffffff;
}

.secondary-button {
  border: 1px solid #b6c2cf;
  background: #ffffff;
  color: #172b4d;
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

  .form-actions {
    align-items: stretch;
    flex-direction: column-reverse;
  }
}
</style>
