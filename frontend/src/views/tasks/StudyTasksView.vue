<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'

import { moduleService } from '@/features/modules/moduleService'
import type { StudyModule } from '@/features/modules/moduleModels'
import StudyTaskForm from '@/features/tasks/StudyTaskForm.vue'
import type { SaveStudyTaskRequest, StudyTask, StudyTaskStatus } from '@/features/tasks/taskModels'
import { taskService } from '@/features/tasks/taskService'
import { ApiError } from '@/services/api/apiClient'

const { locale, t } = useI18n()

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
const taskPendingDeletion = ref<StudyTask | null>(null)
const dateLocale = computed(() => (locale.value === 'en' ? 'en-GB' : 'de-DE'))

const isFormOpen = computed(() => isCreateFormOpen.value || editingTask.value !== null)

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

onMounted(() => {
  void loadPage()
  document.addEventListener('keydown', handleDialogKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleDialogKeydown)
})

async function loadPage(): Promise<void> {
  isLoading.value = true
  loadErrorMessage.value = ''

  try {
    const [modules, loadedTasks] = await Promise.all([
      moduleService.getAll(),
      taskService.getByModule(props.moduleId),
    ])

    module.value = modules.find((item) => item.id === props.moduleId) ?? null

    tasks.value = sortTasks(loadedTasks)

    if (!module.value) {
      loadErrorMessage.value = t('tasks.errors.moduleNotFound')
    }
  } catch (error: unknown) {
    loadErrorMessage.value = getErrorMessage(error, t('tasks.errors.load'))
  } finally {
    isLoading.value = false
  }
}

async function saveTask(request: SaveStudyTaskRequest): Promise<void> {
  if (editingTask.value) {
    await updateTask(request)
    return
  }

  await createTask(request)
}

async function createTask(request: SaveStudyTaskRequest): Promise<void> {
  beginAction()
  isSaving.value = true

  try {
    const createdTask = await taskService.create(props.moduleId, request)

    tasks.value = sortTasks([...tasks.value, createdTask])

    closeTaskForm()
    successMessage.value = t('tasks.success.created')
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(error, t('tasks.errors.save'))
  } finally {
    isSaving.value = false
  }
}

async function updateTask(request: SaveStudyTaskRequest): Promise<void> {
  const taskToUpdate = editingTask.value

  if (!taskToUpdate) {
    return
  }

  beginAction()
  isSaving.value = true

  try {
    const updatedTask = await taskService.update(props.moduleId, taskToUpdate.id, request)

    replaceTask(updatedTask)
    closeTaskForm()
    successMessage.value = t('tasks.success.updated')
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(error, t('tasks.errors.save'))
  } finally {
    isSaving.value = false
  }
}

async function toggleTaskStatus(task: StudyTask): Promise<void> {
  beginAction()
  changingStatusTaskId.value = task.id

  const nextStatus: StudyTaskStatus = task.status === 'Completed' ? 'Open' : 'Completed'

  try {
    const updatedTask = await taskService.updateStatus(props.moduleId, task.id, nextStatus)

    replaceTask(updatedTask)
    successMessage.value =
      nextStatus === 'Completed' ? t('tasks.success.completed') : t('tasks.success.reopened')
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(error, t('tasks.errors.status'))
  } finally {
    changingStatusTaskId.value = null
  }
}

function requestTaskDeletion(task: StudyTask): void {
  beginAction()
  taskPendingDeletion.value = task
}

function cancelTaskDeletion(): void {
  if (deletingTaskId.value) {
    return
  }

  taskPendingDeletion.value = null
}

function handleDialogKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && taskPendingDeletion.value && !deletingTaskId.value) {
    cancelTaskDeletion()
  }
}

async function confirmTaskDeletion(): Promise<void> {
  const task = taskPendingDeletion.value

  if (!task) {
    return
  }

  beginAction()
  deletingTaskId.value = task.id

  try {
    await taskService.delete(props.moduleId, task.id)

    tasks.value = tasks.value.filter((item) => item.id !== task.id)

    if (editingTask.value?.id === task.id) {
      closeTaskForm()
    }

    successMessage.value = t('tasks.success.deleted')
    taskPendingDeletion.value = null
  } catch (error: unknown) {
    actionErrorMessage.value = getErrorMessage(error, t('tasks.errors.delete'))
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
    tasks.value.map((task) => (task.id === updatedTask.id ? updatedTask : task)),
  )
}

function sortTasks(items: StudyTask[]): StudyTask[] {
  return [...items].sort(
    (first, second) => Date.parse(first.dueDateUtc) - Date.parse(second.dueDateUtc),
  )
}

function formatDueDate(value: string): string {
  const formattedDate = new Intl.DateTimeFormat(dateLocale.value, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))

  return locale.value === 'de' ? `${formattedDate} Uhr` : formattedDate
}

function isOverdue(task: StudyTask): boolean {
  return task.status === 'Open' && Date.parse(task.dueDateUtc) < Date.now()
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return fallback
}
</script>

<template>
  <section class="tasks-page">
    <RouterLink class="back-link" to="/modules">
      {{ t('tasks.back') }}
    </RouterLink>

    <header class="page-header">
      <div>
        <p class="eyebrow">
          {{ module?.code || t('tasks.eyebrow') }}
        </p>
        <h1>{{ module?.name || t('tasks.title') }}</h1>
        <p class="introduction">
          {{ t('tasks.description') }}
        </p>
      </div>

      <button
        v-if="!isFormOpen && !isLoading && !loadErrorMessage"
        class="add-task-button"
        type="button"
        @click="openCreateForm"
      >
        {{ t('tasks.new') }}
      </button>
    </header>

    <p v-if="successMessage" class="feedback-message success-message" role="status">
      {{ successMessage }}
    </p>

    <p v-if="actionErrorMessage" class="feedback-message error-message" role="alert">
      {{ actionErrorMessage }}
    </p>

    <StudyTaskForm
      v-if="isFormOpen"
      :initial-values="formInitialValues"
      :is-submitting="isSaving"
      :title="editingTask ? t('tasks.form.editTitle') : t('tasks.form.newTitle')"
      :submit-label="editingTask ? t('tasks.form.saveChanges') : t('tasks.form.create')"
      @save="saveTask"
      @cancel="closeTaskForm"
    />

    <p v-if="isLoading" class="state-card" role="status">
      {{ t('tasks.loading') }}
    </p>

    <div v-else-if="loadErrorMessage" class="state-card error-state" role="alert">
      <p>{{ loadErrorMessage }}</p>
      <button class="retry-button" type="button" @click="loadPage">
        {{ t('tasks.retry') }}
      </button>
    </div>

    <div v-else-if="tasks.length === 0" class="state-card empty-state">
      <h2>{{ t('tasks.empty.title') }}</h2>
      <p>{{ t('tasks.empty.description') }}</p>
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
              ? t('tasks.actions.reopenAria', { title: task.title })
              : t('tasks.actions.completeAria', { title: task.title })
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
              {{
                task.status === 'Completed' ? t('tasks.status.completed') : t('tasks.status.open')
              }}
            </span>
          </div>

          <p v-if="task.description" class="task-description">
            {{ task.description }}
          </p>
          <p v-else class="task-description muted">
            {{ t('tasks.noDescription') }}
          </p>

          <p v-if="task.externalSource" class="external-task-source">
            {{ t('tasks.externalSource.label', { course: task.externalSource.courseName }) }}
            <a
              :href="task.externalSource.sourceUrl"
              target="_blank"
              rel="noopener noreferrer"
            >
              {{ t('tasks.externalSource.open') }}
            </a>
          </p>

          <p class="due-date">
            <strong>
              {{ isOverdue(task) ? t('tasks.due.overdue') : t('tasks.due.due') }}
            </strong>
            {{ formatDueDate(task.dueDateUtc) }}
          </p>

          <div class="task-actions">
            <button
              v-if="!task.externalSource"
              class="edit-task-button"
              type="button"
              :aria-label="t('tasks.actions.editAria', { title: task.title })"
              :disabled="deletingTaskId === task.id"
              @click="openEditForm(task)"
            >
              {{ t('tasks.actions.edit') }}
            </button>
            <button
              v-if="!task.externalSource"
              class="delete-task-button"
              type="button"
              :aria-label="t('tasks.actions.deleteAria', { title: task.title })"
              :disabled="deletingTaskId === task.id"
              @click="requestTaskDeletion(task)"
            >
              {{
                deletingTaskId === task.id ? t('tasks.actions.deleting') : t('tasks.actions.delete')
              }}
            </button>
          </div>
        </div>
      </li>
    </ul>

    <div v-if="taskPendingDeletion" class="delete-dialog-backdrop" @click.self="cancelTaskDeletion">
      <section
        class="delete-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="task-delete-dialog-title"
        aria-describedby="task-delete-dialog-description"
      >
        <button
          class="delete-dialog-close"
          type="button"
          :aria-label="t('tasks.deleteDialog.close')"
          :disabled="Boolean(deletingTaskId)"
          @click="cancelTaskDeletion"
        >
          ×
        </button>

        <div class="delete-dialog-icon" aria-hidden="true">!</div>

        <p class="delete-dialog-eyebrow">
          {{ t('tasks.deleteDialog.eyebrow') }}
        </p>
        <h2 id="task-delete-dialog-title">
          {{ t('tasks.deleteDialog.title') }}
        </h2>
        <p id="task-delete-dialog-description">
          {{
            t('tasks.deleteDialog.message', {
              title: taskPendingDeletion.title,
            })
          }}
        </p>

        <div class="delete-dialog-actions">
          <button
            class="cancel-delete-button"
            type="button"
            :disabled="Boolean(deletingTaskId)"
            @click="cancelTaskDeletion"
          >
            {{ t('tasks.deleteDialog.cancel') }}
          </button>
          <button
            class="confirm-delete-button"
            type="button"
            :disabled="Boolean(deletingTaskId)"
            @click="confirmTaskDeletion"
          >
            {{ deletingTaskId ? t('tasks.actions.deleting') : t('tasks.deleteDialog.confirm') }}
          </button>
        </div>
      </section>
    </div>
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

.external-task-source {
  color: #44546f;
}

.external-task-source a {
  color: #0c66e4;
  font-weight: 650;
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
  position: relative;
  isolation: isolate;
  display: flex;
  gap: 1rem;
  padding: 1.25rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-left: 0.5rem solid #0c66e4;
  border-radius: 1rem;
  background: linear-gradient(145deg, #ffffff 0%, #ffffff 60%, #f4f7fb 100%);
  box-shadow:
    0 0.7rem 1.4rem rgb(9 30 66 / 12%),
    0 0.18rem 0.45rem rgb(9 30 66 / 7%);
}

.task-card::after {
  position: absolute;
  z-index: -1;
  top: -4.5rem;
  right: 4%;
  width: 45%;
  height: 8rem;
  background: linear-gradient(110deg, transparent 10%, rgb(255 255 255 / 72%) 50%, transparent 90%);
  pointer-events: none;
  transform: rotate(-11deg);
  content: '';
}

.task-card.overdue {
  border-left-color: #c9372c;
}

.task-card.completed {
  border-left-color: #22a06b;
  background: linear-gradient(145deg, #fbfdfc 0%, #f7faf8 62%, #edf6f1 100%);
}

.status-button {
  width: 1.75rem;
  height: 1.75rem;
  flex: 0 0 auto;
  padding: 0;
  border: 2px solid #0c66e4;
  border-radius: 50%;
  background: linear-gradient(145deg, #ffffff 0%, #edf2f7 100%);
  color: #ffffff;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 0.35rem 0.7rem rgb(9 30 66 / 13%);
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    background-color 150ms ease;
}

.status-button:hover:not(:disabled) {
  box-shadow: 0 0.55rem 0.95rem rgb(9 30 66 / 20%);
  transform: translateY(-0.1rem);
}

.status-button:active:not(:disabled) {
  transform: translateY(0.04rem);
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
  font-size: 1.08rem;
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
  position: relative;
  overflow: hidden;
  padding: 0.5rem 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.4rem;
  background:
    linear-gradient(
      115deg,
      transparent 0%,
      transparent 43%,
      rgb(255 255 255 / 90%) 50%,
      transparent 58%,
      transparent 100%
    ),
    linear-gradient(145deg, #ffffff 0%, #eef2f7 100%);
  box-shadow: 0 0.3rem 0.65rem rgb(9 30 66 / 12%);
  cursor: pointer;
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    border-color 150ms ease,
    color 150ms ease;
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
  box-shadow: 0 0.5rem 0.9rem rgb(9 30 66 / 17%);
  transform: translateY(-0.12rem);
}

.delete-task-button:hover {
  border-color: #ae2e24;
  background: linear-gradient(145deg, #fff7f5 0%, #ffebe6 100%);
  box-shadow: 0 0.5rem 0.9rem rgb(9 30 66 / 17%);
  transform: translateY(-0.12rem);
}

.edit-task-button:active:not(:disabled),
.delete-task-button:active:not(:disabled) {
  transform: translateY(0.04rem);
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.delete-dialog-backdrop {
  position: fixed;
  z-index: 1000;
  inset: 0;
  display: grid;
  padding: 1rem;
  place-items: center;
  background: rgb(9 30 66 / 48%);
  backdrop-filter: blur(0.22rem);
}

.delete-dialog {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  width: min(100%, 38rem);
  padding: 2rem;
  border: 1px solid #d8dee8;
  border-radius: 1.25rem;
  background: linear-gradient(145deg, #ffffff 0%, #ffffff 54%, #f6f8fb 100%);
  box-shadow:
    0 1.5rem 3.5rem rgb(9 30 66 / 30%),
    0 0.35rem 0.9rem rgb(9 30 66 / 15%);
}

.delete-dialog::before {
  position: absolute;
  z-index: -1;
  top: -42%;
  left: -12%;
  width: 78%;
  height: 68%;
  border-radius: 50%;
  background: rgb(255 255 255 / 78%);
  pointer-events: none;
  transform: rotate(-8deg);
  content: '';
}

.delete-dialog-close {
  position: absolute;
  top: 1rem;
  right: 1rem;
  display: grid;
  width: 2.5rem;
  height: 2.5rem;
  padding: 0;
  place-items: center;
  border: 0;
  border-radius: 0.6rem;
  background: transparent;
  color: #626f86;
  font-size: 1.8rem;
  cursor: pointer;
  transition:
    color 150ms ease,
    background-color 150ms ease,
    transform 150ms ease;
}

.delete-dialog-close:hover:not(:disabled) {
  background: #f1f2f4;
  color: #172b4d;
  transform: translateY(-0.08rem);
}

.delete-dialog-icon {
  display: grid;
  width: 3rem;
  height: 3rem;
  margin-bottom: 1rem;
  place-items: center;
  border-radius: 50%;
  background: linear-gradient(145deg, #ffebe6 0%, #ffd7d2 100%);
  color: #ae2e24;
  font-size: 1.45rem;
  font-weight: 800;
  box-shadow: 0 0.45rem 0.9rem rgb(174 46 36 / 18%);
}

.delete-dialog-eyebrow {
  margin: 0 0 0.4rem;
  color: #ae2e24;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.12em;
}

.delete-dialog h2 {
  margin: 0;
  color: #172b4d;
  font-size: 1.75rem;
}

.delete-dialog > p:last-of-type {
  max-width: 31rem;
  margin: 0.8rem 0 0;
  color: #44546f;
  line-height: 1.6;
}

.delete-dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.75rem;
}

.cancel-delete-button,
.confirm-delete-button {
  min-width: 8.5rem;
  padding: 0.75rem 1.15rem;
  border-radius: 0.55rem;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 0.4rem 0.85rem rgb(9 30 66 / 14%);
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    border-color 150ms ease;
}

.cancel-delete-button {
  border: 1px solid #b6c2cf;
  background: linear-gradient(145deg, #ffffff 0%, #edf2f7 100%);
  color: #172b4d;
}

.confirm-delete-button {
  border: 1px solid #ae2e24;
  background: linear-gradient(145deg, #d9473f 0%, #ae2e24 100%);
  color: #ffffff;
}

.cancel-delete-button:hover:not(:disabled),
.confirm-delete-button:hover:not(:disabled) {
  box-shadow: 0 0.7rem 1.2rem rgb(9 30 66 / 20%);
  transform: translateY(-0.12rem);
}

.cancel-delete-button:hover:not(:disabled) {
  border-color: #0c66e4;
}

.confirm-delete-button:hover:not(:disabled) {
  border-color: #8e201a;
}

.cancel-delete-button:active:not(:disabled),
.confirm-delete-button:active:not(:disabled) {
  transform: translateY(0.04rem);
}

.delete-dialog button:focus-visible {
  outline: 0.18rem solid rgb(12 102 228 / 28%);
  outline-offset: 0.15rem;
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

  .delete-dialog {
    padding: 1.5rem;
  }

  .delete-dialog-actions {
    align-items: stretch;
    flex-direction: column-reverse;
  }

  .cancel-delete-button,
  .confirm-delete-button {
    width: 100%;
  }
}
</style>
