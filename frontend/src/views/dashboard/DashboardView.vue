<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { dashboardService } from '@/features/dashboard/dashboardService'

import type { DashboardData } from '@/features/dashboard/dashboardModels'

const dashboard = ref<DashboardData | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')

const openTasks = computed(() =>
  dashboard.value?.tasks.filter(
    (task) => task.status === 'Open',
  ) ?? [],
)

const completedTasks = computed(() =>
  dashboard.value?.tasks.filter(
    (task) => task.status === 'Completed',
  ) ?? [],
)

const overdueTasks = computed(() =>
  openTasks.value.filter(
    (task) => new Date(task.dueDateUtc) < new Date(),
  ),
)

const nextTasks = computed(() =>
  [...openTasks.value]
    .sort(
      (first, second) =>
        new Date(first.dueDateUtc).getTime()
        - new Date(second.dueDateUtc).getTime(),
    )
    .slice(0, 5),
)

async function loadDashboard(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''

  try {
    dashboard.value =
      await dashboardService.getDashboard()
  } catch {
    errorMessage.value =
      'Das Dashboard konnte nicht geladen werden.'
  } finally {
    isLoading.value = false
  }
}

function formatDate(dateUtc: string): string {
  return new Intl.DateTimeFormat('de-DE', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(dateUtc))
}

function isOverdue(dateUtc: string): boolean {
  return new Date(dateUtc) < new Date()
}

onMounted(loadDashboard)
</script>

<template>
  <section class="dashboard-page">
    <header class="dashboard-header">
      <div>
        <p class="eyebrow">DEIN ÜBERBLICK</p>
        <h1>Dashboard</h1>
        <p class="subtitle">
          Behalte deine Lernmodule und Aufgaben im Blick.
        </p>
      </div>

      <RouterLink class="modules-link" to="/modules">
        Lernmodule verwalten
      </RouterLink>
    </header>

    <p v-if="isLoading" class="state-message">
      Dashboard wird geladen …
    </p>

    <div
      v-else-if="errorMessage"
      class="error-state"
      role="alert"
    >
      <p>{{ errorMessage }}</p>
      <button type="button" @click="loadDashboard">
        Erneut versuchen
      </button>
    </div>

    <template v-else-if="dashboard">
      <div class="summary-grid">
        <article class="summary-card">
          <span>Lernmodule</span>
          <strong>{{ dashboard.moduleCount }}</strong>
        </article>

        <article class="summary-card open">
          <span>Offene Aufgaben</span>
          <strong>{{ openTasks.length }}</strong>
        </article>

        <article class="summary-card overdue">
          <span>Überfällig</span>
          <strong>{{ overdueTasks.length }}</strong>
        </article>

        <article class="summary-card completed">
          <span>Erledigt</span>
          <strong>{{ completedTasks.length }}</strong>
        </article>
      </div>

      <section class="next-section">
        <div class="section-heading">
          <div>
            <p class="eyebrow">ALS NÄCHSTES</p>
            <h2>Nächste Aufgaben</h2>
          </div>
        </div>

        <div
          v-if="nextTasks.length === 0"
          class="empty-state"
        >
          <h3>Keine offenen Aufgaben</h3>
          <p>
            Aktuell stehen keine offenen Aufgaben an.
          </p>
          <RouterLink to="/modules">
            Zu den Lernmodulen
          </RouterLink>
        </div>

        <div v-else class="task-list">
          <RouterLink
            v-for="task in nextTasks"
            :key="task.id"
            class="task-row"
            :class="{ overdue: isOverdue(task.dueDateUtc) }"
            :to="{
              name: 'module-tasks',
              params: { moduleId: task.moduleId },
            }"
          >
            <div class="task-information">
              <span class="module-name">
                {{ task.moduleCode || task.moduleName }}
              </span>
              <strong>{{ task.title }}</strong>
              <small>{{ task.moduleName }}</small>
            </div>

            <div class="due-date">
              <span v-if="isOverdue(task.dueDateUtc)">
                Überfällig
              </span>
              <time :datetime="task.dueDateUtc">
                {{ formatDate(task.dueDateUtc) }}
              </time>
            </div>
          </RouterLink>
        </div>
      </section>
    </template>
  </section>
</template>

<style scoped>
.dashboard-page {
  width: min(1180px, calc(100% - 2rem));
  margin: 0 auto;
  padding: 4rem 0;
  color: #172b4d;
}

.dashboard-header,
.section-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 2rem;
}

.eyebrow {
  margin: 0 0 0.75rem;
  color: #0c66e4;
  font-size: 0.8rem;
  font-weight: 800;
  letter-spacing: 0.16em;
}

h1 {
  margin: 0;
  font-size: clamp(2.5rem, 6vw, 4rem);
}

h2 {
  margin: 0;
  font-size: 2rem;
}

.subtitle {
  color: #626f86;
  font-size: 1.15rem;
}

.modules-link,
.empty-state a {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  padding: 0.8rem 1rem;
  border: 1px solid #0755c7;
  border-radius: 0.5rem;
  background:
    linear-gradient(
      180deg,
      rgb(255 255 255 / 24%) 0%,
      transparent 42%
    ),
    linear-gradient(
      145deg,
      #2781f5 0%,
      #0c66e4 55%,
      #0754bd 100%
    );
  color: white;
  font-weight: 700;
  box-shadow:
    0 0.65rem 1.25rem rgb(12 102 228 / 24%),
    inset 0 1px rgb(255 255 255 / 65%),
    inset 0 -2px rgb(5 53 122 / 20%);
  text-decoration: none;
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    filter 150ms ease;
}

.modules-link:hover,
.empty-state a:hover {
  filter: saturate(1.08) brightness(1.04);
  box-shadow:
    0 0.9rem 1.55rem rgb(12 102 228 / 31%),
    inset 0 1px rgb(255 255 255 / 75%),
    inset 0 -2px rgb(5 53 122 / 22%);
  transform: translateY(-0.14rem);
}

.modules-link:active,
.empty-state a:active {
  transform: translateY(0.03rem);
}

.modules-link:focus-visible,
.empty-state a:focus-visible {
  outline: 0.2rem solid rgb(12 102 228 / 25%);
  outline-offset: 0.18rem;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  margin: 3rem 0;
}

.summary-card {
  position: relative;
  overflow: hidden;
  padding: 1.5rem;
  border: 1px solid #dfe1e6;
  border-top: 0.65rem solid #0c66e4;
  border-radius: 0.75rem;
  background: linear-gradient(
    145deg,
    #ffffff 0%,
    #ffffff 55%,
    #f4f7fb 100%
  );
  box-shadow:
    0 0.7rem 1.4rem rgb(9 30 66 / 12%),
    inset 0 1px 0 rgb(255 255 255 / 95%);
  transition:
    transform 160ms ease,
    box-shadow 160ms ease;
}

.summary-card::after {
  position: absolute;
  top: -3.5rem;
  right: -2rem;
  width: 70%;
  height: 7rem;
  transform: rotate(-12deg);
  background: linear-gradient(
    110deg,
    transparent 10%,
    rgb(255 255 255 / 75%) 50%,
    transparent 90%
  );
  content: '';
  pointer-events: none;
}

.summary-card:hover {
  transform: translateY(-0.2rem);
  box-shadow:
    0 1rem 1.8rem rgb(9 30 66 / 16%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
}

.summary-card span {
  position: relative;
  z-index: 1;
  display: block;
  color: #626f86;
}

.summary-card strong {
  position: relative;
  z-index: 1;
  display: block;
  margin-top: 0.5rem;
  font-size: 2.5rem;
}

.summary-card.open {
  border-top-color: #ffab00;
}

.summary-card.overdue {
  border-top-color: #c9372c;
}

.summary-card.completed {
  border-top-color: #22a06b;
}

.next-section {
  margin-top: 3rem;
}

.task-list {
  display: grid;
  gap: 0.75rem;
  margin-top: 1.5rem;
}

.task-row {
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  padding: 1.25rem;
  border: 1px solid #dfe1e6;
  border-left: 0.55rem solid #0c66e4;
  border-radius: 0.75rem;
  background: linear-gradient(
    145deg,
    #ffffff 0%,
    #ffffff 62%,
    #f4f7fb 100%
  );
  color: #172b4d;
  box-shadow:
    0 0.55rem 1.2rem rgb(9 30 66 / 10%),
    inset 0 1px 0 rgb(255 255 255 / 95%);
  text-decoration: none;
  transition:
    transform 160ms ease,
    box-shadow 160ms ease,
    border-color 160ms ease;
}

.task-row::after {
  position: absolute;
  top: -4.5rem;
  right: 5%;
  width: 38%;
  height: 8rem;
  transform: rotate(-10deg);
  background: linear-gradient(
    110deg,
    transparent 10%,
    rgb(255 255 255 / 72%) 50%,
    transparent 90%
  );
  content: '';
  pointer-events: none;
}

.task-row:hover {
  border-color: #0c66e4;
  transform: translateY(-0.15rem);
  box-shadow:
    0 0.85rem 1.5rem rgb(9 30 66 / 15%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
}

.task-row.overdue {
  border-left-color: #c9372c;
}

.task-information {
  position: relative;
  z-index: 1;
  display: grid;
  gap: 0.25rem;
}

.module-name {
  color: #0c66e4;
  font-size: 1rem;
  font-weight: 800;
  letter-spacing: 0.1em;
}

.task-information strong {
  color: #2a3d5d;
  font-size: 1.35rem;
  font-weight: 500;
  letter-spacing: 0.01em;
  text-shadow: 0 1px 0 rgb(255 255 255 / 80%);
}

.task-information small {
  color: #626f86;
  font-size: 1rem;
}

.due-date {
  position: relative;
  z-index: 1;
  display: grid;
  gap: 0.25rem;
  text-align: right;
}

.due-date span {
  color: #b93f37;
  font-weight: 700;
}

.due-date time {
  color: #44546f;
}

.state-message,
.error-state,
.empty-state {
  margin-top: 3rem;
  padding: 2rem;
  border: 1px solid #dfe1e6;
  border-radius: 0.75rem;
  background: white;
}

.error-state {
  border-color: #f5a6a0;
  color: #ae2e24;
}

.error-state button {
  padding: 0.65rem 0.9rem;
  border: 0;
  border-radius: 0.4rem;
  background: #c9372c;
  color: white;
  cursor: pointer;
}

@media (max-width: 800px) {
  .dashboard-header,
  .task-row {
    align-items: stretch;
    flex-direction: column;
  }

  .summary-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .due-date {
    text-align: left;
  }
}

@media (max-width: 480px) {
  .summary-grid {
    grid-template-columns: 1fr;
  }
}
</style>
