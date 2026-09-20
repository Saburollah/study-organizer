<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'

import CourseRegistrationForm from '@/features/externalCourses/CourseRegistrationForm.vue'
import type {
  CourseScanSummary,
  CourseSubscription,
  ExternalContentStatus,
  ExternalCourseContent,
  RegisterCourseRequest,
} from '@/features/externalCourses/externalCourseModels'
import { externalCourseService } from '@/features/externalCourses/externalCourseService'
import { ApiError } from '@/services/api/apiClient'

const { locale, t } = useI18n()
const subscriptions = ref<CourseSubscription[]>([])
const contentsBySubscription = ref<Record<string, ExternalCourseContent[]>>({})
const summariesBySubscription = ref<Record<string, CourseScanSummary>>({})
const isLoading = ref(true)
const isRegistering = ref(false)
const scanningSubscriptionId = ref<string | null>(null)
const errorMessage = ref('')
const successMessage = ref('')

onMounted(() => {
  void loadSubscriptions()
})

async function loadSubscriptions(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''

  try {
    subscriptions.value = await externalCourseService.getAll()
    await Promise.all(
      subscriptions.value.map((subscription) =>
        loadContents(subscription.id),
      ),
    )
  } catch (error: unknown) {
    errorMessage.value = getErrorMessage(
      error,
      'externalCourses.errors.load',
    )
  } finally {
    isLoading.value = false
  }
}

async function loadContents(subscriptionId: string): Promise<void> {
  const contents = await externalCourseService.getContents(subscriptionId)
  contentsBySubscription.value = {
    ...contentsBySubscription.value,
    [subscriptionId]: contents,
  }
}

async function registerCourse(request: RegisterCourseRequest): Promise<void> {
  isRegistering.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const subscription = await externalCourseService.register(request)
    subscriptions.value = [
      subscription,
      ...subscriptions.value.filter((item) => item.id !== subscription.id),
    ]
    await loadContents(subscription.id)
    successMessage.value = t('externalCourses.success.registered')
  } catch (error: unknown) {
    errorMessage.value = getErrorMessage(
      error,
      'externalCourses.errors.register',
    )
  } finally {
    isRegistering.value = false
  }
}

async function scanCourse(subscriptionId: string): Promise<void> {
  scanningSubscriptionId.value = subscriptionId
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const summary = await externalCourseService.scan(subscriptionId)
    summariesBySubscription.value = {
      ...summariesBySubscription.value,
      [subscriptionId]: summary,
    }
    await loadContents(subscriptionId)
  } catch (error: unknown) {
    errorMessage.value = getErrorMessage(
      error,
      'externalCourses.errors.scan',
    )
  } finally {
    scanningSubscriptionId.value = null
  }
}

function contentsFor(subscriptionId: string): ExternalCourseContent[] {
  return contentsBySubscription.value[subscriptionId] ?? []
}

function statusClass(status: ExternalContentStatus): string {
  return `content-status-${status.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()}`
}

function formatDate(value: string | null): string {
  if (!value) {
    return t('externalCourses.contents.noDueDate')
  }

  return new Intl.DateTimeFormat(locale.value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function getErrorMessage(error: unknown, fallbackKey: string): string {
  return error instanceof ApiError ? error.message : t(fallbackKey)
}
</script>

<template>
  <section class="moodle-courses-page">
    <header class="page-header">
      <p class="eyebrow">{{ t('externalCourses.eyebrow') }}</p>
      <h1>{{ t('externalCourses.title') }}</h1>
      <p>{{ t('externalCourses.description') }}</p>
    </header>

    <CourseRegistrationForm
      :is-submitting="isRegistering"
      @register="registerCourse"
    />

    <p v-if="successMessage" class="success-message" role="status">
      {{ successMessage }}
    </p>

    <div v-if="errorMessage" class="error-state" role="alert">
      <p>{{ errorMessage }}</p>
      <button v-if="!isLoading" class="retry-button" type="button" @click="loadSubscriptions">
        {{ t('externalCourses.retry') }}
      </button>
    </div>

    <p v-if="isLoading" class="state-card" role="status">
      {{ t('externalCourses.loading') }}
    </p>

    <div v-else-if="subscriptions.length === 0" class="state-card empty-state">
      <h2>{{ t('externalCourses.empty.title') }}</h2>
      <p>{{ t('externalCourses.empty.description') }}</p>
    </div>

    <ul v-else class="course-list">
      <li v-for="subscription in subscriptions" :key="subscription.id" class="course-card">
        <header class="course-card-header">
          <div>
            <p class="provider-key">{{ subscription.providerKey }}</p>
            <h2>{{ subscription.courseName }}</h2>
            <p>
              {{ t('externalCourses.lastScan') }}:
              {{ subscription.lastScanStatus }}
            </p>
          </div>

          <div class="course-actions">
            <RouterLink
              class="course-module-link"
              :to="{
                name: 'module-tasks',
                params: { moduleId: subscription.moduleId },
              }"
            >
              {{ t('externalCourses.actions.openModule') }}
            </RouterLink>
            <button
              class="scan-course-button"
              type="button"
              :disabled="scanningSubscriptionId === subscription.id"
              @click="scanCourse(subscription.id)"
            >
              {{ scanningSubscriptionId === subscription.id
                ? t('externalCourses.actions.scanning')
                : t('externalCourses.actions.scan') }}
            </button>
          </div>
        </header>

        <dl v-if="summariesBySubscription[subscription.id]" class="scan-summary">
          <div>
            <dt>{{ t('externalCourses.summary.new') }}</dt>
            <dd>{{ summariesBySubscription[subscription.id]?.newContentCount }}</dd>
          </div>
          <div>
            <dt>{{ t('externalCourses.summary.changed') }}</dt>
            <dd>{{ summariesBySubscription[subscription.id]?.changedContentCount }}</dd>
          </div>
          <div>
            <dt>{{ t('externalCourses.summary.review') }}</dt>
            <dd>{{ summariesBySubscription[subscription.id]?.reviewRequiredCount }}</dd>
          </div>
          <div>
            <dt>{{ t('externalCourses.summary.notVisible') }}</dt>
            <dd>{{ summariesBySubscription[subscription.id]?.notVisibleCount }}</dd>
          </div>
          <div>
            <dt>{{ t('externalCourses.summary.taskEligible') }}</dt>
            <dd>{{ summariesBySubscription[subscription.id]?.newTaskEligibleCount }}</dd>
          </div>
        </dl>

        <h3 class="contents-heading">{{ t('externalCourses.contents.title') }}</h3>
        <p v-if="contentsFor(subscription.id).length === 0" class="muted">
          {{ t('externalCourses.contents.empty') }}
        </p>
        <ul v-else class="content-list">
          <li v-for="content in contentsFor(subscription.id)" :key="content.id" class="content-card">
            <div>
              <h4>{{ content.title }}</h4>
              <a
                class="external-content-link"
                :href="content.sourceUrl"
                target="_blank"
                rel="noopener noreferrer"
              >
                {{ t('externalCourses.contents.openSource') }}
              </a>
              <p>{{ t('externalCourses.contents.due') }} {{ formatDate(content.dueDateUtc) }}</p>
              <p v-if="content.reviewReason" class="review-reason">
                {{ t(`externalCourses.reviewReasons.${content.reviewReason}`) }}
              </p>
            </div>
            <span class="content-status" :class="statusClass(content.status)">
              {{ t(`externalCourses.statuses.${content.status}`) }}
            </span>
          </li>
        </ul>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.moodle-courses-page {
  display: grid;
  gap: 1.25rem;
  max-width: 72rem;
  margin: 0 auto;
  padding: 2rem;
}

.page-header h1,
.course-card h2,
.content-card h4 {
  margin: 0;
  color: #172b4d;
}

.eyebrow,
.provider-key {
  margin: 0 0 0.35rem;
  color: #0c66e4;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.course-list,
.content-list {
  display: grid;
  gap: 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.course-card,
.state-card,
.error-state {
  padding: 1.25rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.9rem;
  background: #ffffff;
  box-shadow: 0 0.35rem 0.9rem rgb(9 30 66 / 7%);
}

.course-card-header,
.content-card,
.course-actions,
.scan-summary {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.course-actions a,
.course-actions button,
.retry-button {
  padding: 0.55rem 0.8rem;
  border: 1px solid #0c66e4;
  border-radius: 0.55rem;
  background: #ffffff;
  color: #0c66e4;
  font: inherit;
  font-weight: 700;
  text-decoration: none;
  cursor: pointer;
}

.scan-summary {
  margin: 1rem 0;
  padding: 0.85rem;
  border-radius: 0.65rem;
  background: #f1f6fd;
}

.scan-summary div {
  display: flex;
  gap: 0.3rem;
}

.scan-summary dd {
  margin: 0;
  font-weight: 800;
}

.content-card {
  padding: 0.9rem;
  border: 1px solid #e2e8f0;
  border-radius: 0.7rem;
}

.content-status {
  padding: 0.3rem 0.55rem;
  border-radius: 999px;
  font-size: 0.82rem;
  font-weight: 800;
  white-space: nowrap;
}

.content-status-task-created {
  background: #dcfce7;
  color: #166534;
}

.content-status-review-required {
  background: #fef3c7;
  color: #92400e;
}

.content-status-not-visible {
  background: #e2e8f0;
  color: #475569;
}

.success-message {
  color: #216e4e;
}

.error-state {
  color: #ae2a19;
}

.muted {
  color: #626f86;
}

@media (max-width: 48rem) {
  .course-card-header,
  .content-card,
  .scan-summary {
    display: grid;
  }
}
</style>
