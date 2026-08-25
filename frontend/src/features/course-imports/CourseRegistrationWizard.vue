<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import type { StudyModule } from '@/features/modules/moduleModels'
import { ApiError } from '@/services/api/apiClient'

import { courseImportService } from './courseImportService'
import type { CourseSubscription } from './courseImportModels'

const props = defineProps<{
  modules: StudyModule[]
}>()

const emit = defineEmits<{
  registered: [subscription: CourseSubscription]
  cancel: []
}>()

const { t } = useI18n()
const step = ref<1 | 2 | 3>(1)
const courseUrl = ref('')
const selectedModuleId = ref('')
const isSubmitting = ref(false)
const errorMessage = ref('')
const urlErrorMessage = ref('')
const moduleErrorMessage = ref('')
const stepHeading = ref<HTMLElement | null>(null)

const selectedModule = computed(() =>
  props.modules.find((module) => module.id === selectedModuleId.value),
)

async function goToStep(nextStep: 1 | 2 | 3): Promise<void> {
  errorMessage.value = ''
  step.value = nextStep
  await nextTick()
  stepHeading.value?.focus()
}

async function continueToModules(): Promise<void> {
  const normalizedUrl = courseUrl.value.trim()
  urlErrorMessage.value = ''

  if (!normalizedUrl) {
    urlErrorMessage.value = t('courseImports.registration.validation.urlRequired')
    return
  }

  if (normalizedUrl.length > 2048) {
    urlErrorMessage.value = t('courseImports.registration.validation.urlMax')
    return
  }

  if (!isAbsoluteUrl(normalizedUrl)) {
    urlErrorMessage.value = t('courseImports.registration.validation.urlInvalid')
    return
  }

  await goToStep(2)
}

function isAbsoluteUrl(value: string): boolean {
  try {
    return Boolean(new URL(value).host)
  } catch {
    return false
  }
}

async function continueToSummary(): Promise<void> {
  moduleErrorMessage.value = ''
  if (!selectedModule.value) {
    moduleErrorMessage.value = t('courseImports.registration.validation.moduleRequired')
    return
  }

  await goToStep(3)
}

async function registerCourse(): Promise<void> {
  if (!selectedModule.value || isSubmitting.value) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const result = await courseImportService.register(
      selectedModule.value.id,
      courseUrl.value.trim(),
    )
    emit('registered', result.data)
  } catch (error: unknown) {
    errorMessage.value = getRegistrationErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

function getRegistrationErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return t('courseImports.registration.errors.unexpected')
  }

  const messages: Record<string, string> = {
    'validation-error': t('courseImports.registration.errors.validation'),
    'unsupported-course-url': t('courseImports.registration.errors.unsupportedCourseUrl'),
    'module-already-subscribed': t('courseImports.registration.errors.moduleAlreadySubscribed'),
    'course-already-subscribed': t('courseImports.registration.errors.courseAlreadySubscribed'),
  }

  return error.problem?.code ? (messages[error.problem.code] ?? error.message) : error.message
}
</script>

<template>
  <section class="course-registration" aria-labelledby="course-registration-title">
    <header class="registration-header">
      <div>
        <p class="registration-eyebrow">{{ t('courseImports.registration.eyebrow') }}</p>
        <h2 id="course-registration-title">{{ t('courseImports.registration.title') }}</h2>
        <p>{{ t('courseImports.registration.description') }}</p>
      </div>
      <button class="cancel-registration-button" type="button" @click="emit('cancel')">
        {{ t('courseImports.registration.cancel') }}
      </button>
    </header>

    <ol class="registration-steps" :aria-label="t('courseImports.registration.progressLabel')">
      <li
        :class="{ active: step === 1, completed: step > 1 }"
        :aria-current="step === 1 ? 'step' : undefined"
      >
        <span aria-hidden="true">1</span>{{ t('courseImports.registration.steps.link') }}
      </li>
      <li
        :class="{ active: step === 2, completed: step > 2 }"
        :aria-current="step === 2 ? 'step' : undefined"
      >
        <span aria-hidden="true">2</span>{{ t('courseImports.registration.steps.module') }}
      </li>
      <li :class="{ active: step === 3 }" :aria-current="step === 3 ? 'step' : undefined">
        <span aria-hidden="true">3</span>{{ t('courseImports.registration.steps.summary') }}
      </li>
    </ol>

    <p v-if="errorMessage" class="registration-error" role="alert">
      {{ errorMessage }}
    </p>

    <form v-if="step === 1" class="registration-panel" @submit.prevent="continueToModules">
      <h3 ref="stepHeading" tabindex="-1">{{ t('courseImports.registration.link.title') }}</h3>
      <p>{{ t('courseImports.registration.link.description') }}</p>
      <label for="course-registration-url">{{ t('courseImports.registration.link.label') }}</label>
      <input
        id="course-registration-url"
        v-model="courseUrl"
        type="url"
        maxlength="2048"
        autocomplete="url"
        :aria-invalid="Boolean(urlErrorMessage)"
        :aria-describedby="urlErrorMessage ? 'course-registration-url-error' : undefined"
        :placeholder="t('courseImports.registration.link.placeholder')"
        @input="urlErrorMessage = ''"
      />
      <p v-if="urlErrorMessage" id="course-registration-url-error" class="field-error" role="alert">
        {{ urlErrorMessage }}
      </p>
      <p class="privacy-note">{{ t('courseImports.registration.link.privacy') }}</p>
      <div class="registration-actions end">
        <button class="primary-button continue-to-modules-button" type="submit">
          {{ t('courseImports.registration.link.continue') }}
        </button>
      </div>
    </form>

    <form v-else-if="step === 2" class="registration-panel" @submit.prevent="continueToSummary">
      <h3 ref="stepHeading" tabindex="-1">{{ t('courseImports.registration.module.title') }}</h3>
      <p>{{ t('courseImports.registration.module.description') }}</p>
      <fieldset
        class="module-options"
        :aria-invalid="Boolean(moduleErrorMessage)"
        :aria-describedby="moduleErrorMessage ? 'course-registration-module-error' : undefined"
      >
        <legend class="visually-hidden">{{ t('courseImports.registration.module.legend') }}</legend>
        <label
          v-for="module in modules"
          :key="module.id"
          class="module-option"
          :class="{ selected: selectedModuleId === module.id }"
        >
          <input
            v-model="selectedModuleId"
            type="radio"
            name="course-module"
            :value="module.id"
            @change="moduleErrorMessage = ''"
          />
          <span
            class="module-color"
            :style="{ backgroundColor: module.color ?? '#0c66e4' }"
            aria-hidden="true"
          />
          <span
            ><strong>{{ module.name }}</strong
            ><small>{{ module.code || t('courseImports.registration.module.noCode') }}</small></span
          >
        </label>
      </fieldset>
      <p
        v-if="moduleErrorMessage"
        id="course-registration-module-error"
        class="field-error"
        role="alert"
      >
        {{ moduleErrorMessage }}
      </p>
      <div class="registration-actions">
        <button class="secondary-button" type="button" @click="goToStep(1)">
          {{ t('courseImports.registration.back') }}
        </button>
        <button class="primary-button continue-to-summary-button" type="submit">
          {{ t('courseImports.registration.module.continue') }}
        </button>
      </div>
    </form>

    <form v-else class="registration-panel" @submit.prevent="registerCourse">
      <h3 ref="stepHeading" tabindex="-1">{{ t('courseImports.registration.summary.title') }}</h3>
      <p>{{ t('courseImports.registration.summary.description') }}</p>
      <dl class="registration-summary">
        <div>
          <dt>{{ t('courseImports.registration.summary.course') }}</dt>
          <dd>{{ courseUrl }}</dd>
        </div>
        <div>
          <dt>{{ t('courseImports.registration.summary.module') }}</dt>
          <dd>{{ selectedModule?.name }}</dd>
        </div>
        <div>
          <dt>{{ t('courseImports.registration.summary.scan') }}</dt>
          <dd>{{ t('courseImports.registration.summary.scanValue') }}</dd>
        </div>
      </dl>
      <div class="registration-actions">
        <button
          class="secondary-button"
          type="button"
          :disabled="isSubmitting"
          @click="goToStep(2)"
        >
          {{ t('courseImports.registration.back') }}
        </button>
        <button
          class="primary-button confirm-registration-button"
          type="submit"
          :disabled="isSubmitting"
        >
          {{
            isSubmitting
              ? t('courseImports.registration.summary.submitting')
              : t('courseImports.registration.summary.confirm')
          }}
        </button>
      </div>
    </form>
  </section>
</template>

<style scoped>
.course-registration {
  margin-bottom: 2rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-radius: 1.25rem;
  background: #fff;
  box-shadow: 0 1rem 2.5rem rgb(9 30 66 / 12%);
}
.registration-header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.5rem;
  border-bottom: 1px solid #dfe3e8;
  background: linear-gradient(135deg, #eef6ff, #fff);
}
.registration-header h2,
.registration-header p {
  margin: 0;
}
.registration-header p:last-child {
  margin-top: 0.4rem;
  color: #626f86;
}
.registration-eyebrow {
  margin-bottom: 0.35rem !important;
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.1em;
}
.cancel-registration-button,
.primary-button,
.secondary-button {
  padding: 0.65rem 0.9rem;
  border-radius: 0.55rem;
  font-weight: 700;
  cursor: pointer;
}
.cancel-registration-button,
.secondary-button {
  border: 1px solid #b6c2cf;
  background: #fff;
  color: #172b4d;
}
.primary-button {
  border: 1px solid #0c66e4;
  background: #0c66e4;
  color: #fff;
}
button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}
.registration-steps {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  margin: 0;
  padding: 1rem 1.5rem;
  border-bottom: 1px solid #dfe3e8;
  background: #f7f8fa;
  list-style: none;
}
.registration-steps li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #7a869a;
  font-weight: 700;
}
.registration-steps li:not(:last-child)::after {
  height: 1px;
  flex: 1;
  margin-right: 0.75rem;
  background: #dfe3e8;
  content: '';
}
.registration-steps span {
  display: grid;
  width: 1.8rem;
  height: 1.8rem;
  place-items: center;
  border-radius: 50%;
  background: #dfe3e8;
}
.registration-steps .active {
  color: #0c66e4;
}
.registration-steps .active span {
  background: #0c66e4;
  color: #fff;
}
.registration-steps .completed {
  color: #216e4e;
}
.registration-steps .completed span {
  background: #22a06b;
  color: #fff;
}
.registration-error {
  margin: 1rem 1.5rem 0;
  padding: 0.85rem 1rem;
  border: 1px solid #f5b7b1;
  border-radius: 0.6rem;
  background: #ffebe6;
  color: #ae2e24;
}
.registration-panel {
  padding: 1.75rem;
}
.registration-panel h3 {
  margin: 0;
  color: #172b4d;
  font-size: 1.35rem;
}
.registration-panel > p {
  color: #626f86;
  line-height: 1.55;
}
.registration-panel > label {
  display: block;
  margin-bottom: 0.45rem;
  font-weight: 700;
}
.registration-panel > input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.55rem;
}
.registration-panel > input[aria-invalid='true'] {
  border-color: #c9372c;
}
.field-error {
  margin: 0.45rem 0 0 !important;
  color: #ae2e24 !important;
  font-size: 0.9rem;
}
.privacy-note {
  padding: 0.85rem 1rem;
  border-left: 0.25rem solid #0c66e4;
  background: #eef6ff;
  color: #44546f !important;
}
.module-options {
  display: grid;
  gap: 0.75rem;
  margin: 1.25rem 0 0;
  padding: 0;
  border: 0;
}
.module-option {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.75rem;
  cursor: pointer;
}
.module-option.selected {
  border-color: #0c66e4;
  background: #eef6ff;
}
.module-option small {
  display: block;
  margin-top: 0.2rem;
  color: #626f86;
}
.module-color {
  width: 0.65rem;
  height: 2.5rem;
  border-radius: 999px;
}
.registration-summary {
  display: grid;
  gap: 0.75rem;
  margin: 1.25rem 0;
}
.registration-summary div {
  padding: 0.9rem 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.65rem;
  background: #f7f8fa;
}
.registration-summary dt {
  margin-bottom: 0.25rem;
  color: #626f86;
  font-size: 0.8rem;
  font-weight: 700;
}
.registration-summary dd {
  margin: 0;
  overflow-wrap: anywhere;
  font-weight: 700;
}
.registration-actions {
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
  margin-top: 1.5rem;
}
.registration-actions.end {
  justify-content: flex-end;
}
.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
@media (max-width: 40rem) {
  .registration-header {
    display: block;
  }
  .cancel-registration-button {
    margin-top: 1rem;
  }
  .registration-steps {
    gap: 0.5rem;
  }
  .registration-steps li::after {
    display: none;
  }
  .registration-actions {
    flex-direction: column;
  }
}
</style>
