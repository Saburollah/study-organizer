<script setup lang="ts">
import { ref } from 'vue'

import type { StudyModule } from '@/features/modules/moduleModels'

defineProps<{
  modules: StudyModule[]
}>()

const activeModuleId = ref<string | null>(null)
const connectedModuleId = ref<string | null>(null)
const courseUrl = ref('https://moodle.mock/course/software-engineering')
const scanOutcome = ref<'idle' | 'success' | 'error'>('idle')

function openConnection(moduleId: string): void {
  activeModuleId.value = moduleId
  scanOutcome.value = 'idle'
}

function cancelConnection(): void {
  activeModuleId.value = null
}

function connectCourse(moduleId: string): void {
  if (!courseUrl.value.trim()) {
    return
  }

  connectedModuleId.value = moduleId
  activeModuleId.value = null
  scanOutcome.value = 'idle'
}

function simulateScan(outcome: 'success' | 'error'): void {
  scanOutcome.value = outcome
}
</script>

<template>
  <section class="module-first-prototype">
    <header class="prototype-heading">
      <div>
        <p class="prototype-label">PROTOTYP · VARIANTE B</p>
        <h2>Kurs direkt am Modul verbinden</h2>
        <p>
          Der Benutzer entscheidet zuerst, in welchem persönlichen Modul neue Aufgaben erscheinen
          sollen.
        </p>
      </div>
      <span class="layout-badge">Modul zuerst</span>
    </header>

    <div class="module-board">
      <article
        v-for="module in modules"
        :key="module.id"
        class="prototype-module-card"
        :class="{ connected: connectedModuleId === module.id }"
      >
        <span
          class="module-marker"
          :style="{ backgroundColor: module.color ?? '#0c66e4' }"
          aria-hidden="true"
        />

        <div class="module-card-body">
          <div class="module-heading">
            <div>
              <p>{{ module.code ?? 'STUDY MODULE' }}</p>
              <h3>{{ module.name }}</h3>
            </div>

            <span v-if="connectedModuleId === module.id" class="status-badge connected-badge">
              Kurs verbunden
            </span>
            <span v-else class="status-badge"> Kein Kurs </span>
          </div>

          <p class="module-description">
            {{ module.description ?? 'Keine Beschreibung vorhanden.' }}
          </p>

          <div v-if="connectedModuleId === module.id" class="connected-course">
            <div>
              <small>EXTERNAL COURSE</small>
              <strong>Software Engineering · Mock-Kurs</strong>
              <span>Letzter Scan: noch nicht ausgeführt</span>
            </div>

            <div class="scan-actions">
              <button class="secondary-button" type="button" @click="simulateScan('error')">
                Fehler simulieren
              </button>
              <button class="primary-button" type="button" @click="simulateScan('success')">
                Jetzt scannen
              </button>
            </div>

            <div v-if="scanOutcome === 'success'" class="outcome success-outcome" role="status">
              <strong>Scan abgeschlossen</strong>
              <span>2 neue Aufgaben · 0 Änderungen · 0 Duplikate</span>
            </div>

            <div v-if="scanOutcome === 'error'" class="outcome error-outcome" role="alert">
              <strong>Quelle nicht erreichbar</strong>
              <span>Der letzte erfolgreiche Kursstand bleibt erhalten.</span>
            </div>
          </div>

          <form
            v-else-if="activeModuleId === module.id"
            class="inline-connection-form"
            @submit.prevent="connectCourse(module.id)"
          >
            <label :for="`variant-b-url-${module.id}`"> Mock-Kurslink </label>
            <input
              :id="`variant-b-url-${module.id}`"
              v-model="courseUrl"
              type="url"
              placeholder="https://moodle.mock/course/..."
            />
            <p>Nur du verwaltest diese Verbindung. Andere Abonnenten bleiben unsichtbar.</p>

            <div class="inline-actions">
              <button class="secondary-button" type="button" @click="cancelConnection">
                Abbrechen
              </button>
              <button class="primary-button" type="submit" :disabled="!courseUrl.trim()">
                Kurs verbinden
              </button>
            </div>
          </form>

          <button v-else class="connect-button" type="button" @click="openConnection(module.id)">
            + Mock-Kurs verbinden
          </button>
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped>
.module-first-prototype {
  margin-bottom: 2rem;
}

.prototype-heading {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding: 1.5rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
}

.prototype-heading h2,
.prototype-heading p {
  margin: 0;
}

.prototype-heading h2 {
  color: #172b4d;
}

.prototype-heading p:last-child {
  max-width: 42rem;
  margin-top: 0.45rem;
  color: #626f86;
  line-height: 1.5;
}

.prototype-label {
  margin-bottom: 0.35rem;
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.1em;
}

.layout-badge,
.status-badge {
  flex: 0 0 auto;
  padding: 0.35rem 0.6rem;
  border-radius: 999px;
  background: #f1f2f4;
  color: #44546f;
  font-size: 0.75rem;
  font-weight: 800;
}

.layout-badge {
  background: #e9f2ff;
  color: #0c66e4;
}

.module-board {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 21rem), 1fr));
  gap: 1rem;
}

.prototype-module-card {
  position: relative;
  display: flex;
  min-height: 18rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
  box-shadow: 0 0.8rem 1.8rem rgb(9 30 66 / 10%);
}

.prototype-module-card.connected {
  border-color: #7ee2b8;
}

.module-marker {
  width: 0.65rem;
  flex: 0 0 auto;
}

.module-card-body {
  display: flex;
  min-width: 0;
  flex: 1;
  flex-direction: column;
  padding: 1.25rem;
}

.module-heading {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 0.75rem;
}

.module-heading p,
.module-heading h3 {
  margin: 0;
}

.module-heading p {
  margin-bottom: 0.3rem;
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.module-heading h3 {
  color: #172b4d;
  font-size: 1.2rem;
}

.connected-badge {
  background: #dcfff1;
  color: #216e4e;
}

.module-description {
  margin: 0.75rem 0 1rem;
  color: #626f86;
  line-height: 1.5;
}

.connect-button,
.primary-button,
.secondary-button {
  padding: 0.65rem 0.85rem;
  border-radius: 0.55rem;
  font-weight: 700;
  cursor: pointer;
}

.connect-button {
  width: 100%;
  margin-top: auto;
  border: 1px dashed #0c66e4;
  background: #eef6ff;
  color: #0c66e4;
}

.primary-button {
  border: 1px solid #0c66e4;
  background: #0c66e4;
  color: #ffffff;
}

.primary-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.secondary-button {
  border: 1px solid #b6c2cf;
  background: #ffffff;
  color: #172b4d;
}

.inline-connection-form,
.connected-course {
  margin-top: auto;
  padding: 1rem;
  border-radius: 0.75rem;
  background: #f7f8fa;
}

.inline-connection-form label {
  display: block;
  margin-bottom: 0.4rem;
  font-weight: 700;
}

.inline-connection-form input {
  width: 100%;
  padding: 0.7rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.5rem;
}

.inline-connection-form p {
  margin: 0.65rem 0 0;
  color: #626f86;
  font-size: 0.85rem;
  line-height: 1.45;
}

.inline-actions,
.scan-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 0.85rem;
}

.connected-course > div:first-child {
  display: grid;
  gap: 0.25rem;
}

.connected-course small {
  color: #216e4e;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.connected-course span {
  color: #626f86;
  font-size: 0.85rem;
}

.outcome {
  display: grid;
  gap: 0.2rem;
  margin-top: 0.85rem;
  padding: 0.75rem;
  border-radius: 0.6rem;
}

.success-outcome {
  background: #dcfff1;
  color: #216e4e;
}

.error-outcome {
  background: #ffebe6;
  color: #ae2e24;
}

.outcome span {
  color: inherit;
}

@media (max-width: 40rem) {
  .prototype-heading {
    display: block;
  }

  .layout-badge {
    display: inline-block;
    margin-top: 1rem;
  }
}
</style>
