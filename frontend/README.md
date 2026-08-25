# Study Organizer Frontend

Das Frontend ist eine Vue-3-Single-Page-Application mit TypeScript. Es verwendet die ASP.NET-Core-API im Verzeichnis `backend`.

## Wichtige Bausteine

- Vue Router für Seiten und Zugriffsschutz
- Pinia für den Authentifizierungszustand
- Vue I18n für Deutsch und Englisch
- Feature-Services für Authentifizierung, Dashboard, Module, Aufgaben und Profil
- Vitest und Vue Test Utils für automatisierte Tests
- Playwright für den browserbasierten Kursimport-Golden-Path

## Konfiguration

Die API-Adresse kann in `frontend/.env` gesetzt werden:

```env
VITE_API_BASE_URL=http://localhost:5101
```

Ohne diese Variable wird lokal ebenfalls `http://localhost:5101` verwendet.

## Entwicklung

```bash
pnpm install
pnpm dev
```

Das Frontend läuft standardmäßig unter `http://localhost:5173`.

## Prüfungen

```bash
pnpm type-check
pnpm lint
pnpm exec vitest run
pnpm build
```

Der Playwright-Golden-Path verwendet die echte Vue-App, API und eine isolierte
PostgreSQL-Datenbank sowie den deterministischen Mock-Moodle-Adapter. Er startet
alle benötigten Prozesse selbst und läuft standardmäßig headless:

```bash
pnpm exec playwright install chromium # einmalig
pnpm test:e2e
```

Die Repository-`.env` muss vorhanden sein. Trace und Screenshot fehlgeschlagener
Läufe liegen unter `test-results/`; der HTML-Bericht liegt unter
`playwright-report/`.

## Struktur

```text
src/
├── components/   # wiederverwendbare UI-Komponenten
├── config/       # Laufzeitkonfiguration
├── features/     # Models, Stores und API-Services je Funktion
├── i18n/         # deutsche und englische Übersetzungen
├── router/       # Routen und Authentifizierungs-Guard
└── views/        # Seiten der Anwendung
```
