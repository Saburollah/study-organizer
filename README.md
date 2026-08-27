# Study Organizer

Study Organizer ist eine Webanwendung zur persönlichen Organisation des Studiums. Studierende verwalten Lernmodule, Aufgaben und Fristen, sehen ihren aktuellen Fortschritt im Dashboard und pflegen ihr Profil. Die Oberfläche ist auf Deutsch und Englisch verfügbar.

## Aktueller Stand

Die Webanwendung und das Backend sind als funktionsfähige erste Version umgesetzt.

- Registrierung und Anmeldung mit sicherem Passwort und JWT
- Wiederherstellung der Anmeldung nach dem Neuladen der Seite
- persönliches Profil mit Vorname, Nachname, Geburtsdatum und Geschlecht
- Änderung des eigenen Passworts
- Lernmodule anlegen, anzeigen, bearbeiten und löschen
- Aufgaben je Lernmodul anlegen, anzeigen, bearbeiten und löschen
- Aufgaben als offen oder erledigt markieren
- Dashboard mit offenen, überfälligen und erledigten Aufgaben
- Mock-Moodle-Kurse mit einem Lernmodul verbinden und manuell scannen
- PDF-, Link- und Aktivitätsinhalte als persönliche Aufgaben importieren
- gemeinsame Kursscans ohne doppelte Verarbeitung für mehrere Abonnenten
- deutsche und englische Benutzeroberfläche
- Trennung der Daten nach angemeldetem Benutzer
- automatisierte Backend- und Frontend-Tests

Die native iOS-App ist der nächste geplante Ausbauschritt und noch nicht Bestandteil der aktuellen Version.

## Technologie

| Bereich               | Technologie                                               |
| --------------------- | --------------------------------------------------------- |
| Backend               | ASP.NET Core 8, C#, Minimal APIs                          |
| Authentifizierung     | ASP.NET Core Identity, JWT Bearer                         |
| Persistenz            | Entity Framework Core, PostgreSQL 16                      |
| Frontend              | Vue 3, TypeScript, Vite, Pinia, Vue Router                |
| Internationalisierung | Vue I18n, Deutsch und Englisch                            |
| Tests                 | xUnit, ASP.NET Core Integration Tests, Vitest, Playwright |
| Lokale Infrastruktur  | Docker Compose                                            |
| Geplant               | native iOS-App mit Swift/SwiftUI und Xcode                |

## Projektstruktur

```text
study-organizer/
├── backend/
│   ├── src/
│   │   ├── Api/             # HTTP-Endpunkte und API-Konfiguration
│   │   ├── Application/     # Schnittstellen und Anwendungsmodelle
│   │   ├── Domain/          # Domänenobjekte und Geschäftsregeln
│   │   └── Infrastructure/  # EF Core, Identity und Handler
│   └── tests/
├── frontend/                # Vue-/TypeScript-Anwendung
├── Docs/                    # Anforderungen, User Stories und Diagramme
└── compose.yaml             # lokaler PostgreSQL-Container
```

## Voraussetzungen

- .NET SDK 8
- Node.js 22.18 oder neuer
- pnpm
- Docker Desktop mit Docker Compose

## Lokal starten

### 1. Repository vorbereiten

```bash
git clone https://github.com/Saburollah/study-organizer.git
cd study-organizer
cp .env.example .env
```

Passe `POSTGRES_PASSWORD` in `.env` an. Die Datei `.env` wird nicht in Git gespeichert.

### 2. PostgreSQL starten

```bash
docker compose up -d
docker compose ps
```

### 3. Backend-Secrets konfigurieren

Der Wert bei `Password` muss mit `POSTGRES_PASSWORD` aus `.env` übereinstimmen.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=study_organizer;Username=study_organizer;Password=DEIN_PASSWORT" \
  --project backend/src/Api/StudyOrganizer.Api.csproj

dotnet user-secrets set "Jwt:SigningKey" \
  "$(openssl rand -base64 48)" \
  --project backend/src/Api/StudyOrganizer.Api.csproj
```

### 4. Datenbankmigrationen anwenden

```bash
dotnet ef database update \
  --project backend/src/Infrastructure \
  --startup-project backend/src/Api
```

### 5. Backend starten

```bash
dotnet run --project backend/src/Api
```

- API: `http://localhost:5101`
- Swagger: `http://localhost:5101/swagger`
- Health Check: `http://localhost:5101/health`

### 6. Frontend starten

Öffne ein zweites Terminal:

```bash
cd frontend
pnpm install
pnpm dev
```

Das Frontend ist anschließend unter `http://localhost:5173` erreichbar.

## Mock-Moodle-Kursimport

Der aktuelle Kursimport ist ein kontrollierter Vertikalschnitt mit einer
deterministischen Mock-Moodle-Quelle. Er beweist Registrierung, gemeinsame
Scans, Deduplizierung, persönliche Aufgaben, Fehlerzustände und den
Wiederholungsablauf. Eine Anmeldung an einem echten Moodle, tägliches Polling,
LLM-Erkennung und Benachrichtigungen gehören noch nicht zu diesem Schnitt.

Inaktive External Courses werden standardmäßig nach einer Schonfrist von
30 Tagen periodisch bereinigt. Persönlich referenzierte Inhalte behalten ihre
stabile Identität, während externe Metadaten entfernt werden. Die Werte können
für eine Laufzeitumgebung über
`ExternalCourseCleanup__RetentionPeriod` und
`ExternalCourseCleanup__Interval` als .NET-`TimeSpan` überschrieben werden.
Die 30 Tage sind eine technische Ausgangseinstellung und keine rechtlich
geprüfte Aufbewahrungsfrist.

## Produktionsmigrationen

Das produktive Docker-Image enthält ein EF-Core-Migrationsbundle aus demselben
Build wie die API. Beim Containerstart führt `scripts/start-api.sh` zuerst alle
ausstehenden Migrationen aus. Erst nach einem erfolgreichen Abschluss wird die
API gestartet. Schlägt eine Migration fehl, beendet sich der Container mit
einem Fehlerstatus; dadurch kann kein Deployment mit einem veralteten oder nur
teilweise aktualisierten Schema gesund gemeldet werden.

Für Render oder eine andere Docker-Laufzeit werden die bestehenden Secrets nur
als Umgebungsvariablen des Containers benötigt, insbesondere
`ConnectionStrings__DefaultConnection` und `Jwt__SigningKey`. Es ist kein .NET
SDK und kein global installiertes `dotnet-ef` im Runtime-Image erforderlich.
Die Datenbankverbindung darf weder als Docker-Build-Argument noch direkt in
einem Startbefehl hinterlegt werden.

Der vollständige Ablauf gegen drei flüchtige PostgreSQL-Datenbanken lässt sich
lokal prüfen:

```bash
scripts/test-production-migrations.sh
```

Der Test deckt eine leere Datenbank, das Upgrade vom vorherigen
Migrationsstand, wiederholte idempotente Starts, den Erhalt vorhandener Daten
und einen absichtlich fehlgeschlagenen Migrationslauf ab. Docker Desktop muss
dazu laufen. Derselbe Nachweis wird in
`.github/workflows/production-migrations.yml` für Pull Requests ausgeführt.

### Wiederherstellung nach einem Migrationsfehler

1. Das fehlgeschlagene Deployment nicht durch Überspringen der Migration
   freigeben. Zuerst Container- und PostgreSQL-Logs prüfen und bei
   produktiven Daten ein aktuelles Backup sicherstellen.
2. Die Ursache beheben oder ein geprüftes Vorwärts-Fix als neue Migration
   bereitstellen. Produktive Migrationen nicht ungeprüft zurückrollen.
3. Falls die Migration getrennt vom API-Start erneut ausgeführt werden muss,
   exakt dasselbe unveränderte Image verwenden und die Secrets ausschließlich
   aus der geschützten Laufzeitumgebung übernehmen:

   ```bash
   docker run --rm \
     --env ConnectionStrings__DefaultConnection \
     --env Jwt__SigningKey \
     --entrypoint /app/efbundle \
     EXAKTER_IMAGE_TAG --no-color
   ```

4. Erst nach erfolgreichem Abschluss das API-Deployment erneut starten und
   Health Check sowie Anwendungsschema kontrollieren.

## Qualität prüfen

Backend:

```bash
dotnet build backend/StudyOrganizer.sln
dotnet test backend/StudyOrganizer.sln
```

Die Infrastructure-Integrationstests starten über Testcontainers
automatisch einen flüchtigen PostgreSQL-16-Container. Dafür muss Docker
Desktop laufen. Eine lokale `.env`-Datei oder eine zuvor gestartete
Entwicklungsdatenbank ist für die Tests nicht erforderlich. Der
Testcontainer wird nach dem Testlauf automatisch entfernt.

Frontend:

```bash
cd frontend
pnpm type-check
pnpm lint
pnpm exec vitest run
pnpm build
```

Browser-Golden-Path für den Kursimport:

```bash
cd frontend
pnpm test:e2e
```

Der Befehl startet PostgreSQL per Docker Compose, erstellt eine isolierte
`*_e2e`-Datenbank, wendet alle Migrationen an und startet API sowie Vue-App auf
separaten Testports. Playwright führt den Test standardmäßig headless mit
Chromium aus. Die E2E-Datenbank wird auch nach einem fehlgeschlagenen Lauf
entfernt; die normale Entwicklungsdatenbank bleibt unverändert. Vor dem ersten
Lauf müssen `.env` vorhanden, die Frontend-Abhängigkeiten installiert und der
Chromium-Browser einmalig mit folgendem Befehl eingerichtet sein:

```bash
cd frontend
pnpm exec playwright install chromium
```

Bei einem Fehler bleiben Trace und Screenshot unter `frontend/test-results/`
erhalten. Derselbe headless Lauf wird durch
`.github/workflows/playwright-e2e.yml` für Pull Requests ausgeführt.

## Dokumentation

- [Produktvision](Docs/vision.md)
- [Anforderungen](Docs/requirements.md)
- [User Stories](Docs/user-stories.md)
- [Use-Case-Diagramm](Docs/diagrams/use-case.puml)
- [C4-Diagramme](Docs/diagrams/c4/)

## Roadmap

- native iOS-App mit SwiftUI
- Push-Benachrichtigungen für anstehende Fristen
- Notizen und Dateianhänge
- Kalender- und Stundenplanansicht
- erweiterte Statistiken und Lernfortschritt
- optionale Zusammenarbeit in Lerngruppen
