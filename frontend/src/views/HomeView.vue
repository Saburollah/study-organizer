<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'

import { useAuthStore } from '@/features/auth/authStore'

const authStore = useAuthStore()
const { t } = useI18n()
</script>

<template>
  <div class="home-page">
    <section class="hero" aria-labelledby="hero-heading">
      <div class="hero-copy">
        <p class="eyebrow">
          {{ t('home.hero.eyebrow') }}
        </p>

        <h1 id="hero-heading">
          {{ t('home.hero.title') }}
          <span>{{ t('home.hero.titleAccent') }}</span>
        </h1>

        <p class="intro">
          {{ t('home.hero.description') }}
        </p>

        <div class="hero-actions">
          <template v-if="authStore.isAuthenticated">
            <RouterLink class="primary-action" to="/dashboard">
              {{ t('home.hero.dashboard') }}
              <span aria-hidden="true">→</span>
            </RouterLink>

            <RouterLink class="secondary-action" to="/modules">
              {{ t('home.hero.openModules') }}
            </RouterLink>
          </template>

          <template v-else>
            <RouterLink class="primary-action" to="/register">
              {{ t('home.hero.startFree') }}
              <span aria-hidden="true">→</span>
            </RouterLink>

            <RouterLink class="secondary-action" to="/login">
              {{ t('home.hero.existingAccount') }}
            </RouterLink>
          </template>
        </div>

        <ul
          class="hero-benefits"
          :aria-label="t('home.benefits.label')"
        >
          <li>
            <span aria-hidden="true">✓</span>
            {{ t('home.benefits.deadlines') }}
          </li>
          <li>
            <span aria-hidden="true">✓</span>
            {{ t('home.benefits.onePlace') }}
          </li>
          <li>
            <span aria-hidden="true">✓</span>
            {{ t('home.benefits.secure') }}
          </li>
        </ul>
      </div>

      <div
        class="organizer-preview"
        :aria-label="t('home.preview.label')"
      >
        <div class="preview-window">
          <div class="preview-toolbar" aria-hidden="true">
            <span></span>
            <span></span>
            <span></span>
            <p>{{ t('home.preview.week') }}</p>
          </div>

          <div class="preview-heading">
            <div>
              <p>{{ t('home.preview.greeting') }}</p>
              <h2>{{ t('home.preview.overview') }}</h2>
            </div>

            <div
              class="progress-ring"
              :aria-label="t('home.preview.progressLabel')"
            >
              <span>75%</span>
            </div>
          </div>

          <div class="preview-stats">
            <div>
              <span class="stat-dot blue"></span>
              <p>{{ t('home.preview.modules') }}</p>
              <strong>5</strong>
            </div>
            <div>
              <span class="stat-dot amber"></span>
              <p>{{ t('home.preview.open') }}</p>
              <strong>4</strong>
            </div>
            <div>
              <span class="stat-dot green"></span>
              <p>{{ t('home.preview.completed') }}</p>
              <strong>12</strong>
            </div>
          </div>

          <div class="next-task-heading">
            <h3>{{ t('home.preview.next') }}</h3>
            <span>{{ t('home.preview.today') }}</span>
          </div>

          <div class="preview-tasks">
            <article class="preview-task urgent">
              <div class="module-icon">MA</div>
              <div>
                <p>{{ t('home.preview.mathematics') }}</p>
                <strong>{{ t('home.preview.mathTask') }}</strong>
              </div>
              <time datetime="18:00">18:00</time>
            </article>

            <article class="preview-task">
              <div class="module-icon purple">SE</div>
              <div>
                <p>{{ t('home.preview.softwareEngineering') }}</p>
                <strong>{{ t('home.preview.softwareTask') }}</strong>
              </div>
              <span class="tomorrow">
                {{ t('home.preview.tomorrow') }}
              </span>
            </article>
          </div>
        </div>

        <div class="floating-badge">
          <span aria-hidden="true">✓</span>
          <div>
            <strong>{{ t('home.preview.organized') }}</strong>
            <p>{{ t('home.preview.completedThisWeek') }}</p>
          </div>
        </div>
      </div>
    </section>

    <section class="features" aria-labelledby="features-heading">
      <div class="section-heading">
        <p class="eyebrow">
          {{ t('home.features.eyebrow') }}
        </p>
        <h2 id="features-heading">
          {{ t('home.features.title') }}
        </h2>
        <p>
          {{ t('home.features.description') }}
        </p>
      </div>

      <div class="feature-grid">
        <article class="feature-card modules-card">
          <div class="feature-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24">
              <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5z" />
              <path d="M4 5.5v16M8 7h8M8 11h6" />
            </svg>
          </div>
          <span class="feature-number">01</span>
          <h3>{{ t('home.features.modules.title') }}</h3>
          <p>
            {{ t('home.features.modules.description') }}
          </p>
          <span class="feature-detail">
            {{ t('home.features.modules.detail') }}
          </span>
        </article>

        <article class="feature-card tasks-card">
          <div class="feature-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24">
              <rect x="4" y="4" width="16" height="16" rx="3" />
              <path d="m8 12 2.4 2.4L16.5 8.5" />
            </svg>
          </div>
          <span class="feature-number">02</span>
          <h3>{{ t('home.features.tasks.title') }}</h3>
          <p>
            {{ t('home.features.tasks.description') }}
          </p>
          <span class="feature-detail">
            {{ t('home.features.tasks.detail') }}
          </span>
        </article>

        <article class="feature-card progress-card">
          <div class="feature-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24">
              <path d="M4 19V9M10 19V5M16 19v-7M22 19V3" />
              <path d="M2 19h20" />
            </svg>
          </div>
          <span class="feature-number">03</span>
          <h3>{{ t('home.features.progress.title') }}</h3>
          <p>
            {{ t('home.features.progress.description') }}
          </p>
          <span class="feature-detail">
            {{ t('home.features.progress.detail') }}
          </span>
        </article>
      </div>
    </section>

    <section class="final-cta" aria-labelledby="cta-heading">
      <div>
        <p class="eyebrow">
          {{ t('home.callToAction.eyebrow') }}
        </p>
        <h2 id="cta-heading">
          {{ t('home.callToAction.title') }}
        </h2>
      </div>

      <RouterLink
        v-if="authStore.isAuthenticated"
        class="primary-action light-action"
        to="/dashboard"
      >
        {{ t('home.callToAction.dashboard') }}
        <span aria-hidden="true">→</span>
      </RouterLink>

      <RouterLink
        v-else
        class="primary-action light-action"
        to="/register"
      >
        {{ t('home.callToAction.createAccount') }}
        <span aria-hidden="true">→</span>
      </RouterLink>
    </section>
  </div>
</template>

<style scoped>
.home-page {
  position: relative;
  overflow: hidden;
  padding: clamp(2rem, 5vw, 4.5rem) 0 5rem;
}

.home-page::before,
.home-page::after {
  position: absolute;
  z-index: -1;
  border-radius: 50%;
  content: '';
  filter: blur(0.2rem);
  pointer-events: none;
}

.home-page::before {
  top: -16rem;
  right: -12rem;
  width: 40rem;
  height: 40rem;
  background: radial-gradient(circle, rgb(12 102 228 / 15%), transparent 68%);
}

.home-page::after {
  top: 36rem;
  left: -18rem;
  width: 36rem;
  height: 36rem;
  background: radial-gradient(circle, rgb(101 84 192 / 10%), transparent 70%);
}

.hero,
.features,
.final-cta {
  width: min(1180px, calc(100% - 2rem));
  margin-inline: auto;
}

.hero {
  display: grid;
  grid-template-columns: minmax(0, 0.92fr) minmax(30rem, 1.08fr);
  align-items: center;
  gap: clamp(2.5rem, 6vw, 6rem);
  min-height: 37rem;
}

.hero-copy {
  max-width: 39rem;
}

.eyebrow {
  margin: 0 0 1rem;
  color: #0c66e4;
  font-size: 0.8rem;
  font-weight: 800;
  letter-spacing: 0.17em;
}

h1 {
  margin: 0;
  color: #172b4d;
  font-size: clamp(3rem, 6.2vw, 5.4rem);
  line-height: 0.98;
  letter-spacing: -0.055em;
}

h1 span {
  display: block;
  margin-top: 0.12em;
  background: linear-gradient(110deg, #0c66e4, #6554c0 78%);
  background-clip: text;
  color: transparent;
}

.intro {
  max-width: 37rem;
  margin: 1.7rem 0 0;
  color: #44546f;
  font-size: clamp(1.05rem, 1.8vw, 1.25rem);
  line-height: 1.65;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.85rem;
  margin-top: 2rem;
}

.primary-action,
.secondary-action {
  position: relative;
  overflow: hidden;
  display: inline-flex;
  min-height: 3.3rem;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  padding: 0.85rem 1.25rem;
  border-radius: 0.85rem;
  font-weight: 750;
  text-decoration: none;
  transition:
    transform 160ms ease,
    box-shadow 160ms ease,
    border-color 160ms ease;
}

.primary-action {
  border: 1px solid #0755c7;
  background: linear-gradient(145deg, #2d82f7 0%, #0c66e4 58%, #0755c7 100%);
  color: #ffffff;
  box-shadow:
    0 0.8rem 1.5rem rgb(12 102 228 / 24%),
    inset 0 1px rgb(255 255 255 / 58%),
    inset 0 -2px rgb(5 55 130 / 28%);
}

.primary-action::before {
  position: absolute;
  inset: 0;
  background: linear-gradient(110deg, transparent 25%, rgb(255 255 255 / 48%) 49%, transparent 73%);
  content: '';
  pointer-events: none;
  transform: translateX(-130%);
  transition: transform 480ms ease;
}

.primary-action:hover::before {
  transform: translateX(130%);
}

.secondary-action {
  border: 1px solid #b6c2cf;
  background: linear-gradient(145deg, #ffffff, #edf2f8);
  color: #172b4d;
  box-shadow:
    0 0.5rem 1rem rgb(9 30 66 / 10%),
    inset 0 1px #ffffff,
    inset 0 -1px rgb(9 30 66 / 9%);
}

.primary-action:hover,
.secondary-action:hover {
  transform: translateY(-0.16rem);
}

.primary-action:hover {
  box-shadow:
    0 1rem 1.8rem rgb(12 102 228 / 30%),
    inset 0 1px rgb(255 255 255 / 70%);
}

.secondary-action:hover {
  border-color: #8fb8f4;
  box-shadow:
    0 0.75rem 1.3rem rgb(12 102 228 / 15%),
    inset 0 1px #ffffff;
}

.hero-benefits {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem 1.1rem;
  margin: 1.6rem 0 0;
  padding: 0;
  color: #44546f;
  font-size: 0.87rem;
  list-style: none;
}

.hero-benefits li {
  display: flex;
  align-items: center;
  gap: 0.38rem;
}

.hero-benefits span {
  display: grid;
  width: 1.35rem;
  height: 1.35rem;
  place-items: center;
  border-radius: 50%;
  background: #dcfff1;
  color: #1f845a;
  font-size: 0.72rem;
  font-weight: 900;
}

.organizer-preview {
  position: relative;
  width: calc(100% + 2.5rem);
  margin-left: -1.25rem;
  padding: 1.25rem 0 2rem;
}

.organizer-preview::before {
  position: absolute;
  inset: 6% -5% 4% 7%;
  z-index: -1;
  border-radius: 3rem;
  background: linear-gradient(145deg, rgb(12 102 228 / 15%), rgb(101 84 192 / 10%));
  content: '';
  filter: blur(0.15rem);
  transform: rotate(3deg);
}

.preview-window {
  position: relative;
  overflow: hidden;
  padding: 1.45rem;
  border: 1px solid rgb(174 189 206 / 85%);
  border-radius: 1.7rem;
  background: linear-gradient(145deg, #ffffff 0%, #fbfdff 55%, #eaf2fb 100%);
  box-shadow:
    0 1.8rem 4rem rgb(9 30 66 / 19%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(9 30 66 / 8%);
}

.preview-window::after {
  position: absolute;
  top: -6rem;
  left: -4rem;
  width: 26rem;
  height: 12rem;
  border-radius: 50%;
  background: rgb(255 255 255 / 62%);
  content: '';
  pointer-events: none;
  transform: rotate(-10deg);
}

.preview-window > * {
  position: relative;
  z-index: 1;
}

.preview-toolbar {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #dfe3e8;
}

.preview-toolbar > span {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 50%;
  background: #f87168;
}

.preview-toolbar > span:nth-child(2) {
  background: #f5cd47;
}

.preview-toolbar > span:nth-child(3) {
  background: #4bce97;
}

.preview-toolbar p {
  margin: 0 0 0 auto;
  color: #7a869a;
  font-size: 0.66rem;
  font-weight: 800;
  letter-spacing: 0.14em;
}

.preview-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.3rem 0 1rem;
}

.preview-heading p,
.preview-task p,
.floating-badge p {
  margin: 0;
  color: #626f86;
}

.preview-heading p {
  font-size: 0.82rem;
}

.preview-heading h2 {
  margin: 0.25rem 0 0;
  color: #172b4d;
  font-size: 1.7rem;
}

.progress-ring {
  position: relative;
  display: grid;
  width: 4.4rem;
  height: 4.4rem;
  place-items: center;
  border-radius: 50%;
  background: conic-gradient(#22a06b 0 75%, #dfe3e8 75% 100%);
  box-shadow: 0 0.55rem 1rem rgb(9 30 66 / 13%);
}

.progress-ring::before {
  position: absolute;
  width: 3.45rem;
  height: 3.45rem;
  border-radius: 50%;
  background: #ffffff;
  content: '';
}

.progress-ring span {
  position: relative;
  z-index: 1;
  color: #172b4d;
  font-size: 0.84rem;
  font-weight: 800;
}

.preview-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.65rem;
}

.preview-stats > div {
  position: relative;
  padding: 0.95rem;
  border: 1px solid #dfe3e8;
  border-radius: 0.85rem;
  background: rgb(255 255 255 / 76%);
  box-shadow:
    0 0.4rem 0.9rem rgb(9 30 66 / 8%),
    inset 0 1px #ffffff;
}

.preview-stats p {
  margin: 0.3rem 0 0;
  color: #626f86;
  font-size: 0.75rem;
}

.preview-stats strong {
  display: block;
  margin-top: 0.12rem;
  color: #172b4d;
  font-size: 1.38rem;
}

.stat-dot {
  display: block;
  width: 1.5rem;
  height: 0.22rem;
  border-radius: 999px;
}

.stat-dot.blue {
  background: #0c66e4;
}

.stat-dot.amber {
  background: #e56910;
}

.stat-dot.green {
  background: #22a06b;
}

.next-task-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 1.2rem 0 0.7rem;
}

.next-task-heading h3 {
  margin: 0;
  color: #172b4d;
  font-size: 0.9rem;
}

.next-task-heading span,
.tomorrow {
  color: #0c66e4;
  font-size: 0.7rem;
  font-weight: 750;
}

.preview-tasks {
  display: grid;
  gap: 0.6rem;
}

.preview-task {
  display: grid;
  grid-template-columns: auto 1fr auto;
  align-items: center;
  gap: 0.75rem;
  padding: 0.82rem;
  border: 1px solid #dfe3e8;
  border-left: 0.3rem solid #0c66e4;
  border-radius: 0.8rem;
  background: linear-gradient(145deg, #ffffff, #f3f7fc);
  box-shadow:
    0 0.35rem 0.75rem rgb(9 30 66 / 8%),
    inset 0 1px #ffffff;
}

.preview-task.urgent {
  border-left-color: #e56910;
}

.module-icon {
  display: grid;
  width: 2.55rem;
  height: 2.55rem;
  place-items: center;
  border-radius: 0.7rem;
  background: #fff3eb;
  color: #a54800;
  font-size: 0.67rem;
  font-weight: 850;
}

.module-icon.purple {
  background: #f3f0ff;
  color: #5e4db2;
}

.preview-task p {
  font-size: 0.65rem;
}

.preview-task strong {
  display: block;
  margin-top: 0.15rem;
  color: #172b4d;
  font-size: 0.84rem;
}

.preview-task time {
  color: #ae2a19;
  font-size: 0.73rem;
  font-weight: 750;
}

.floating-badge {
  position: absolute;
  right: -1.2rem;
  bottom: 0;
  display: flex;
  max-width: 16rem;
  align-items: center;
  gap: 0.7rem;
  padding: 0.85rem 1rem;
  border: 1px solid #b8e9d2;
  border-radius: 1rem;
  background: linear-gradient(145deg, #ffffff, #edfff7);
  box-shadow:
    0 1rem 2rem rgb(9 30 66 / 18%),
    inset 0 1px #ffffff;
}

.floating-badge > span {
  display: grid;
  width: 2rem;
  height: 2rem;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 50%;
  background: #22a06b;
  color: #ffffff;
  font-weight: 850;
}

.floating-badge strong {
  color: #172b4d;
  font-size: 0.78rem;
}

.floating-badge p {
  margin-top: 0.12rem;
  font-size: 0.64rem;
}

.features {
  padding-top: clamp(5rem, 9vw, 8rem);
}

.section-heading {
  max-width: 43rem;
  margin: 0 auto 2.5rem;
  text-align: center;
}

.section-heading h2,
.final-cta h2 {
  margin: 0;
  color: #172b4d;
  font-size: clamp(2rem, 4vw, 3rem);
  line-height: 1.12;
  letter-spacing: -0.035em;
}

.section-heading > p:last-child {
  margin: 1rem auto 0;
  color: #626f86;
  font-size: 1.05rem;
  line-height: 1.6;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.2rem;
}

.feature-card {
  position: relative;
  overflow: hidden;
  min-height: 20rem;
  padding: 1.7rem;
  border: 1px solid #cbd5e1;
  border-top: 0.42rem solid var(--card-accent);
  border-radius: 1.3rem;
  background:
    linear-gradient(120deg, transparent 30%, rgb(255 255 255 / 80%) 49%, transparent 68%) 160% 0 / 220% 100% no-repeat,
    linear-gradient(145deg, #ffffff 0%, #fbfdff 56%, #eaf1f8 100%);
  box-shadow:
    0 1rem 2rem rgb(9 30 66 / 12%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(9 30 66 / 8%);
  transition:
    transform 180ms ease,
    box-shadow 180ms ease,
    background-position 520ms ease;
}

.feature-card:hover {
  background-position: -70% 0, 0 0;
  box-shadow:
    0 1.5rem 2.8rem rgb(9 30 66 / 17%),
    inset 0 2px #ffffff,
    inset 0 -2px rgb(12 102 228 / 9%);
  transform: translateY(-0.3rem);
}

.modules-card {
  --card-accent: #0c66e4;
  --icon-bg: #e9f2ff;
  --icon-color: #0c66e4;
}

.tasks-card {
  --card-accent: #e56910;
  --icon-bg: #fff3eb;
  --icon-color: #a54800;
}

.progress-card {
  --card-accent: #22a06b;
  --icon-bg: #dcfff1;
  --icon-color: #1f845a;
}

.feature-icon {
  display: grid;
  width: 3.6rem;
  height: 3.6rem;
  place-items: center;
  border: 1px solid color-mix(in srgb, var(--card-accent) 48%, white);
  border-radius: 1rem;
  background: var(--icon-bg);
  color: var(--icon-color);
  box-shadow:
    0 0.65rem 1.1rem rgb(9 30 66 / 11%),
    inset 0 1px #ffffff;
}

.feature-icon svg {
  width: 1.75rem;
  height: 1.75rem;
  fill: none;
  stroke: currentColor;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-width: 1.8;
}

.feature-number {
  position: absolute;
  top: 1.8rem;
  right: 1.7rem;
  color: var(--card-accent);
  font-size: 0.76rem;
  font-weight: 850;
  letter-spacing: 0.12em;
}

.feature-card h3 {
  margin: 1.6rem 0 0.65rem;
  color: #172b4d;
  font-size: 1.45rem;
}

.feature-card p {
  min-height: 5rem;
  margin: 0;
  color: #626f86;
  line-height: 1.65;
}

.feature-detail {
  display: inline-flex;
  margin-top: 1.35rem;
  color: var(--icon-color);
  font-size: 0.82rem;
  font-weight: 750;
}

.final-cta {
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  margin-top: clamp(5rem, 9vw, 8rem);
  padding: clamp(2rem, 5vw, 3.5rem);
  border: 1px solid #0755c7;
  border-radius: 1.6rem;
  background: linear-gradient(125deg, #0747a6 0%, #0c66e4 55%, #6554c0 120%);
  box-shadow:
    0 1.6rem 3rem rgb(9 30 66 / 22%),
    inset 0 2px rgb(255 255 255 / 30%),
    inset 0 -2px rgb(5 55 130 / 35%);
}

.final-cta::before {
  position: absolute;
  top: -9rem;
  left: -4rem;
  width: 36rem;
  height: 16rem;
  border-radius: 50%;
  background: rgb(255 255 255 / 11%);
  content: '';
  pointer-events: none;
  transform: rotate(-8deg);
}

.final-cta > * {
  position: relative;
}

.final-cta .eyebrow {
  color: #b8d3f7;
}

.final-cta h2 {
  max-width: 39rem;
  color: #ffffff;
}

.light-action {
  flex: 0 0 auto;
  border-color: #ffffff;
  background: linear-gradient(145deg, #ffffff, #e9f2ff);
  color: #0747a6;
  box-shadow:
    0 0.8rem 1.5rem rgb(9 30 66 / 22%),
    inset 0 1px #ffffff,
    inset 0 -2px rgb(9 30 66 / 10%);
}

.primary-action:focus-visible,
.secondary-action:focus-visible {
  outline: 0.2rem solid rgb(12 102 228 / 28%);
  outline-offset: 0.16rem;
}

.light-action:focus-visible {
  outline-color: rgb(255 255 255 / 65%);
}

@media (min-width: 1200px) {
  .organizer-preview {
    transform: scale(1.1);
    transform-origin: center;
  }
}

@media (max-width: 980px) {
  .hero {
    grid-template-columns: 1fr;
    min-height: auto;
  }

  .hero-copy {
    max-width: 46rem;
  }

  .organizer-preview {
    width: min(38rem, 100%);
    margin-inline: auto;
  }

  .feature-grid {
    grid-template-columns: 1fr;
  }

  .feature-card {
    min-height: auto;
  }

  .feature-card p {
    min-height: auto;
  }
}

@media (max-width: 620px) {
  .home-page {
    padding-top: 2rem;
  }

  .hero,
  .features,
  .final-cta {
    width: min(100% - 1.25rem, 1180px);
  }

  h1 {
    font-size: clamp(2.8rem, 15vw, 4rem);
  }

  .hero-actions,
  .hero-actions a {
    width: 100%;
  }

  .hero-benefits {
    display: grid;
  }

  .preview-window {
    padding: 0.9rem;
    border-radius: 1.25rem;
  }

  .preview-stats {
    gap: 0.4rem;
  }

  .preview-stats > div {
    padding: 0.65rem;
  }

  .floating-badge {
    position: relative;
    right: auto;
    bottom: auto;
    max-width: none;
    margin: -0.35rem 0 0 1rem;
  }

  .final-cta {
    display: grid;
  }

  .final-cta .primary-action {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .primary-action,
  .secondary-action,
  .feature-card {
    transition: none;
  }

  .primary-action:hover,
  .secondary-action:hover,
  .feature-card:hover {
    transform: none;
  }
}
</style>
