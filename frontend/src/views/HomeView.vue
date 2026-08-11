<template>
  <section class="home">
    <div class="hero">
      <p class="eyebrow">DEIN STUDIUM. KLAR ORGANISIERT.</p>

      <h1>Organisiere dein Studium an einem Ort.</h1>

      <p class="intro">
        Verwalte deine Lernmodule und Aufgaben übersichtlich und behalte
        deine nächsten Ziele im Blick.
      </p>

      <span class="status">
        <span class="status-dot" aria-hidden="true"></span>
        Frontend erfolgreich eingerichtet
      </span>
    </div>

    <section
      class="features"
      aria-labelledby="features-heading"
    >
      <h2 id="features-heading">
        Deine Werkzeuge
      </h2>

      <div class="feature-grid">
        <article class="feature-card">
          <span class="feature-number">01</span>
          <h3>Lernmodule</h3>
          <p>
            Erstelle Module für deine Fächer und verwalte sie an einem Ort.
          </p>
        </article>

        <article class="feature-card">
          <span class="feature-number">02</span>
          <h3>Aufgaben</h3>
          <p>
            Plane Aufgaben, setze Fälligkeiten und aktualisiere ihren Status.
          </p>
        </article>

        <article class="feature-card">
          <span class="feature-number">03</span>
          <h3>Fortschritt</h3>
          <p>
            Erkenne schnell, was erledigt ist und was als Nächstes ansteht.
          </p>
        </article>
      </div>
    </section>
  </section>
</template>

<style scoped>
.home {
  width: min(1120px, calc(100% - 2rem));
  margin: 0 auto;
  padding: 5rem 0;
}

.hero {
  max-width: 760px;
}

.eyebrow {
  margin: 0 0 1rem;
  color: #0c66e4;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.12em;
}

h1 {
  margin: 0;
  color: #172b4d;
  font-size: clamp(2.5rem, 7vw, 5rem);
  line-height: 1;
  letter-spacing: -0.04em;
}

.intro {
  max-width: 640px;
  margin: 1.5rem 0;
  color: #44546f;
  font-size: 1.15rem;
  line-height: 1.7;
}

.status {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  color: #216e4e;
  font-weight: 600;
}

.status-dot {
  width: 0.65rem;
  height: 0.65rem;
  border-radius: 50%;
  background: #22a06b;
}

.features {
  margin-top: 6rem;
}

.features h2 {
  margin-bottom: 1.5rem;
  color: #172b4d;
  font-size: 1.75rem;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}

.feature-card {
  padding: 1.5rem;
  border: 1px solid #dfe3e8;
  border-radius: 1rem;
  background: #ffffff;
}

.feature-number {
  color: #0c66e4;
  font-size: 0.8rem;
  font-weight: 700;
}

.feature-card h3 {
  margin: 1rem 0 0.5rem;
  color: #172b4d;
}

.feature-card p {
  margin: 0;
  color: #626f86;
  line-height: 1.6;
}

@media (max-width: 720px) {
  .home {
    padding: 3rem 0;
  }

  .feature-grid {
    grid-template-columns: 1fr;
  }

  .features {
    margin-top: 4rem;
  }
}
</style>