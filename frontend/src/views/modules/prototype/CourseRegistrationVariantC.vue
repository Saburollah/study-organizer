<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import type { StudyModule } from '@/features/modules/moduleModels'

const props = defineProps<{
  modules: StudyModule[]
}>()

const courseUrl = ref('https://moodle.mock/course/software-engineering')
const selectedModuleId = ref('')
const isConnected = ref(false)
const scanState = ref<'idle' | 'success' | 'error'>('idle')

const selectedModule = computed(() =>
  props.modules.find((module) => module.id === selectedModuleId.value),
)

watch(
  () => props.modules,
  (modules) => {
    if (!selectedModuleId.value && modules[0]) {
      selectedModuleId.value = modules[0].id
    }
  },
  { immediate: true },
)

function registerCourse(): void {
  if (!courseUrl.value.trim() || !selectedModule.value) {
    return
  }

  isConnected.value = true
  scanState.value = 'idle'
}

function simulateScan(state: 'success' | 'error'): void {
  scanState.value = state
}

function disconnectCourse(): void {
  isConnected.value = false
  scanState.value = 'idle'
}
</script>

<template>
  <section class="course-console">
    <aside class="configuration-panel">
      <p class="prototype-label">PROTOTYP · VARIANTE C</p>
      <h2>Kurszentrale</h2>
      <p class="introduction">
        Verbindung, Zielmodul und Scansteuerung bleiben dauerhaft an einem Ort.
      </p>

      <div class="field-group">
        <label for="variant-c-course-url">Mock-Kurslink</label>
        <input id="variant-c-course-url" v-model="courseUrl" type="url" :disabled="isConnected" />
      </div>

      <div class="field-group">
        <label for="variant-c-module">Persönliches Zielmodul</label>
        <select id="variant-c-module" v-model="selectedModuleId" :disabled="isConnected">
          <option v-for="module in modules" :key="module.id" :value="module.id">
            {{ module.name }}
          </option>
        </select>
      </div>

      <div class="access-note">
        <strong>Dein Zugriff</strong>
        <span> Persönliche Zugangsdaten werden nicht mit anderen Abonnenten geteilt. </span>
      </div>

      <button
        v-if="!isConnected"
        class="primary-button full-width"
        type="button"
        :disabled="!courseUrl.trim() || !selectedModule"
        @click="registerCourse"
      >
        Kurs registrieren
      </button>

      <button v-else class="disconnect-button full-width" type="button" @click="disconnectCourse">
        Verbindung beenden
      </button>
    </aside>

    <div class="operations-panel">
      <header class="operations-header">
        <div>
          <p>EXTERNAL COURSE</p>
          <h3>
            {{ isConnected ? 'Software Engineering · Mock-Kurs' : 'Noch kein Kurs verbunden' }}
          </h3>
        </div>

        <span class="connection-status" :class="{ active: isConnected }">
          {{ isConnected ? 'Aktiv' : 'Nicht verbunden' }}
        </span>
      </header>

      <div v-if="isConnected" class="course-overview">
        <div class="metric">
          <strong>{{ selectedModule?.name }}</strong>
          <span>Persönliches Zielmodul</span>
        </div>
        <div class="metric">
          <strong>12</strong>
          <span>Bekannte Kursinhalte</span>
        </div>
        <div class="metric">
          <strong>Manuell</strong>
          <span>Scan-Auslösung</span>
        </div>
      </div>

      <div v-if="!isConnected" class="empty-console">
        <span aria-hidden="true">↗</span>
        <h3>Links mit der Registrierung beginnen</h3>
        <p>Nach der Verbindung erscheinen hier Scanstatus, Ergebnis und Verlauf.</p>
      </div>

      <template v-else>
        <section class="scan-control">
          <div>
            <p class="section-label">NÄCHSTE AKTION</p>
            <h3>Kurs jetzt auf neue Inhalte prüfen</h3>
            <p>
              Der Kurs wird einmal gemeinsam gescannt. Du siehst nur das Ergebnis für dein
              persönliches Modul.
            </p>
          </div>

          <div class="scan-buttons">
            <button class="secondary-button" type="button" @click="simulateScan('error')">
              Fehler simulieren
            </button>
            <button class="primary-button" type="button" @click="simulateScan('success')">
              Scan starten
            </button>
          </div>
        </section>

        <section v-if="scanState === 'success'" class="result-panel success-result" role="status">
          <span class="result-icon" aria-hidden="true">✓</span>
          <div>
            <strong>Scan erfolgreich abgeschlossen</strong>
            <p>2 neue Aufgaben · 1 PDF · 1 Online-Aufgabe · 0 Duplikate</p>
          </div>
        </section>

        <section v-if="scanState === 'error'" class="result-panel error-result" role="alert">
          <span class="result-icon" aria-hidden="true">!</span>
          <div>
            <strong>Mock-Quelle nicht erreichbar</strong>
            <p>Keine Daten wurden verändert. Der letzte Kursstand bleibt gültig.</p>
          </div>
        </section>

        <section class="activity-panel">
          <div class="activity-heading">
            <h3>Scan-Verlauf</h3>
            <span>Nur kursbezogene Daten</span>
          </div>

          <ol>
            <li v-if="scanState === 'success'">
              <span class="timeline-dot success-dot" />
              <div>
                <strong>Scan erfolgreich</strong>
                <small>Gerade eben · 2 neue Inhalte</small>
              </div>
            </li>
            <li v-if="scanState === 'error'">
              <span class="timeline-dot error-dot" />
              <div>
                <strong>Scan fehlgeschlagen</strong>
                <small>Gerade eben · Quelle nicht erreichbar</small>
              </div>
            </li>
            <li>
              <span class="timeline-dot" />
              <div>
                <strong>Course Subscription erstellt</strong>
                <small>Heute · {{ selectedModule?.name }}</small>
              </div>
            </li>
          </ol>
        </section>
      </template>
    </div>
  </section>
</template>

<style scoped>
.course-console {
  display: grid;
  grid-template-columns: minmax(16rem, 0.75fr) minmax(0, 2fr);
  margin-bottom: 2rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-radius: 1.25rem;
  background: #ffffff;
  box-shadow: 0 1rem 2.5rem rgb(9 30 66 / 14%);
}

.configuration-panel {
  padding: 1.5rem;
  border-right: 1px solid #dfe3e8;
  background: #172b4d;
  color: #ffffff;
}

.configuration-panel h2,
.configuration-panel p {
  margin: 0;
}

.prototype-label {
  margin-bottom: 0.4rem;
  color: #85b8ff;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.1em;
}

.introduction {
  margin-top: 0.5rem !important;
  color: #b6c2cf;
  line-height: 1.5;
}

.field-group {
  margin-top: 1.25rem;
}

.field-group label {
  display: block;
  margin-bottom: 0.4rem;
  color: #deebff;
  font-size: 0.85rem;
  font-weight: 700;
}

.field-group input,
.field-group select {
  width: 100%;
  padding: 0.7rem;
  border: 1px solid #8590a2;
  border-radius: 0.55rem;
  background: #ffffff;
  color: #172b4d;
}

.field-group input:disabled,
.field-group select:disabled {
  background: #dfe3e8;
  color: #626f86;
}

.access-note {
  display: grid;
  gap: 0.25rem;
  margin: 1.25rem 0;
  padding: 0.85rem;
  border: 1px solid rgb(133 184 255 / 45%);
  border-radius: 0.65rem;
  background: rgb(12 102 228 / 20%);
}

.access-note span {
  color: #deebff;
  font-size: 0.8rem;
  line-height: 1.4;
}

.primary-button,
.secondary-button,
.disconnect-button {
  padding: 0.7rem 0.9rem;
  border-radius: 0.55rem;
  font-weight: 750;
  cursor: pointer;
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

.disconnect-button {
  border: 1px solid #f5b7b1;
  background: transparent;
  color: #ffbdad;
}

.full-width {
  width: 100%;
}

.operations-panel {
  min-width: 0;
  padding: 1.5rem;
  background: #f7f8fa;
}

.operations-header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid #dfe3e8;
}

.operations-header p,
.operations-header h3 {
  margin: 0;
}

.operations-header p,
.section-label {
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.09em;
}

.operations-header h3 {
  margin-top: 0.3rem;
  color: #172b4d;
  font-size: 1.3rem;
}

.connection-status {
  padding: 0.35rem 0.6rem;
  border-radius: 999px;
  background: #dfe3e8;
  color: #626f86;
  font-size: 0.75rem;
  font-weight: 800;
}

.connection-status.active {
  background: #dcfff1;
  color: #216e4e;
}

.course-overview {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.75rem;
  margin: 1.25rem 0;
}

.metric {
  display: grid;
  gap: 0.25rem;
  padding: 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.7rem;
  background: #ffffff;
}

.metric strong {
  color: #172b4d;
}

.metric span {
  color: #626f86;
  font-size: 0.8rem;
}

.empty-console {
  display: grid;
  min-height: 22rem;
  place-items: center;
  align-content: center;
  color: #626f86;
  text-align: center;
}

.empty-console > span {
  display: grid;
  width: 3rem;
  height: 3rem;
  place-items: center;
  border-radius: 50%;
  background: #deebff;
  color: #0c66e4;
  font-size: 1.4rem;
}

.empty-console h3 {
  margin: 1rem 0 0.35rem;
  color: #172b4d;
}

.empty-console p {
  max-width: 28rem;
  margin: 0;
}

.scan-control {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem;
  border: 1px solid #b8d3f7;
  border-radius: 0.8rem;
  background: #eef6ff;
}

.scan-control h3,
.scan-control p {
  margin: 0;
}

.scan-control h3 {
  margin-top: 0.25rem;
  color: #172b4d;
}

.scan-control p:last-child {
  max-width: 38rem;
  margin-top: 0.4rem;
  color: #44546f;
  line-height: 1.45;
}

.scan-buttons {
  display: flex;
  flex: 0 0 auto;
  gap: 0.5rem;
}

.result-panel {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  margin-top: 1rem;
  padding: 1rem;
  border-radius: 0.75rem;
}

.result-panel p {
  margin: 0.25rem 0 0;
}

.result-icon {
  display: grid;
  width: 2.25rem;
  height: 2.25rem;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 50%;
  font-weight: 900;
}

.success-result {
  background: #dcfff1;
  color: #216e4e;
}

.success-result .result-icon {
  background: #22a06b;
  color: #ffffff;
}

.error-result {
  background: #ffebe6;
  color: #ae2e24;
}

.error-result .result-icon {
  background: #c9372c;
  color: #ffffff;
}

.activity-panel {
  margin-top: 1rem;
  padding: 1.25rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.8rem;
  background: #ffffff;
}

.activity-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.activity-heading h3 {
  margin: 0;
  color: #172b4d;
}

.activity-heading span {
  color: #626f86;
  font-size: 0.8rem;
}

.activity-panel ol {
  display: grid;
  gap: 1rem;
  margin: 1.25rem 0 0;
  padding: 0;
  list-style: none;
}

.activity-panel li {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.activity-panel li div {
  display: grid;
  gap: 0.2rem;
}

.activity-panel small {
  color: #626f86;
}

.timeline-dot {
  width: 0.75rem;
  height: 0.75rem;
  flex: 0 0 auto;
  border-radius: 50%;
  background: #0c66e4;
}

.success-dot {
  background: #22a06b;
}

.error-dot {
  background: #c9372c;
}

@media (max-width: 48rem) {
  .course-console {
    grid-template-columns: 1fr;
  }

  .configuration-panel {
    border-right: 0;
    border-bottom: 1px solid #dfe3e8;
  }

  .course-overview {
    grid-template-columns: 1fr;
  }

  .scan-control {
    align-items: stretch;
    flex-direction: column;
  }

  .scan-buttons {
    flex-wrap: wrap;
  }

  .scan-buttons button {
    flex: 1;
  }
}
</style>
