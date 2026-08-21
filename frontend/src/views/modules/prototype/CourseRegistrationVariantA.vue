<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import type { StudyModule } from '@/features/modules/moduleModels'

const props = defineProps<{
  modules: StudyModule[]
}>()

const step = ref<1 | 2 | 3>(1)
const courseUrl = ref('https://moodle.mock/course/software-engineering')
const selectedModuleId = ref('')
const scanOutcome = ref<'idle' | 'success' | 'error'>('idle')

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

function continueToModules(): void {
  if (courseUrl.value.trim()) {
    step.value = 2
  }
}

function continueToReview(): void {
  if (selectedModule.value) {
    step.value = 3
  }
}

function simulateScan(outcome: 'success' | 'error'): void {
  scanOutcome.value = outcome
}

function resetPrototype(): void {
  step.value = 1
  scanOutcome.value = 'idle'
}
</script>

<template>
  <section class="wizard-prototype">
    <header class="prototype-heading">
      <div>
        <p class="prototype-label">PROTOTYP · VARIANTE A</p>
        <h2>Geführter Assistent</h2>
        <p>Kurs verknüpfen und ersten Scan in drei klaren Schritten durchführen.</p>
      </div>

      <button class="reset-button" type="button" @click="resetPrototype">Zurücksetzen</button>
    </header>

    <ol class="step-list" aria-label="Fortschritt">
      <li
        v-for="item in 3"
        :key="item"
        :class="{
          active: step === item,
          completed: step > item,
        }"
        :aria-current="step === item ? 'step' : undefined"
      >
        <span>{{ item }}</span>
        {{ item === 1 ? 'Kurs' : item === 2 ? 'Modul' : 'Prüfen' }}
      </li>
    </ol>

    <div v-if="step === 1" class="step-panel">
      <p class="step-kicker">Schritt 1 von 3</p>
      <h3>Mock-Kurs registrieren</h3>
      <p class="step-description">
        Trage den Link zu dem Kurs ein, dessen Inhalte regelmäßig geprüft werden sollen.
      </p>

      <label for="variant-a-course-url">Kurslink</label>
      <input
        id="variant-a-course-url"
        v-model="courseUrl"
        type="url"
        placeholder="https://moodle.mock/course/..."
      />

      <div class="privacy-note">
        <strong>Privat:</strong>
        Deine Zugangsdaten werden nicht mit anderen Abonnenten geteilt.
      </div>

      <div class="panel-actions end">
        <button
          class="primary-button"
          type="button"
          :disabled="!courseUrl.trim()"
          @click="continueToModules"
        >
          Link prüfen
        </button>
      </div>
    </div>

    <div v-else-if="step === 2" class="step-panel">
      <p class="step-kicker">Schritt 2 von 3</p>
      <h3>Persönliches Modul auswählen</h3>
      <p class="step-description">
        Neue Aufgaben aus dem Kurs werden diesem Study Module zugeordnet.
      </p>

      <div class="module-options">
        <label
          v-for="module in modules"
          :key="module.id"
          class="module-option"
          :class="{ selected: selectedModuleId === module.id }"
        >
          <input
            v-model="selectedModuleId"
            type="radio"
            name="variant-a-module"
            :value="module.id"
          />
          <span class="module-color" :style="{ backgroundColor: module.color ?? '#0c66e4' }" />
          <span>
            <strong>{{ module.name }}</strong>
            <small>{{ module.code ?? 'Ohne Modulkürzel' }}</small>
          </span>
        </label>
      </div>

      <div class="panel-actions">
        <button class="secondary-button" type="button" @click="step = 1">Zurück</button>
        <button
          class="primary-button"
          type="button"
          :disabled="!selectedModule"
          @click="continueToReview"
        >
          Weiter
        </button>
      </div>
    </div>

    <div v-else class="step-panel">
      <p class="step-kicker">Schritt 3 von 3</p>
      <h3>Verbindung prüfen</h3>

      <dl class="summary-list">
        <div>
          <dt>Mock-Kurs</dt>
          <dd>{{ courseUrl }}</dd>
        </div>
        <div>
          <dt>Persönliches Modul</dt>
          <dd>{{ selectedModule?.name }}</dd>
        </div>
        <div>
          <dt>Scan</dt>
          <dd>Jetzt einmal manuell starten</dd>
        </div>
      </dl>

      <div v-if="scanOutcome === 'success'" class="outcome success-outcome" role="status">
        <strong>Scan erfolgreich</strong>
        <p>2 neue Aufgaben wurden erkannt und deinem Modul hinzugefügt:</p>
        <ul>
          <li>Übungsblatt 05.pdf</li>
          <li>Online-Aufgabe: Wiederholungsfragen</li>
        </ul>
      </div>

      <div v-if="scanOutcome === 'error'" class="outcome error-outcome" role="alert">
        <strong>Scan fehlgeschlagen</strong>
        <p>
          Die Mock-Quelle war nicht erreichbar. Der letzte erfolgreiche Kursstand bleibt erhalten.
        </p>
      </div>

      <div class="panel-actions">
        <button class="secondary-button" type="button" @click="step = 2">Zurück</button>
        <button class="error-button" type="button" @click="simulateScan('error')">
          Fehler simulieren
        </button>
        <button class="primary-button" type="button" @click="simulateScan('success')">
          Verbinden und Scan starten
        </button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.wizard-prototype {
  margin-bottom: 2rem;
  overflow: hidden;
  border: 1px solid #dfe3e8;
  border-radius: 1.25rem;
  background: #ffffff;
  box-shadow: 0 1rem 2.5rem rgb(9 30 66 / 12%);
}

.prototype-heading {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.5rem;
  border-bottom: 1px solid #dfe3e8;
  background: linear-gradient(135deg, #eef6ff, #ffffff);
}

.prototype-heading h2,
.prototype-heading p {
  margin: 0;
}

.prototype-heading h2 {
  color: #172b4d;
}

.prototype-heading p:last-child {
  margin-top: 0.4rem;
  color: #626f86;
}

.prototype-label,
.step-kicker {
  margin: 0 0 0.35rem;
  color: #0c66e4;
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.1em;
}

.reset-button,
.primary-button,
.secondary-button,
.error-button {
  padding: 0.65rem 0.9rem;
  border-radius: 0.55rem;
  font-weight: 700;
  cursor: pointer;
}

.reset-button,
.secondary-button {
  border: 1px solid #b6c2cf;
  background: #ffffff;
  color: #172b4d;
}

.primary-button {
  border: 1px solid #0c66e4;
  background: #0c66e4;
  color: #ffffff;
}

.error-button {
  border: 1px solid #e2483d;
  background: #fff7f5;
  color: #ae2e24;
}

.primary-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.step-list {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  margin: 0;
  padding: 1rem 1.5rem;
  border-bottom: 1px solid #dfe3e8;
  background: #f7f8fa;
  list-style: none;
}

.step-list li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #7a869a;
  font-weight: 700;
}

.step-list li:not(:last-child)::after {
  height: 1px;
  flex: 1;
  margin-right: 0.75rem;
  background: #dfe3e8;
  content: '';
}

.step-list span {
  display: grid;
  width: 1.8rem;
  height: 1.8rem;
  place-items: center;
  border-radius: 50%;
  background: #dfe3e8;
}

.step-list .active {
  color: #0c66e4;
}

.step-list .active span {
  background: #0c66e4;
  color: #ffffff;
}

.step-list .completed {
  color: #216e4e;
}

.step-list .completed span {
  background: #22a06b;
  color: #ffffff;
}

.step-panel {
  padding: 1.75rem;
}

.step-panel h3 {
  margin: 0;
  color: #172b4d;
  font-size: 1.35rem;
}

.step-description {
  max-width: 42rem;
  margin: 0.5rem 0 1.25rem;
  color: #626f86;
  line-height: 1.55;
}

.step-panel > label {
  display: block;
  margin-bottom: 0.45rem;
  font-weight: 700;
}

.step-panel > input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #b6c2cf;
  border-radius: 0.55rem;
}

.privacy-note {
  margin-top: 1rem;
  padding: 0.85rem 1rem;
  border-left: 0.25rem solid #0c66e4;
  background: #eef6ff;
  color: #44546f;
}

.module-options {
  display: grid;
  gap: 0.75rem;
}

.module-option {
  display: flex;
  align-items: center;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.75rem;
  background: #ffffff;
  cursor: pointer;
}

.module-option.selected {
  border-color: #0c66e4;
  background: #eef6ff;
}

.module-option input {
  margin: 0;
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

.summary-list {
  display: grid;
  gap: 0.75rem;
  margin: 1.25rem 0;
}

.summary-list div {
  padding: 0.9rem 1rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.65rem;
  background: #f7f8fa;
}

.summary-list dt {
  margin-bottom: 0.25rem;
  color: #626f86;
  font-size: 0.8rem;
  font-weight: 700;
}

.summary-list dd {
  margin: 0;
  overflow-wrap: anywhere;
  font-weight: 700;
}

.outcome {
  margin: 1rem 0;
  padding: 1rem;
  border-radius: 0.75rem;
}

.outcome p {
  margin: 0.35rem 0;
}

.outcome ul {
  margin-bottom: 0;
}

.success-outcome {
  border: 1px solid #7ee2b8;
  background: #dcfff1;
  color: #216e4e;
}

.error-outcome {
  border: 1px solid #f5b7b1;
  background: #ffebe6;
  color: #ae2e24;
}

.panel-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 0.75rem;
  margin-top: 1.5rem;
}

.panel-actions.end {
  justify-content: flex-end;
}

@media (max-width: 40rem) {
  .prototype-heading {
    display: block;
  }

  .reset-button {
    margin-top: 1rem;
  }

  .step-list {
    gap: 0.5rem;
  }

  .step-list li::after {
    display: none;
  }

  .panel-actions > button {
    width: 100%;
  }
}
</style>
