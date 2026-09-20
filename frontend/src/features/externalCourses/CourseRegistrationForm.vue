<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import type { RegisterCourseRequest } from './externalCourseModels'

const props = defineProps<{ isSubmitting: boolean }>()
const emit = defineEmits<{
  register: [request: RegisterCourseRequest]
}>()

const { t } = useI18n()
const courseUrl = ref('')
const validationMessage = ref('')

function submit(): void {
  const normalizedUrl = courseUrl.value.trim()
  validationMessage.value = ''

  if (!normalizedUrl) {
    validationMessage.value = t(
      'externalCourses.form.validation.required',
    )
    return
  }

  try {
    const url = new URL(normalizedUrl)
    if (url.protocol !== 'https:') {
      throw new Error('Unsupported protocol')
    }
  } catch {
    validationMessage.value = t(
      'externalCourses.form.validation.invalid',
    )
    return
  }

  emit('register', { courseUrl: normalizedUrl })
}
</script>

<template>
  <form class="course-registration-form" novalidate @submit.prevent="submit">
    <div class="form-field">
      <label for="course-url">
        {{ t('externalCourses.form.label') }}
      </label>
      <input
        id="course-url"
        v-model="courseUrl"
        type="url"
        maxlength="2048"
        :placeholder="t('externalCourses.form.placeholder')"
        :aria-invalid="Boolean(validationMessage)"
        :aria-describedby="validationMessage ? 'course-url-error' : undefined"
        :disabled="props.isSubmitting"
      >
      <p
        v-if="validationMessage"
        id="course-url-error"
        class="validation-message"
        role="alert"
      >
        {{ validationMessage }}
      </p>
    </div>

    <button type="submit" :disabled="props.isSubmitting">
      {{ props.isSubmitting
        ? t('externalCourses.form.submitting')
        : t('externalCourses.form.submit') }}
    </button>
  </form>
</template>

<style scoped>
.course-registration-form {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.9rem;
  background: #ffffff;
}

.form-field {
  display: grid;
  flex: 1;
  gap: 0.35rem;
}

label {
  color: #172b4d;
  font-weight: 700;
}

input {
  min-height: 2.7rem;
  padding: 0.6rem 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.6rem;
  font: inherit;
}

button {
  min-height: 2.7rem;
  margin-top: 1.55rem;
  padding: 0.6rem 1rem;
  border: 0;
  border-radius: 0.6rem;
  background: #0c66e4;
  color: #ffffff;
  font-weight: 700;
  cursor: pointer;
}

button:disabled {
  cursor: wait;
  opacity: 0.65;
}

.validation-message {
  margin: 0;
  color: #ae2a19;
}

@media (max-width: 42rem) {
  .course-registration-form {
    display: grid;
  }

  button {
    margin-top: 0;
  }
}
</style>
