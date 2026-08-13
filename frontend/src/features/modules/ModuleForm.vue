<script setup lang="ts">
import { ref, watch } from 'vue'

import type { SaveModuleRequest } from './moduleModels'

const props = withDefaults(
  defineProps<{
    initialValues?: SaveModuleRequest
    isSubmitting?: boolean
    title?: string
    submitLabel?: string
  }>(),
  {
    initialValues: () => ({ name: '' }),
    isSubmitting: false,
    title: 'Neues Lernmodul',
    submitLabel: 'Lernmodul speichern',
  },
)

const emit = defineEmits<{
  save: [request: SaveModuleRequest]
  cancel: []
}>()

const name = ref('')
const code = ref('')
const description = ref('')
const color = ref('#0C66E4')

const nameError = ref('')
const codeError = ref('')
const descriptionError = ref('')
const colorError = ref('')

watch(
  () => props.initialValues,
  (values) => {
    name.value = values.name
    code.value = values.code ?? ''
    description.value = values.description ?? ''
    color.value = values.color ?? '#0C66E4'
    clearErrors()
  },
  {
    immediate: true,
    deep: true,
  },
)

function submit(): void {
  clearErrors()

  const normalizedName = name.value.trim()
  const normalizedCode = code.value.trim()
  const normalizedDescription = description.value.trim()
  const normalizedColor = color.value.trim()

  if (!normalizedName) {
    nameError.value = 'Bitte gib einen Namen ein.'
  } else if (normalizedName.length > 100) {
    nameError.value =
      'Der Name darf höchstens 100 Zeichen enthalten.'
  }

  if (normalizedCode.length > 30) {
    codeError.value =
      'Das Kürzel darf höchstens 30 Zeichen enthalten.'
  }

  if (normalizedDescription.length > 1000) {
    descriptionError.value =
      'Die Beschreibung darf höchstens 1000 Zeichen enthalten.'
  }

  if (
    normalizedColor
    && !/^#[0-9A-Fa-f]{6}$/.test(normalizedColor)
  ) {
    colorError.value =
      'Die Farbe muss das Format #RRGGBB verwenden.'
  }

  if (
    nameError.value
    || codeError.value
    || descriptionError.value
    || colorError.value
  ) {
    return
  }

  emit('save', {
    name: normalizedName,
    code: normalizedCode || null,
    description: normalizedDescription || null,
    color: normalizedColor || null,
  })
}

function clearErrors(): void {
  nameError.value = ''
  codeError.value = ''
  descriptionError.value = ''
  colorError.value = ''
}
</script>

<template>
  <form class="module-form" novalidate @submit.prevent="submit">
    <div class="form-heading">
      <div>
        <p class="form-eyebrow">LERNMODUL</p>
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
      <div class="form-field name-field">
        <label for="module-name">Name *</label>
        <input
          id="module-name"
          v-model="name"
          maxlength="100"
          autocomplete="off"
          placeholder="z. B. Sichere Systeme"
          :aria-invalid="Boolean(nameError)"
          :aria-describedby="nameError ? 'module-name-error' : undefined"
        />
        <p v-if="nameError" id="module-name-error" class="field-error">
          {{ nameError }}
        </p>
      </div>

      <div class="form-field code-field">
        <label for="module-code">Kürzel</label>
        <input
          id="module-code"
          v-model="code"
          maxlength="30"
          autocomplete="off"
          placeholder="z. B. SIS"
          :aria-invalid="Boolean(codeError)"
          :aria-describedby="codeError ? 'module-code-error' : undefined"
        />
        <p v-if="codeError" id="module-code-error" class="field-error">
          {{ codeError }}
        </p>
      </div>

      <div class="form-field description-field">
        <label for="module-description">Beschreibung</label>
        <textarea
          id="module-description"
          v-model="description"
          maxlength="1000"
          rows="4"
          placeholder="Worum geht es in diesem Lernmodul?"
          :aria-invalid="Boolean(descriptionError)"
          :aria-describedby="
            descriptionError
              ? 'module-description-error'
              : undefined
          "
        />
        <p
          v-if="descriptionError"
          id="module-description-error"
          class="field-error"
        >
          {{ descriptionError }}
        </p>
      </div>

      <div class="form-field color-field">
        <label for="module-color">Farbe</label>
        <div class="color-control">
          <input
            id="module-color"
            v-model="color"
            class="color-picker"
            type="color"
            :aria-invalid="Boolean(colorError)"
            :aria-describedby="colorError ? 'module-color-error' : undefined"
          />
          <output for="module-color">{{ color }}</output>
        </div>
        <p v-if="colorError" id="module-color-error" class="field-error">
          {{ colorError }}
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
.module-form {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  margin-bottom: 2rem;
  padding: 1.5rem;
  border: 1px solid #b6c2cf;
  border-radius: 1rem;
  background:
    linear-gradient(
      145deg,
      #ffffff 0%,
      #ffffff 54%,
      #f5f8fc 100%
    );
  box-shadow:
    0 1.15rem 2.4rem rgb(9 30 66 / 13%),
    0 0.25rem 0.7rem rgb(9 30 66 / 8%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
}

.module-form::before {
  position: absolute;
  z-index: -1;
  top: -35%;
  left: -8%;
  width: 72%;
  height: 78%;
  border-radius: 50%;
  background: rgb(255 255 255 / 72%);
  filter: blur(0.15rem);
  pointer-events: none;
  transform: rotate(-8deg);
  content: '';
}

.form-heading,
.form-actions,
.color-control {
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
  grid-template-columns: minmax(0, 2fr) minmax(10rem, 1fr);
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
  background:
    linear-gradient(
      145deg,
      #ffffff 0%,
      #ffffff 58%,
      #f3f6fa 100%
    );
  color: #172b4d;
  box-shadow:
    0 0.3rem 0.7rem rgb(9 30 66 / 7%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
  transition:
    transform 150ms ease,
    border-color 150ms ease,
    box-shadow 150ms ease;
}

input:hover,
textarea:hover {
  border-color: #8fa6c0;
  box-shadow:
    0 0.55rem 1rem rgb(9 30 66 / 12%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
  transform: translateY(-0.08rem);
}

input:focus,
textarea:focus {
  border-color: #0c66e4;
  outline: 0.15rem solid rgb(12 102 228 / 15%);
  box-shadow:
    0 0.55rem 1rem rgb(12 102 228 / 13%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
  transform: translateY(-0.08rem);
}

.color-control {
  gap: 0.75rem;
}

.color-picker {
  width: 3.5rem;
  height: 2.75rem;
  padding: 0.2rem;
  cursor: pointer;
}

output {
  color: #44546f;
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
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
  position: relative;
  overflow: hidden;
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

.primary-button:focus-visible,
.secondary-button:focus-visible,
.close-button:focus-visible {
  outline: 0.18rem solid rgb(12 102 228 / 24%);
  outline-offset: 0.15rem;
}

.secondary-button {
  border: 1px solid #b6c2cf;
  background:
    linear-gradient(
      115deg,
      transparent 0%,
      transparent 42%,
      rgb(255 255 255 / 92%) 50%,
      transparent 59%,
      transparent 100%
    ),
    linear-gradient(145deg, #ffffff 0%, #ffffff 55%, #edf2f7 100%);
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
