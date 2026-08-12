<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import ModuleForm from '@/features/modules/ModuleForm.vue'
import type {
  SaveModuleRequest,
  StudyModule,
} from '@/features/modules/moduleModels'
import { moduleService } from '@/features/modules/moduleService'
import { ApiError } from '@/services/api/apiClient'

const modules = ref<StudyModule[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const isCreateFormOpen = ref(false)
const editingModule = ref<StudyModule | null>(null)
const isSaving = ref(false)
const deletingModuleId = ref<string | null>(null)
const saveErrorMessage = ref('')
const successMessage = ref('')

const isFormOpen = computed(() =>
  isCreateFormOpen.value || editingModule.value !== null,
)

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

onMounted(loadModules)

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

async function createModule(
  request: SaveModuleRequest,
): Promise<void> {
  isSaving.value = true
  saveErrorMessage.value = ''
  successMessage.value = ''

  try {
    const createdModule =
      await moduleService.create(request)

    modules.value = [createdModule, ...modules.value]
    closeModuleForm()
    successMessage.value =
      'Das Lernmodul wurde erfolgreich erstellt.'
  } catch (error: unknown) {
    saveErrorMessage.value = getSaveErrorMessage(error)
  } finally {
    isSaving.value = false
  }
}

async function updateModule(
  request: SaveModuleRequest,
): Promise<void> {
  const moduleToUpdate = editingModule.value

  if (!moduleToUpdate) {
    return
  }

  isSaving.value = true
  saveErrorMessage.value = ''
  successMessage.value = ''

  try {
    const updatedModule = await moduleService.update(
      moduleToUpdate.id,
      request,
    )

    modules.value = modules.value.map((module) =>
      module.id === updatedModule.id
        ? updatedModule
        : module,
    )

    closeModuleForm()
    successMessage.value =
      'Das Lernmodul wurde erfolgreich aktualisiert.'
  } catch (error: unknown) {
    saveErrorMessage.value = getSaveErrorMessage(error)
  } finally {
    isSaving.value = false
  }
}

async function saveModule(
  request: SaveModuleRequest,
): Promise<void> {
  if (editingModule.value) {
    await updateModule(request)
    return
  }

  await createModule(request)
}

async function deleteModule(module: StudyModule): Promise<void> {
  const wasConfirmed = window.confirm(
    `Möchtest du „${module.name}“ wirklich löschen?`,
  )

  if (!wasConfirmed) {
    return
  }

  deletingModuleId.value = module.id
  saveErrorMessage.value = ''
  successMessage.value = ''

  try {
    await moduleService.delete(module.id)

    modules.value = modules.value.filter(
      (item) => item.id !== module.id,
    )

    if (editingModule.value?.id === module.id) {
      closeModuleForm()
    }

    successMessage.value =
      'Das Lernmodul wurde erfolgreich gelöscht.'
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

  return 'Die Lernmodule konnten nicht geladen werden.'
}

function getSaveErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return 'Das Lernmodul konnte nicht gespeichert werden.'
}

function getDeleteErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return 'Das Lernmodul konnte nicht gelöscht werden.'
}
</script>

<template>
  <section class="modules-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">DEIN STUDIUM</p>
        <h1>Lernmodule</h1>
        <p class="introduction">
          Verwalte hier deine persönlichen Fächer und
          Vorlesungen.
        </p>
      </div>

      <button
        v-if="!isFormOpen"
        class="add-module-button"
        type="button"
        @click="openCreateForm"
      >
        Neues Lernmodul
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
      v-if="saveErrorMessage"
      class="feedback-message save-error-message"
      role="alert"
    >
      {{ saveErrorMessage }}
    </p>

    <ModuleForm
      v-if="isFormOpen"
      :initial-values="formInitialValues"
      :is-submitting="isSaving"
      :title="
        editingModule
          ? 'Lernmodul bearbeiten'
          : 'Neues Lernmodul'
      "
      :submit-label="
        editingModule
          ? 'Änderungen speichern'
          : 'Lernmodul speichern'
      "
      @save="saveModule"
      @cancel="closeModuleForm"
    />

    <p
      v-if="isLoading"
      class="state-card"
      role="status"
    >
      Lernmodule werden geladen …
    </p>

    <div
      v-else-if="errorMessage"
      class="state-card error-state"
      role="alert"
    >
      <p>{{ errorMessage }}</p>
      <button
        class="retry-button"
        type="button"
        @click="loadModules"
      >
        Erneut versuchen
      </button>
    </div>

    <div
      v-else-if="modules.length === 0"
      class="state-card empty-state"
    >
      <h2>Noch keine Lernmodule</h2>
      <p>
        Erstelle dein erstes Lernmodul, um dein Studium
        zu organisieren.
      </p>
    </div>

    <ul v-else class="module-grid">
      <li
        v-for="module in modules"
        :key="module.id"
        class="module-card"
      >
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
          <p
            v-if="module.description"
            class="module-description"
          >
            {{ module.description }}
          </p>
          <p v-else class="module-description muted">
            Keine Beschreibung vorhanden.
          </p>

          <div class="module-actions">
            <RouterLink
              class="module-tasks-link"
              :to="{
                name: 'module-tasks',
                params: { moduleId: module.id },
              }"
            >
              Aufgaben
            </RouterLink>

            <button
              class="edit-module-button"
              type="button"
              :aria-label="`${module.name} bearbeiten`"
              :disabled="deletingModuleId === module.id"
              @click="openEditForm(module)"
            >
              Bearbeiten
            </button>

            <button
              class="delete-module-button"
              type="button"
              :aria-label="`${module.name} löschen`"
              :disabled="deletingModuleId === module.id"
              @click="deleteModule(module)"
            >
              {{
                deletingModuleId === module.id
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
  grid-template-columns: repeat(
    auto-fit,
    minmax(min(100%, 18rem), 1fr)
  );
  gap: 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.module-card {
  display: flex;
  min-height: 12rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
  box-shadow: 0 0.25rem 1rem rgb(9 30 66 / 6%);
}

.color-marker {
  width: 0.75rem;
  flex: 0 0 auto;
}

.module-card-content {
  display: flex;
  flex: 1;
  flex-direction: column;
  padding: 1.5rem;
}

.module-code {
  margin: 0 0 0.5rem;
  color: #0c66e4;
  font-size: 0.8rem;
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
  padding: 0.5rem 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.4rem;
  background: #ffffff;
  cursor: pointer;
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
  background: #ffebe6;
}

.edit-module-button:disabled,
.delete-module-button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

@media (max-width: 40rem) {
  .page-header {
    align-items: stretch;
    flex-direction: column;
  }

  .add-module-button {
    align-self: start;
  }
}
</style>
