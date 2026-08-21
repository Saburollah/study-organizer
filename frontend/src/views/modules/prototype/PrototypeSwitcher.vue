<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

type VariantKey = 'A' | 'B' | 'C'

interface PrototypeVariant {
  key: VariantKey
  label: string
}

const props = defineProps<{
  variants: PrototypeVariant[]
  current: VariantKey
}>()

const route = useRoute()
const router = useRouter()
const isDevelopment = import.meta.env.DEV

const currentIndex = computed(() =>
  props.variants.findIndex((variant) => variant.key === props.current),
)

const currentVariant = computed(
  () =>
    props.variants[currentIndex.value] ?? {
      key: 'A' as const,
      label: 'Unbekannte Variante',
    },
)

function selectVariant(key: VariantKey): void {
  void router.replace({
    query: {
      ...route.query,
      variant: key,
    },
  })
}

function cycleVariant(offset: number): void {
  if (props.variants.length === 0) {
    return
  }

  const index = currentIndex.value < 0 ? 0 : currentIndex.value
  const nextIndex = (index + offset + props.variants.length) % props.variants.length
  const nextVariant = props.variants[nextIndex]

  if (nextVariant) {
    selectVariant(nextVariant.key)
  }
}

function handleKeydown(event: KeyboardEvent): void {
  const target = event.target

  if (
    target instanceof HTMLElement &&
    (target.matches('input, textarea, select') || target.isContentEditable)
  ) {
    return
  }

  if (event.key === 'ArrowLeft') {
    cycleVariant(-1)
  }

  if (event.key === 'ArrowRight') {
    cycleVariant(1)
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <nav v-if="isDevelopment" class="prototype-switcher" aria-label="Prototyp-Variante auswählen">
    <button type="button" aria-label="Vorherige Variante" @click="cycleVariant(-1)">←</button>

    <strong> {{ currentVariant.key }} — {{ currentVariant.label }} </strong>

    <button type="button" aria-label="Nächste Variante" @click="cycleVariant(1)">→</button>
  </nav>
</template>

<style scoped>
.prototype-switcher {
  position: fixed;
  z-index: 2000;
  bottom: 1.25rem;
  left: 50%;
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 0.55rem 0.75rem;
  border: 1px solid rgb(255 255 255 / 28%);
  border-radius: 999px;
  background: #172b4d;
  color: #ffffff;
  box-shadow: 0 1rem 2.5rem rgb(9 30 66 / 35%);
  transform: translateX(-50%);
}

.prototype-switcher strong {
  min-width: 12rem;
  text-align: center;
}

.prototype-switcher button {
  display: grid;
  width: 2.25rem;
  height: 2.25rem;
  padding: 0;
  place-items: center;
  border: 0;
  border-radius: 50%;
  background: #ffffff;
  color: #172b4d;
  font-size: 1.2rem;
  cursor: pointer;
}

.prototype-switcher button:hover {
  background: #deebff;
}

.prototype-switcher button:focus-visible {
  outline: 0.2rem solid #85b8ff;
  outline-offset: 0.15rem;
}

@media (max-width: 32rem) {
  .prototype-switcher {
    width: calc(100% - 2rem);
    justify-content: space-between;
  }

  .prototype-switcher strong {
    min-width: 0;
    font-size: 0.85rem;
  }
}
</style>
