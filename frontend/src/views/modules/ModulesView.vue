<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'

import ModuleForm from '@/features/modules/ModuleForm.vue'
import type { SaveModuleRequest, StudyModule } from '@/features/modules/moduleModels'
import { moduleService } from '@/features/modules/moduleService'
import { ApiError } from '@/services/api/apiClient'
import CourseRegistrationPrototype from './prototype/CourseRegistrationPrototype.vue'

const { t } = useI18n()
const route = useRoute()

const isCourseRegistrationPrototype = computed(
  () => import.meta.env.DEV && route.query.prototype === 'course-registration',
)

const modules = ref<StudyModule[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const isCreateFormOpen = ref(false)
const editingModule = ref<StudyModule | null>(null)
const isSaving = ref(false)
const deletingModuleId = ref<string | null>(null)
const modulePendingDeletion = ref<StudyModule | null>(null)
const saveErrorMessage = ref('')
const successMessage = ref('')

const isFormOpen = computed(() => isCreateFormOpen.value || editingModule.value !== null)

const formInitialValues = computed<SaveModuleRequest>(() => {
  if (!editingModule.value) {
    return { name: '' }
  }

  return {
    name: editingModule.value.name,
    code: editingModule.value.code,
    description: editingModule.value.description,
    color: editingModule.value.color,
  }
})

onMounted(() => {
  void loadModules()
  document.addEventListener('keydown', handleDialogKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleDialogKeydown)
})

async function loadModules(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''

  try {
    modules.value = await moduleService.getAll()
  } catch (error: unknown) {
    errorMessage.value = getErrorMessage(error)
  } finally {
    isLoading.value = false
  }
}

async function createModule(request: SaveModuleRequest): Promise<void> {
  isSaving.value = true
  saveErrorMessage.value = ''
  successMessage.value = ''

  try {
    const createdModule = await moduleService.create(request)

    modules.value = [createdModule, ...modules.value]
    closeModuleForm()
    successMessage.value = t('modules.success.created')
  } catch (error: unknown) {
    saveErrorMessage.value = getSaveErrorMessage(error)
  } finally {
    isSaving.value = false
  }
}

async function updateModule(request: SaveModuleRequest): Promise<void> {
  const moduleToUpdate = editingModule.value

  if (!moduleToUpdate) {
    return
  }

  isSaving.value = true
  saveErrorMessage.value = ''
  successMessage.value = ''

  try {
    const updatedModule = await moduleService.update(moduleToUpdate.id, request)

    modules.value = modules.value.map((module) =>
      module.id === updatedModule.id ? updatedModule : module,
    )

    closeModuleForm()
    successMessage.value = t('modules.success.updated')
  } catch (error: unknown) {
    saveErrorMessage.value = getSaveErrorMessage(error)
  } finally {
    isSaving.value = false
  }
}

async function saveModule(request: SaveModuleRequest): Promise<void> {
  if (editingModule.value) {
    await updateModule(request)
    return
  }

  await createModule(request)
}

function requestModuleDeletion(module: StudyModule): void {
  saveErrorMessage.value = ''
  successMessage.value = ''
  modulePendingDeletion.value = module
}

function cancelModuleDeletion(): void {
  if (deletingModuleId.value) {
    return
  }

  modulePendingDeletion.value = null
}

function handleDialogKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && modulePendingDeletion.value && !deletingModuleId.value) {
    cancelModuleDeletion()
  }
}

async function confirmModuleDeletion(): Promise<void> {
  const module = modulePendingDeletion.value

  if (!module) {
    return
  }

  deletingModuleId.value = module.id
  saveErrorMessage.value = ''
  successMessage.value = ''

  try {
    await moduleService.delete(module.id)

    modules.value = modules.value.filter((item) => item.id !== module.id)

    if (editingModule.value?.id === module.id) {
      closeModuleForm()
    }

    successMessage.value = t('modules.success.deleted')
    modulePendingDeletion.value = null
  } catch (error: unknown) {
    saveErrorMessage.value = getDeleteErrorMessage(error)
  } finally {
    deletingModuleId.value = null
  }
}

function openCreateForm(): void {
  saveErrorMessage.value = ''
  successMessage.value = ''
  editingModule.value = null
  isCreateFormOpen.value = true
}

function openEditForm(module: StudyModule): void {
  saveErrorMessage.value = ''
  successMessage.value = ''
  isCreateFormOpen.value = false
  editingModule.value = module
}

function closeModuleForm(): void {
  saveErrorMessage.value = ''
  isCreateFormOpen.value = false
  editingModule.value = null
}

function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return t('modules.errors.load')
}

function getSaveErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return t('modules.errors.save')
}

function getDeleteErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return t('modules.errors.delete')
}
</script>

<template>
  <section class="modules-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ t('modules.eyebrow') }}</p>
        <h1>{{ t('modules.title') }}</h1>
        <p class="introduction">
          {{ t('modules.description') }}
        </p>
      </div>

      <button v-if="!isFormOpen" class="add-module-button" type="button" @click="openCreateForm">
        {{ t('modules.new') }}
      </button>
    </header>

    <CourseRegistrationPrototype v-if="isCourseRegistrationPrototype" :modules="modules" />

    <p v-if="successMessage" class="feedback-message success-message" role="status">
      {{ successMessage }}
    </p>

    <p v-if="saveErrorMessage" class="feedback-message save-error-message" role="alert">
      {{ saveErrorMessage }}
    </p>

    <ModuleForm
      v-if="isFormOpen"
      :initial-values="formInitialValues"
      :is-submitting="isSaving"
      :title="editingModule ? t('modules.form.editTitle') : t('modules.form.newTitle')"
      :submit-label="editingModule ? t('modules.form.saveChanges') : t('modules.form.create')"
      @save="saveModule"
      @cancel="closeModuleForm"
    />

    <p v-if="isLoading" class="state-card" role="status">
      {{ t('modules.loading') }}
    </p>

    <div v-else-if="errorMessage" class="state-card error-state" role="alert">
      <p>{{ errorMessage }}</p>
      <button class="retry-button" type="button" @click="loadModules">
        {{ t('modules.retry') }}
      </button>
    </div>

    <div v-else-if="modules.length === 0" class="state-card empty-state">
      <h2>{{ t('modules.empty.title') }}</h2>
      <p>{{ t('modules.empty.description') }}</p>
    </div>

    <ul v-else class="module-grid">
      <li v-for="module in modules" :key="module.id" class="module-card">
        <span
          class="color-marker"
          :style="{
            backgroundColor: module.color ?? '#0c66e4',
          }"
          aria-hidden="true"
        />

        <div class="module-card-content">
          <p v-if="module.code" class="module-code">
            {{ module.code }}
          </p>
          <h2>{{ module.name }}</h2>
          <p v-if="module.description" class="module-description">
            {{ module.description }}
          </p>
          <p v-else class="module-description muted">
            {{ t('modules.noDescription') }}
          </p>

          <div class="module-actions">
            <RouterLink
              class="module-tasks-link"
              :to="{
                name: 'module-tasks',
                params: { moduleId: module.id },
              }"
            >
              {{ t('modules.actions.tasks') }}
            </RouterLink>

            <button
              class="edit-module-button"
              type="button"
              :aria-label="t('modules.actions.editAria', { name: module.name })"
              :disabled="deletingModuleId === module.id"
              @click="openEditForm(module)"
            >
              {{ t('modules.actions.edit') }}
            </button>

            <button
              class="delete-module-button"
              type="button"
              :aria-label="t('modules.actions.deleteAria', { name: module.name })"
              :disabled="deletingModuleId === module.id"
              @click="requestModuleDeletion(module)"
            >
              {{
                deletingModuleId === module.id
                  ? t('modules.actions.deleting')
                  : t('modules.actions.delete')
              }}
            </button>
          </div>
        </div>
      </li>
    </ul>

    <div
      v-if="modulePendingDeletion"
      class="delete-dialog-backdrop"
      @click.self="cancelModuleDeletion"
    >
      <section
        class="delete-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="delete-dialog-title"
        aria-describedby="delete-dialog-description"
      >
        <button
          class="delete-dialog-close"
          type="button"
          :aria-label="t('modules.deleteDialog.close')"
          :disabled="Boolean(deletingModuleId)"
          @click="cancelModuleDeletion"
        >
          ×
        </button>

        <div class="delete-dialog-icon" aria-hidden="true">!</div>

        <p class="delete-dialog-eyebrow">
          {{ t('modules.deleteDialog.eyebrow') }}
        </p>
        <h2 id="delete-dialog-title">
          {{ t('modules.deleteDialog.title') }}
        </h2>
        <p id="delete-dialog-description">
          {{
            t('modules.deleteDialog.message', {
              name: modulePendingDeletion.name,
            })
          }}
        </p>

        <div class="delete-dialog-actions">
          <button
            class="cancel-delete-button"
            type="button"
            :disabled="Boolean(deletingModuleId)"
            @click="cancelModuleDeletion"
          >
            {{ t('modules.deleteDialog.cancel') }}
          </button>
          <button
            class="confirm-delete-button"
            type="button"
            :disabled="Boolean(deletingModuleId)"
            @click="confirmModuleDeletion"
          >
            {{
              deletingModuleId ? t('modules.actions.deleting') : t('modules.deleteDialog.confirm')
            }}
          </button>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.modules-page {
  width: min(100% - 2rem, 75rem);
  margin: 0 auto;
  padding: 3rem 0 5rem;
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

.add-module-button {
  position: relative;
  isolation: isolate;
  overflow: hidden;
  flex: 0 0 auto;
  padding: 0.75rem 1rem;
  border: 1px solid #0755c7;
  border-radius: 0.5rem;
  background:
    linear-gradient(180deg, rgb(255 255 255 / 24%) 0%, transparent 42%),
    linear-gradient(145deg, #2781f5 0%, #0c66e4 55%, #0754bd 100%);
  color: #ffffff;
  font-weight: 650;
  box-shadow:
    0 0.65rem 1.25rem rgb(12 102 228 / 24%),
    inset 0 1px rgb(255 255 255 / 65%),
    inset 0 -2px rgb(5 53 122 / 20%);
  cursor: pointer;
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    filter 150ms ease;
}

.add-module-button:hover {
  filter: saturate(1.08) brightness(1.04);
  box-shadow:
    0 0.9rem 1.55rem rgb(12 102 228 / 31%),
    inset 0 1px rgb(255 255 255 / 75%),
    inset 0 -2px rgb(5 53 122 / 22%);
  transform: translateY(-0.14rem);
}

.add-module-button:active {
  transform: translateY(0.03rem);
}

.add-module-button:focus-visible {
  outline: 0.2rem solid rgb(12 102 228 / 25%);
  outline-offset: 0.18rem;
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

.save-error-message {
  border: 1px solid #f5b7b1;
  background: #ffebe6;
  color: #ae2e24;
}

.state-card {
  padding: 2rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
  color: #44546f;
  text-align: center;
}

.state-card h2,
.state-card p {
  margin-top: 0;
}

.state-card p:last-child {
  margin-bottom: 0;
}

.error-state {
  border-color: #f5b7b1;
  color: #ae2e24;
}

.error-state button {
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

.module-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.module-card {
  position: relative;
  display: flex;
  min-height: 12rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: linear-gradient(145deg, #ffffff 0%, #ffffff 60%, #f4f7fb 100%);
  box-shadow:
    0 0.7rem 1.4rem rgb(9 30 66 / 12%),
    inset 0 1px 0 rgb(255 255 255 / 95%);
  transition: box-shadow 160ms ease;
}

.module-card::after {
  position: absolute;
  top: -4.5rem;
  right: 4%;
  width: 45%;
  height: 8rem;
  transform: rotate(-11deg);
  background: linear-gradient(110deg, transparent 10%, rgb(255 255 255 / 72%) 50%, transparent 90%);
  content: '';
  pointer-events: none;
}

.module-card:hover {
  box-shadow:
    0 1rem 1.8rem rgb(9 30 66 / 16%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
}

.color-marker {
  width: 1rem;
  flex: 0 0 auto;
}

.module-card-content {
  position: relative;
  z-index: 1;
  display: flex;
  flex: 1;
  flex-direction: column;
  padding: 1.5rem;
}

.module-code {
  margin: 0 0 0.5rem;
  color: #0c66e4;
  font-size: 1rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.module-card h2 {
  margin: 0;
  color: #172b4d;
  font-size: 1.35rem;
}

.module-description {
  margin: 0.75rem 0 0;
  color: #44546f;
  line-height: 1.55;
  overflow-wrap: anywhere;
}

.muted {
  color: #7a869a;
}

.module-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: auto;
  padding-top: 1.25rem;
}

.module-tasks-link,
.edit-module-button,
.delete-module-button {
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
    linear-gradient(145deg, #ffffff 0%, #ffffff 55%, #eef2f7 100%);
  box-shadow:
    0 0.3rem 0.65rem rgb(9 30 66 / 12%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
  cursor: pointer;
  transition:
    transform 150ms ease,
    box-shadow 150ms ease,
    border-color 150ms ease,
    color 150ms ease;
}

.module-tasks-link:hover,
.edit-module-button:hover,
.delete-module-button:hover {
  transform: translateY(-0.12rem);
  box-shadow:
    0 0.5rem 0.9rem rgb(9 30 66 / 17%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
}

.module-tasks-link:active,
.edit-module-button:active,
.delete-module-button:active {
  transform: translateY(0.04rem);
  box-shadow:
    0 0.15rem 0.35rem rgb(9 30 66 / 14%),
    inset 0 1px 0 rgb(255 255 255 / 100%);
}

.module-tasks-link {
  border-color: #0c66e4;
  color: #0c66e4;
  font-weight: 650;
  text-decoration: none;
}

.edit-module-button {
  color: #172b4d;
}

.delete-module-button {
  color: #ae2e24;
}

.edit-module-button:hover {
  border-color: #0c66e4;
  color: #0c66e4;
}

.delete-module-button:hover {
  border-color: #ae2e24;
  background: linear-gradient(145deg, #fff7f5 0%, #ffebe6 100%);
}

.edit-module-button:disabled,
.delete-module-button:disabled {
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

.delete-dialog button:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

@media (max-width: 40rem) {
  .page-header {
    align-items: stretch;
    flex-direction: column;
  }

  .add-module-button {
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
