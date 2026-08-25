<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { ApiError } from '@/services/api/apiClient'

import { courseImportService } from './courseImportService'
import type { CourseScan, CourseSubscription } from './courseImportModels'

const props = defineProps<{
  moduleId: string
}>()

const emit = defineEmits<{
  scanCompleted: []
  ended: []
}>()

const { locale, t } = useI18n()
const subscription = ref<CourseSubscription | null>(null)
const isLoading = ref(true)
const isStartingScan = ref(false)
const isEnding = ref(false)
const isEndConfirmationOpen = ref(false)
const errorMessage = ref('')
const actionErrorMessage = ref('')
const dateLocale = computed(() => (locale.value === 'en' ? 'en-GB' : 'de-DE'))
let pollTimer: ReturnType<typeof setTimeout> | null = null
let isDisposed = false

onMounted(() => {
  void loadSubscription()
})

onBeforeUnmount(() => {
  isDisposed = true
  clearPollTimer()
})

async function loadSubscription(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''

  try {
    subscription.value = await courseImportService.get(props.moduleId)
    const runningScan =
      subscription.value.latestScan?.status === 'Running' ? subscription.value.latestScan : null
    if (runningScan) {
      schedulePoll(runningScan.scanRunId)
    }
  } catch (error: unknown) {
    if (error instanceof ApiError && error.status === 404) {
      subscription.value = null
    } else {
      errorMessage.value =
        error instanceof ApiError ? error.message : t('courseImports.overview.errors.load')
    }
  } finally {
    isLoading.value = false
  }
}

function schedulePoll(scanRunId: string, delayMilliseconds = 1000): void {
  clearPollTimer()
  pollTimer = setTimeout(() => {
    pollTimer = null
    void pollScan(scanRunId)
  }, delayMilliseconds)
}

async function pollScan(scanRunId: string): Promise<void> {
  try {
    const scan = await courseImportService.getScan(props.moduleId, scanRunId)
    if (isDisposed) {
      return
    }

    updateVisibleScan(scan)

    if (scan.status === 'Running') {
      schedulePoll(scanRunId)
      return
    }

    await loadSubscription()
    if (scan.status === 'Succeeded' && !isDisposed) {
      emit('scanCompleted')
    }
  } catch (error: unknown) {
    if (!isDisposed) {
      errorMessage.value =
        error instanceof ApiError ? error.message : t('courseImports.overview.errors.poll')
    }
  }
}

function updateVisibleScan(scan: CourseScan): void {
  if (!subscription.value) {
    return
  }

  subscription.value = {
    ...subscription.value,
    latestScan: scan,
    recentScans: subscription.value.recentScans.map((item) =>
      item.scanRunId === scan.scanRunId ? scan : item,
    ),
  }
}

function clearPollTimer(): void {
  if (pollTimer) {
    clearTimeout(pollTimer)
    pollTimer = null
  }
}

async function startScan(): Promise<void> {
  if (isStartingScan.value || subscription.value?.latestScan?.status === 'Running') {
    return
  }

  isStartingScan.value = true
  actionErrorMessage.value = ''
  clearPollTimer()

  try {
    const result = await courseImportService.startScan(props.moduleId)
    updateVisibleScan(result.data)

    if (result.data.status === 'Running') {
      schedulePoll(result.data.scanRunId, result.retryAfterMilliseconds)
    } else {
      await loadSubscription()
      if (result.data.status === 'Succeeded') {
        emit('scanCompleted')
      }
    }
  } catch (error: unknown) {
    actionErrorMessage.value =
      error instanceof ApiError ? error.message : t('courseImports.overview.errors.start')
  } finally {
    isStartingScan.value = false
  }
}

function isScanFailure(scan: CourseScan): boolean {
  return scan.status === 'Failed' || scan.status === 'Cancelled' || scan.status === 'Expired'
}

function scanError(scan: CourseScan): string {
  const code = scan.errorCode ?? scan.status.toLowerCase()
  const keyByCode: Record<string, string> = {
    'source-unreachable': 'sourceUnreachable',
    'access-denied': 'accessDenied',
    timeout: 'timeout',
    'invalid-source-data': 'invalidSourceData',
    'persistence-conflict': 'persistenceConflict',
    unexpected: 'unexpected',
    cancelled: 'cancelled',
    expired: 'expired',
  }

  return t(`courseImports.overview.scanErrors.${keyByCode[code] ?? 'unexpected'}`)
}

async function endSubscription(): Promise<void> {
  if (isEnding.value) {
    return
  }

  isEnding.value = true
  actionErrorMessage.value = ''

  try {
    await courseImportService.end(props.moduleId)
    clearPollTimer()
    subscription.value = null
    isEndConfirmationOpen.value = false
    emit('ended')
  } catch (error: unknown) {
    actionErrorMessage.value =
      error instanceof ApiError ? error.message : t('courseImports.overview.errors.end')
  } finally {
    isEnding.value = false
  }
}

function scanStatus(scan: CourseScan): string {
  return t(`courseImports.overview.scanStatus.${scan.status}`)
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(dateLocale.value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
</script>

<template>
  <p v-if="isLoading" class="course-panel-loading" role="status">
    {{ t('courseImports.overview.loading') }}
  </p>

  <section v-else-if="errorMessage" class="course-panel-error" role="alert">
    <p>{{ errorMessage }}</p>
    <button type="button" @click="loadSubscription">
      {{ t('courseImports.overview.retryLoad') }}
    </button>
  </section>

  <section
    v-else-if="subscription"
    class="course-subscription-panel"
    aria-labelledby="course-subscription-title"
  >
    <p v-if="actionErrorMessage" class="scan-action-error" role="alert">
      {{ actionErrorMessage }}
    </p>

    <header class="course-heading">
      <div>
        <p class="section-label">{{ t('courseImports.overview.eyebrow') }}</p>
        <h2 id="course-subscription-title">{{ subscription.course.displayName }}</h2>
        <a
          v-if="subscription.course.sourceUrl"
          :href="subscription.course.sourceUrl"
          target="_blank"
          rel="noreferrer"
        >
          {{ t('courseImports.overview.openSource') }}
        </a>
      </div>
      <span class="subscription-status" :class="subscription.status.toLowerCase()">
        {{ t(`courseImports.overview.subscriptionStatus.${subscription.status}`) }}
      </span>
    </header>

    <div class="course-metrics">
      <div>
        <strong class="known-content-count">
          {{ subscription.latestSnapshot?.knownContentCount ?? 0 }}
        </strong>
        <span>{{ t('courseImports.overview.metrics.knownContents') }}</span>
      </div>
      <div>
        <strong>{{ subscription.latestScan ? scanStatus(subscription.latestScan) : '—' }}</strong>
        <span>{{ t('courseImports.overview.metrics.latestScan') }}</span>
      </div>
      <div>
        <strong>{{ subscription.latestScan?.personalImpact.tasksCreated ?? 0 }}</strong>
        <span>{{ t('courseImports.overview.metrics.personalTasks') }}</span>
      </div>
    </div>

    <div
      v-if="subscription.status === 'Active' && subscription.latestScan?.status !== 'Running'"
      class="scan-control"
    >
      <div>
        <h3>{{ t('courseImports.overview.scanControl.title') }}</h3>
        <p>{{ t('courseImports.overview.scanControl.description') }}</p>
      </div>
      <button
        class="primary-button start-scan-button"
        type="button"
        :disabled="isStartingScan"
        @click="startScan"
      >
        {{
          isStartingScan
            ? t('courseImports.overview.scanControl.starting')
            : t('courseImports.overview.scanControl.start')
        }}
      </button>
    </div>

    <p
      v-if="subscription.latestScan?.status === 'Running'"
      class="running-scan-message"
      role="status"
    >
      {{ t('courseImports.overview.scanRunning') }}
    </p>

    <section
      v-if="subscription.latestScan && isScanFailure(subscription.latestScan)"
      class="latest-scan-result scan-failure"
      role="alert"
    >
      <h3>{{ t('courseImports.overview.scanFailed') }}</h3>
      <p>{{ scanError(subscription.latestScan) }}</p>
      <button
        v-if="subscription.latestScan.canRetry"
        class="primary-button retry-scan-button"
        type="button"
        :disabled="isStartingScan"
        @click="startScan"
      >
        {{
          isStartingScan
            ? t('courseImports.overview.scanControl.starting')
            : t('courseImports.overview.scanControl.retry')
        }}
      </button>
    </section>

    <section
      v-else-if="subscription.latestScan?.status === 'Succeeded'"
      class="latest-scan-result"
      role="status"
    >
      <h3>{{ t('courseImports.overview.latestScan') }}</h3>
      <p>
        {{
          t('courseImports.overview.scanSummary', {
            tasks: subscription.latestScan.personalImpact.tasksCreated,
            newContents: subscription.latestScan.contentCounts.new,
            updatedContents: subscription.latestScan.contentCounts.updated,
          })
        }}
      </p>
    </section>

    <section class="scan-history">
      <h3>{{ t('courseImports.overview.history') }}</h3>
      <p v-if="subscription.recentScans.length === 0">
        {{ t('courseImports.overview.noHistory') }}
      </p>
      <ol v-else>
        <li
          v-for="scan in subscription.recentScans"
          :key="scan.scanRunId"
          class="scan-history-item"
        >
          <span class="history-dot" :class="scan.status.toLowerCase()" aria-hidden="true" />
          <div>
            <strong>{{ scanStatus(scan) }}</strong>
            <small>{{ formatDate(scan.completedAtUtc ?? scan.startedAtUtc) }}</small>
          </div>
          <span>{{
            t('courseImports.overview.historyNew', { count: scan.contentCounts.new })
          }}</span>
        </li>
      </ol>
    </section>

    <footer class="subscription-actions">
      <button class="end-subscription-button" type="button" @click="isEndConfirmationOpen = true">
        {{ t('courseImports.overview.end.action') }}
      </button>
    </footer>

    <div
      v-if="isEndConfirmationOpen"
      class="dialog-backdrop"
      @click.self="isEndConfirmationOpen = false"
    >
      <section
        class="end-subscription-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="end-subscription-title"
        aria-describedby="end-subscription-description"
      >
        <h2 id="end-subscription-title">{{ t('courseImports.overview.end.title') }}</h2>
        <p id="end-subscription-description">
          {{
            t('courseImports.overview.end.description', { course: subscription.course.displayName })
          }}
        </p>
        <p>{{ t('courseImports.overview.end.tasksRemain') }}</p>
        <div class="dialog-actions">
          <button type="button" :disabled="isEnding" @click="isEndConfirmationOpen = false">
            {{ t('courseImports.overview.end.cancel') }}
          </button>
          <button
            class="confirm-end-subscription-button"
            type="button"
            :disabled="isEnding"
            @click="endSubscription"
          >
            {{
              isEnding
                ? t('courseImports.overview.end.ending')
                : t('courseImports.overview.end.confirm')
            }}
          </button>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.course-panel-loading,
.course-panel-error,
.course-subscription-panel {
  margin-bottom: 2rem;
  padding: 1.25rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #fff;
}
.course-panel-error {
  border-color: #f5b7b1;
  background: #ffebe6;
  color: #ae2e24;
}
.course-panel-error button {
  padding: 0.55rem 0.8rem;
  border: 0;
  border-radius: 0.45rem;
  background: #0c66e4;
  color: #fff;
  cursor: pointer;
}
.course-heading {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid #dfe3e8;
}
.scan-action-error {
  padding: 0.85rem 1rem;
  border-radius: 0.6rem;
  background: #ffebe6;
  color: #ae2e24;
}
.course-heading h2,
.course-heading p {
  margin: 0;
}
.course-heading h2 {
  color: #172b4d;
}
.course-heading a {
  display: inline-block;
  margin-top: 0.45rem;
  color: #0c66e4;
}
.section-label {
  margin-bottom: 0.35rem !important;
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.09em;
}
.subscription-status {
  padding: 0.35rem 0.65rem;
  border-radius: 999px;
  background: #fff7d6;
  color: #7f5f01;
  font-size: 0.8rem;
  font-weight: 800;
}
.subscription-status.active {
  background: #dcfff1;
  color: #216e4e;
}
.course-metrics {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
  margin: 1.25rem 0;
}
.course-metrics > div {
  display: grid;
  gap: 0.25rem;
  padding: 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.7rem;
  background: #f7f8fa;
}
.course-metrics strong {
  color: #172b4d;
  font-size: 1.15rem;
}
.course-metrics span {
  color: #626f86;
  font-size: 0.82rem;
}
.latest-scan-result {
  padding: 1rem;
  border-radius: 0.75rem;
  background: #eef6ff;
  color: #172b4d;
}
.running-scan-message {
  padding: 1rem;
  border-radius: 0.75rem;
  background: #eef6ff;
  color: #0055cc;
  font-weight: 700;
}
.scan-control {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
  padding: 1rem;
  border: 1px solid #b8d3f7;
  border-radius: 0.75rem;
  background: #eef6ff;
}
.scan-control h3,
.scan-control p {
  margin: 0;
}
.scan-control p {
  margin-top: 0.3rem;
  color: #44546f;
}
.primary-button {
  flex: 0 0 auto;
  padding: 0.65rem 0.9rem;
  border: 1px solid #0c66e4;
  border-radius: 0.55rem;
  background: #0c66e4;
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}
.primary-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}
.scan-failure {
  background: #ffebe6;
  color: #ae2e24;
}
.scan-failure p {
  color: #ae2e24;
}
.scan-failure button {
  margin-top: 0.8rem;
}
.latest-scan-result h3,
.latest-scan-result p {
  margin: 0;
}
.latest-scan-result p {
  margin-top: 0.35rem;
  color: #44546f;
}
.scan-history {
  margin-top: 1.25rem;
}
.scan-history h3 {
  margin: 0;
  color: #172b4d;
}
.scan-history ol {
  display: grid;
  gap: 0.75rem;
  margin: 1rem 0 0;
  padding: 0;
  list-style: none;
}
.scan-history-item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 0;
  border-top: 1px solid #ebecf0;
}
.scan-history-item div {
  display: grid;
  gap: 0.2rem;
}
.scan-history-item small,
.scan-history-item > span:last-child {
  color: #626f86;
}
.history-dot {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
  background: #7a869a;
}
.history-dot.succeeded {
  background: #22a06b;
}
.history-dot.failed,
.history-dot.cancelled,
.history-dot.expired {
  background: #c9372c;
}
.history-dot.running {
  background: #0c66e4;
}
.subscription-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 1.25rem;
  padding-top: 1rem;
  border-top: 1px solid #dfe3e8;
}
.end-subscription-button {
  padding: 0.6rem 0.85rem;
  border: 1px solid #c9372c;
  border-radius: 0.5rem;
  background: #fff;
  color: #ae2e24;
  font-weight: 700;
  cursor: pointer;
}
.dialog-backdrop {
  position: fixed;
  z-index: 50;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(9 30 66 / 55%);
}
.end-subscription-dialog {
  width: min(100%, 30rem);
  padding: 1.5rem;
  border-radius: 1rem;
  background: #fff;
  box-shadow: 0 1.5rem 4rem rgb(9 30 66 / 30%);
}
.end-subscription-dialog h2 {
  margin-top: 0;
  color: #172b4d;
}
.dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.25rem;
}
.dialog-actions button {
  padding: 0.65rem 0.9rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
  background: #fff;
  font-weight: 700;
  cursor: pointer;
}
.dialog-actions .confirm-end-subscription-button {
  border-color: #c9372c;
  background: #c9372c;
  color: #fff;
}
@media (max-width: 40rem) {
  .course-metrics {
    grid-template-columns: 1fr;
  }
  .scan-control {
    align-items: stretch;
    flex-direction: column;
  }
  .scan-history-item {
    grid-template-columns: auto 1fr;
  }
  .scan-history-item > span:last-child {
    grid-column: 2;
  }
}
</style>
