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
- deutsche und englische Benutzeroberfläche
- Trennung der Daten nach angemeldetem Benutzer
- automatisierte Backend- und Frontend-Tests

Die native iOS-App ist der nächste geplante Ausbauschritt und noch nicht Bestandteil der aktuellen Version.

## Technologie

| Bereich | Technologie |
| --- | --- |
| Backend | ASP.NET Core 8, C#, Minimal APIs |
| Authentifizierung | ASP.NET Core Identity, JWT Bearer |
| Persistenz | Entity Framework Core, PostgreSQL 16 |
| Frontend | Vue 3, TypeScript, Vite, Pinia, Vue Router |
| Internationalisierung | Vue I18n, Deutsch und Englisch |
| Tests | xUnit, ASP.NET Core Integration Tests, Vitest |
| Lokale Infrastruktur | Docker Compose |
| Geplant | native iOS-App mit Swift/SwiftUI und Xcode |

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

## Qualität prüfen

Backend:

```bash
dotnet build backend/StudyOrganizer.sln
dotnet test backend/StudyOrganizer.sln
```

Frontend:

```bash
cd frontend
pnpm type-check
pnpm lint
pnpm exec vitest run
pnpm build
```

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
