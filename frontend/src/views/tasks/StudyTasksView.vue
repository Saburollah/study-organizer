<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { moduleService } from '@/features/modules/moduleService'
import type { StudyModule } from '@/features/modules/moduleModels'
import StudyTaskForm from '@/features/tasks/StudyTaskForm.vue'
import type {
  SaveStudyTaskRequest,
  StudyTask,
  StudyTaskStatus,
} from '@/features/tasks/taskModels'
import { taskService } from '@/features/tasks/taskService'
import { ApiError } from '@/services/api/apiClient'

const props = defineProps<{
  moduleId: string
}>()

const module = ref<StudyModule | null>(null)
const tasks = ref<StudyTask[]>([])
const isLoading = ref(true)
const loadErrorMessage = ref('')
const successMessage = ref('')
const actionErrorMessage = ref('')
const isCreateFormOpen = ref(false)
const editingTask = ref<StudyTask | null>(null)
const isSaving = ref(false)
const changingStatusTaskId = ref<string | null>(null)
const deletingTaskId = ref<string | null>(null)

const isFormOpen = computed(() =>
  isCreateFormOpen.value || editingTask.value !== null,
)

const formInitialValues = computed<SaveStudyTaskRequest>(() => {
  if (!editingTask.value) {
    return {
      title: '',
      dueDateUtc: '',
    }
  }

  return {
    title: editingTask.value.title,
    description: editingTask.value.description,
    dueDateUtc: editingTask.value.dueDateUtc,
  }
})

onMounted(loadPage)

async function loadPage(): Promise<void> {
  isLoading.value = true
  loadErrorMessage.value = ''

  try {
    const [modules, loadedTasks] = await Promise.all([
      moduleService.getAll(),
      taskService.getByModule(props.moduleId),
    ])

    module.value = modules.find(
      (item) => item.id === props.moduleId,
    ) ?? null

    tasks.value = sortTasks(loadedTasks)

    if (!module.value) {
      loadErrorMessage.value =
        'Das ausgewählte Lernmodul wurde nicht gefunden.'
    }
  } catch (error: unknown) {
    loadErrorMessage.value = getErrorMessage(
      error,
      'Die Aufgaben konnten nicht geladen werden.',
    )
  } finally {
    isLoading.value = false
  }
}

async function saveTask(
  request: SaveStudyTaskRequest,
): Promise<void> {
  if (editingTask.value) {
    await updateTask(request)
    return
  }

  await createTask(request)
}

async function createTask(
  request: SaveStudyTaskRequest,
): Promise<void> {
  beginAction()
  isSaving.value = true

  try {
    const createdTask = await taskService.create(
      props.moduleId,
      request,
    )

    tasks.value = sortTasks([
      ...tasks.value,
      createdTask,
    ])

    closeTaskForm()
    successMessage.value =
      'Die Aufgabe wurde erfolgreich erstellt.'
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(
      error,
      'Die Aufgabe konnte nicht gespeichert werden.',
    )
  } finally {
    isSaving.value = false
  }
}

async function updateTask(
  request: SaveStudyTaskRequest,
): Promise<void> {
  const taskToUpdate = editingTask.value

  if (!taskToUpdate) {
    return
  }

  beginAction()
  isSaving.value = true

  try {
    const updatedTask = await taskService.update(
      props.moduleId,
      taskToUpdate.id,
      request,
    )

    replaceTask(updatedTask)
    closeTaskForm()
    successMessage.value =
      'Die Aufgabe wurde erfolgreich aktualisiert.'
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(
      error,
      'Die Aufgabe konnte nicht gespeichert werden.',
    )
  } finally {
    isSaving.value = false
  }
}

async function toggleTaskStatus(task: StudyTask): Promise<void> {
  beginAction()
  changingStatusTaskId.value = task.id

  const nextStatus: StudyTaskStatus =
    task.status === 'Completed' ? 'Open' : 'Completed'

  try {
    const updatedTask = await taskService.updateStatus(
      props.moduleId,
      task.id,
      nextStatus,
    )

    replaceTask(updatedTask)
    successMessage.value = nextStatus === 'Completed'
      ? 'Die Aufgabe wurde als erledigt markiert.'
      : 'Die Aufgabe wurde wieder geöffnet.'
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(
      error,
      'Der Aufgabenstatus konnte nicht geändert werden.',
    )
  } finally {
    changingStatusTaskId.value = null
  }
}

async function deleteTask(task: StudyTask): Promise<void> {
  const wasConfirmed = window.confirm(
    `Möchtest du „${task.title}“ wirklich löschen?`,
  )

  if (!wasConfirmed) {
    return
  }

  beginAction()
  deletingTaskId.value = task.id

  try {
    await taskService.delete(props.moduleId, task.id)

    tasks.value = tasks.value.filter(
      (item) => item.id !== task.id,
    )

    if (editingTask.value?.id === task.id) {
      closeTaskForm()
    }

    successMessage.value =
      'Die Aufgabe wurde erfolgreich gelöscht.'
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(
      error,
      'Die Aufgabe konnte nicht gelöscht werden.',
    )
  } finally {
    deletingTaskId.value = null
  }
}

function openCreateForm(): void {
  beginAction()
  editingTask.value = null
  isCreateFormOpen.value = true
}

function openEditForm(task: StudyTask): void {
  beginAction()
  isCreateFormOpen.value = false
  editingTask.value = task
}

function closeTaskForm(): void {
  actionErrorMessage.value = ''
  isCreateFormOpen.value = false
  editingTask.value = null
}

function beginAction(): void {
  successMessage.value = ''
  actionErrorMessage.value = ''
}

function replaceTask(updatedTask: StudyTask): void {
  tasks.value = sortTasks(
    tasks.value.map((task) =>
      task.id === updatedTask.id ? updatedTask : task,
    ),
  )
}

function sortTasks(items: StudyTask[]): StudyTask[] {
  return [...items].sort(
    (first, second) =>
      Date.parse(first.dueDateUtc)
      - Date.parse(second.dueDateUtc),
  )
}

function formatDueDate(value: string): string {
  return new Intl.DateTimeFormat('de-DE', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function isOverdue(task: StudyTask): boolean {
  return task.status === 'Open'
    && Date.parse(task.dueDateUtc) < Date.now()
}

function getErrorMessage(
  error: unknown,
  fallback: string,
): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return fallback
}
</script>

<template>
  <section class="tasks-page">
    <RouterLink class="back-link" to="/modules">
      ← Zurück zu den Lernmodulen
    </RouterLink>

    <header class="page-header">
      <div>
        <p class="eyebrow">
          {{ module?.code || 'LERNMODUL' }}
        </p>
        <h1>{{ module?.name || 'Aufgaben' }}</h1>
        <p class="introduction">
          Plane deine Aufgaben, Fälligkeiten und deinen
          Bearbeitungsstand.
        </p>
      </div>

      <button
        v-if="!isFormOpen && !isLoading && !loadErrorMessage"
        class="add-task-button"
        type="button"
        @click="openCreateForm"
      >
        Neue Aufgabe
      </button>
    </header>

    <p
      v-if="successMessage"
      class="feedback-message success-message"
      role="status"
    >
      {{ successMessage }}
    </p>

    <p
      v-if="actionErrorMessage"
      class="feedback-message error-message"
      role="alert"
    >
      {{ actionErrorMessage }}
    </p>

    <StudyTaskForm
      v-if="isFormOpen"
      :initial-values="formInitialValues"
      :is-submitting="isSaving"
      :title="editingTask ? 'Aufgabe bearbeiten' : 'Neue Aufgabe'"
      :submit-label="
        editingTask
          ? 'Änderungen speichern'
          : 'Aufgabe speichern'
      "
      @save="saveTask"
      @cancel="closeTaskForm"
    />

    <p v-if="isLoading" class="state-card" role="status">
      Aufgaben werden geladen …
    </p>

    <div
      v-else-if="loadErrorMessage"
      class="state-card error-state"
      role="alert"
    >
      <p>{{ loadErrorMessage }}</p>
      <button class="retry-button" type="button" @click="loadPage">
        Erneut versuchen
      </button>
    </div>

    <div v-else-if="tasks.length === 0" class="state-card empty-state">
      <h2>Noch keine Aufgaben</h2>
      <p>
        Erstelle deine erste Aufgabe für dieses Lernmodul.
      </p>
    </div>

    <ul v-else class="task-list">
      <li
        v-for="task in tasks"
        :key="task.id"
        class="task-card"
        :class="{
          completed: task.status === 'Completed',
          overdue: isOverdue(task),
        }"
      >
        <button
          class="status-button"
          type="button"
          :aria-label="
            task.status === 'Completed'
              ? `${task.title} wieder öffnen`
              : `${task.title} als erledigt markieren`
          "
          :aria-pressed="task.status === 'Completed'"
          :disabled="changingStatusTaskId === task.id"
          @click="toggleTaskStatus(task)"
        >
          {{ task.status === 'Completed' ? '✓' : '' }}
        </button>

        <div class="task-content">
          <div class="task-heading">
            <h2>{{ task.title }}</h2>
            <span class="status-label">
              {{ task.status === 'Completed' ? 'Erledigt' : 'Offen' }}
            </span>
          </div>

          <p v-if="task.description" class="task-description">
            {{ task.description }}
          </p>
          <p v-else class="task-description muted">
            Keine Beschreibung vorhanden.
          </p>

          <p class="due-date">
            <strong>{{ isOverdue(task) ? 'Überfällig:' : 'Fällig:' }}</strong>
            {{ formatDueDate(task.dueDateUtc) }} Uhr
          </p>

          <div class="task-actions">
            <button
              class="edit-task-button"
              type="button"
              :aria-label="`${task.title} bearbeiten`"
              :disabled="deletingTaskId === task.id"
              @click="openEditForm(task)"
            >
              Bearbeiten
            </button>
            <button
              class="delete-task-button"
              type="button"
              :aria-label="`${task.title} löschen`"
              :disabled="deletingTaskId === task.id"
              @click="deleteTask(task)"
            >
              {{
                deletingTaskId === task.id
                  ? 'Wird gelöscht …'
                  : 'Löschen'
              }}
            </button>
          </div>
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.tasks-page {
  width: min(100% - 2rem, 70rem);
  margin: 0 auto;
  padding: 2rem 0 5rem;
}

.back-link {
  display: inline-block;
  margin-bottom: 1.5rem;
  color: #0c66e4;
  font-weight: 650;
  text-decoration: none;
}

.page-header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2rem;
}

.eyebrow {
  margin: 0 0 0.5rem;
  color: #0c66e4;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.12em;
}

h1 {
  margin: 0;
  color: #172b4d;
  font-size: clamp(2rem, 5vw, 3.25rem);
}

.introduction {
  max-width: 38rem;
  margin: 0.75rem 0 0;
  color: #626f86;
  font-size: 1.05rem;
  line-height: 1.6;
}

.add-task-button {
  flex: 0 0 auto;
  padding: 0.75rem 1rem;
  border: 1px solid #0c66e4;
  border-radius: 0.5rem;
  background: #0c66e4;
  color: #ffffff;
  font-weight: 650;
  cursor: pointer;
}

.feedback-message {
  margin: 0 0 1rem;
  padding: 0.85rem 1rem;
  border-radius: 0.5rem;
}

.success-message {
  border: 1px solid #7ee2b8;
  background: #dcfff1;
  color: #216e4e;
}

.state-card {
  padding: 2rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
  color: #44546f;
  text-align: center;
}

.error-message,
.error-state {
  border-color: #f5b7b1;
  background: #ffebe6;
  color: #ae2e24;
}

.state-card h2,
.state-card p {
  margin-top: 0;
}

.state-card p:last-child {
  margin-bottom: 0;
}

.retry-button {
  padding: 0.6rem 1rem;
  border: 0;
  border-radius: 0.5rem;
  background: #0c66e4;
  color: #ffffff;
  cursor: pointer;
}

.empty-state h2 {
  color: #172b4d;
}

.task-list {
  display: grid;
  gap: 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.task-card {
  display: flex;
  gap: 1rem;
  padding: 1.25rem;
  border: 1px solid #dfe3e8;
  border-left: 0.5rem solid #0c66e4;
  border-radius: 1rem;
  background: #ffffff;
  box-shadow: 0 0.25rem 1rem rgb(9 30 66 / 6%);
}

.task-card.overdue {
  border-left-color: #c9372c;
}

.task-card.completed {
  border-left-color: #22a06b;
  background: #f7f8f9;
}

.status-button {
  width: 1.75rem;
  height: 1.75rem;
  flex: 0 0 auto;
  padding: 0;
  border: 2px solid #0c66e4;
  border-radius: 50%;
  background: #ffffff;
  color: #ffffff;
  font-weight: 700;
  cursor: pointer;
}

.completed .status-button {
  border-color: #22a06b;
  background: #22a06b;
}

.task-content {
  min-width: 0;
  flex: 1;
}

.task-heading {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
}

.task-heading h2 {
  margin: 0;
  color: #172b4d;
  font-size: 1.25rem;
  overflow-wrap: anywhere;
}

.completed .task-heading h2 {
  color: #626f86;
  text-decoration: line-through;
}

.status-label {
  padding: 0.25rem 0.55rem;
  border-radius: 999px;
  background: #e9f2ff;
  color: #0055cc;
  font-size: 0.78rem;
  font-weight: 700;
}

.completed .status-label {
  background: #dcfff1;
  color: #216e4e;
}

.task-description,
.due-date {
  color: #44546f;
  line-height: 1.55;
  overflow-wrap: anywhere;
}

.task-description {
  margin: 0.75rem 0 0;
}

.due-date {
  margin: 0.75rem 0 0;
}

.overdue .due-date {
  color: #ae2e24;
}

.muted {
  color: #7a869a;
}

.task-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 1rem;
}

.edit-task-button,
.delete-task-button {
  padding: 0.5rem 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.4rem;
  background: #ffffff;
  cursor: pointer;
}

.edit-task-button {
  color: #172b4d;
}

.delete-task-button {
  color: #ae2e24;
}

.edit-task-button:hover {
  border-color: #0c66e4;
  color: #0c66e4;
}

.delete-task-button:hover {
  border-color: #ae2e24;
  background: #ffebe6;
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

@media (max-width: 40rem) {
  .page-header,
  .task-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .add-task-button {
    align-self: start;
  }

  .status-label {
    align-self: start;
  }
}
</style>
