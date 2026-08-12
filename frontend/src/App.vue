<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">
        Study Organizer
      </RouterLink>

      <nav aria-label="Hauptnavigation">
        <RouterLink to="/">Startseite</RouterLink>
        <RouterLink to="/register">Registrieren</RouterLink>
      </nav>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
}

.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 2rem;
  border-bottom: 1px solid #dfe3e8;
}

.brand {
  color: #172b4d;
  font-size: 1.25rem;
  font-weight: 700;
  text-decoration: none;
}

nav {
  display: flex;
  align-items: center;
  gap: 1rem;
}

nav a {
  color: #44546f;
  text-decoration: none;
}

nav a.router-link-active {
  color: #0c66e4;
}
</style>