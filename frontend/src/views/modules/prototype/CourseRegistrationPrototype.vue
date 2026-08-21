<!--
  PROTOTYPE ONLY:
  Three variants of course registration and manual scanning,
  switchable via ?variant= on the existing /modules route.
-->
<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

import type { StudyModule } from '@/features/modules/moduleModels'
import CourseRegistrationVariantA from './CourseRegistrationVariantA.vue'
import CourseRegistrationVariantB from './CourseRegistrationVariantB.vue'
import CourseRegistrationVariantC from './CourseRegistrationVariantC.vue'
import PrototypeSwitcher from './PrototypeSwitcher.vue'

type VariantKey = 'A' | 'B' | 'C'

const props = defineProps<{
  modules: StudyModule[]
}>()

const route = useRoute()
const isDevelopment = import.meta.env.DEV

const variants: { key: VariantKey; label: string }[] = [
  { key: 'A', label: 'Geführter Assistent' },
  { key: 'B', label: 'Modul zuerst' },
  { key: 'C', label: 'Kurszentrale' },
]

const demoModules: StudyModule[] = [
  {
    id: 'prototype-module-se',
    name: 'Software Engineering',
    code: 'SE',
    description: 'Architektur, Entwurfsmuster und Softwarequalität',
    color: '#0c66e4',
    createdAtUtc: '2026-08-01T09:00:00Z',
  },
  {
    id: 'prototype-module-db',
    name: 'Datenbanken',
    code: 'DB',
    description: 'Relationale Modelle, SQL und Transaktionen',
    color: '#22a06b',
    createdAtUtc: '2026-08-02T09:00:00Z',
  },
  {
    id: 'prototype-module-math',
    name: 'Mathematik 2',
    code: 'MAT2',
    description: 'Analysis und lineare Algebra',
    color: '#e56910',
    createdAtUtc: '2026-08-03T09:00:00Z',
  },
]

const currentVariant = computed<VariantKey>(() => {
  const queryValue = Array.isArray(route.query.variant)
    ? route.query.variant[0]
    : route.query.variant

  if (queryValue === 'B' || queryValue === 'C') {
    return queryValue
  }

  return 'A'
})

const prototypeModules = computed(() => (props.modules.length > 0 ? props.modules : demoModules))
</script>

<template>
  <div v-if="isDevelopment" class="course-registration-prototype">
    <p class="prototype-warning">
      <strong>Wegwerf-Prototyp:</strong>
      Die Schaltflächen verändern nur lokalen Mock-Zustand und rufen keine API auf.
    </p>

    <CourseRegistrationVariantA v-if="currentVariant === 'A'" :modules="prototypeModules" />
    <CourseRegistrationVariantB v-else-if="currentVariant === 'B'" :modules="prototypeModules" />
    <CourseRegistrationVariantC v-else :modules="prototypeModules" />

    <PrototypeSwitcher :variants="variants" :current="currentVariant" />
  </div>
</template>

<style scoped>
.course-registration-prototype {
  margin-bottom: 2rem;
}

.prototype-warning {
  margin: 0 0 1rem;
  padding: 0.75rem 1rem;
  border: 1px dashed #b65c02;
  border-radius: 0.65rem;
  background: #fff7d6;
  color: #7f3f00;
  line-height: 1.45;
}
</style>
