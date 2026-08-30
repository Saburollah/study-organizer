# Agenten-Aktivitätslog: Superpowers-Versuch

Dieses Log erfasst ausschließlich beobachtbare Arbeitsschritte, Entscheidungen,
Dateiänderungen, Befehle, Testergebnisse, Commits und Review-Ausgänge. Es enthält
keine internen Gedankengänge. Tasks 1–6 wurden am 29. August 2026 aus dem
SDD-Ledger, den Task-Berichten und der Git-Historie rekonstruiert; ab Task 7 wird
das Log fortlaufend gepflegt.

## Vorbereitung und Plan

### 27.–28. August 2026 — Main-Agent — S0 bis S2

- **Ausführung:** inline durch den Main-Agenten.
- **Zweck:** isolierten Worktree und gemeinsamen Ausgangscommit prüfen, Baseline
  erfassen, Anforderungen mit `brainstorming` klären, Spezifikation und
  ausführbaren 12-Task-Plan erstellen.
- **Inspiziert:** `AGENTS.md` (nicht vorhanden),
  `Docs/moodle-architecture-notes.md`,
  `Docs/skill-evaluation/experiment-protocol.md`, Projektstruktur, vorhandene
  Backend-/Frontend-Tests und `.agents/skills`.
- **Geändert:** `Docs/skill-evaluation/superpowers-observations.md`,
  `Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md`,
  `Docs/superpowers/plans/2026-08-28-moodle-end-to-end.md`.
- **Entscheidungen:** lokaler Ende-zu-Ende-Schnitt mit deterministischem
  Mock-Moodle; gemeinsamer externer Kurs und persönliche Abonnements/Module;
  automatische Aufgaben nur bei strukturierter Frist; unsichere Inhalte werden
  zur Prüfung markiert; Moodle-gesteuerte Aufgaben sind außer ihrem Status
  schreibgeschützt.
- **Prüfungen:** Backend-Build bestanden; Domain-Baseline 17/17; 43/43
  bestehende API-Tests scheiterten bereits vor der Moodle-Arbeit beim Hoststart
  an unvollständiger JWT-Konfiguration; Frontend type-check/lint  bestanden,
  Vitest 77/77, Build bestanden.
- **Commits:** `3dc5ff5` (Design), `11a8135` (Implementierungsplan).
- **Unterbrechungen/Effizienz:** MSBuild/VSTest benötigten wegen lokaler
  Pipes/Sockets eine Sandbox-Freigabe; Frontend-Abhängigkeiten wurden einmal aus
  dem Lockfile installiert; der Plan ist mit über 1.500 Zeilen sehr detailliert.

## Implementierung

### 28. August 2026 — Task 1 — Domainmodell

- **Agent/Sitzung:** Subagent, Task-1-Implementierer; separater Reviewer und
  Fixturn. Historische Sitzungsnamen wurden vor Anlage dieses Logs nicht
  gespeichert.
- **Zweck:** externe Kurse, Abonnements, Inhalte, Task-Links und Scanläufe als
  Domänenmodell einführen.
- **Inspiziert:** Task-1-Brief, Designspezifikation, bestehende Domain-Entitäten
  und Domain-Testkonventionen.
- **Geändert:** `backend/src/Domain/ExternalCourses/` und
  `backend/tests/Domain.Tests/ExternalCourses/`; Task-1-Bericht.
- **Entscheidungen:** minimale persistenzfähige öffentliche Entitätsflächen;
  generierte IDs; unveränderliche Beziehungs-IDs; einmalige terminale
  Scanübergänge; geschlossene Liste sicherer Scan-Fehlercodes.
- **Tests:** ursprüngliches RED als Compile-Fehler vor den Produktionstypen;
  fokussiert abschließend 35/35 bestanden.
- **Commits:** `f1abab1 feat: model external course domain`;
  `8825513 fix: restrict scan run error codes`.
- **Review/Fixrunde:** ein Critical-Fund, weil `ScanRun.Fail` zunächst beliebige
  Texte akzeptierte; Fixrunde 1 ergänzte Safe-Code-Allowlist und Tests;
  Re-Review sauber.
- **Unterbrechungen/Effizienz:** VSTest benötigte die bekannte
  Loopback-/Sandbox-Ausnahme; RED/GREEN-Chronologie war nur über Bericht und
  beobachteten Worktree, nicht über einen Zwischencommit belegbar.

### 28. August 2026 — Task 2 — Provider-Port und Snapshot-Diff

- **Agent/Sitzung:** `/root/task_2_diff` (Subagent, Implementierung); separater
  Reviewer.
- **Zweck:** Snapshot-/Discovery-Verträge, sichere Providerfehler und
  deterministischen Inhaltsvergleich ergänzen.
- **Inspiziert:** Task-2-Brief, Domainmodell aus Task 1,
  Application-Projektstruktur und Solution-Datei.
- **Geändert:** `backend/src/Application/ExternalCourses/`,
  `backend/tests/Application.Tests/` und `backend/StudyOrganizer.sln`;
  Task-2-Bericht.
- **Entscheidungen:** `CourseSnapshotDiffer` bleibt im Application-Layer, weil
  er Application-Portrecords vergleicht; Providerexception enthält nur einen
  typisierten, sicher abgebildeten Fehler.
- **Tests:** RED wegen fehlender Namespaces/Typen; Application 6/6 und Domain
  52/52 bestanden.
- **Commit:** `0a52747 feat: define external course snapshots`.
- **Review/Fixrunde:** keine Findings; keine Fixrunde.
- **Unterbrechungen/Effizienz:** `NU1900` wegen nicht erreichbarer
  NuGet-Advisory-Metadaten, Tests selbst vollständig grün.

### 28. August 2026 — Task 3 — relationale Persistenz

- **Agent/Sitzung:** Subagent, Task-3-Implementierer; separater Reviewer.
  Historische Sitzungsnamen wurden nicht gespeichert.
- **Zweck:** DbSets, EF-Konfiguration, Migration und SQLite-Constrainttests für
  die fünf externen Entitäten ergänzen.
- **Inspiziert:** Task-3-Brief, `ApplicationDbContext`, vorhandene EF-Mappings,
  Migrationen und Infrastructure-Testfixtures.
- **Geändert:** Persistence-Konfiguration/Migration, Infrastructure-Testprojekt,
  Solution-Datei und Task-3-Bericht.
- **Entscheidungen:** fünf Unique-Constraints; sieben Foreign Keys; drei
  `Restrict`-Löschaktionen; der vorbestehende JWT-Baselinefehler wird nicht in
  diesem Task repariert.
- **Tests:** RED wegen fehlender DbSets; Infrastructure 5/5; Solution-Build mit
  0 Fehlern; Migration auf 5 Tabellen, 7 FKs, 5 Unique-Indizes und 3
  Restrict-Aktionen geprüft.
- **Commit:** `17f3022 feat: persist external course state`.
- **Review/Fixrunde:** kein Critical/Important; ein Minor zur Dispose-Behandlung
  des Testfixtures für den Gesamt-Review vorgemerkt.
- **Unterbrechungen/Effizienz:** vollständige API-Suite reproduzierte nur den
  bekannten JWT-Hoststartfehler.

### 28.–29. August 2026 — Task 4 — Mock-Registrierung und Abfragen

- **Agent/Sitzung:** Subagent, Task-4-Implementierer; separater Reviewer;
  ursprünglicher Fixagent und frischer Ersatzagent. Historische Sitzungsnamen
  wurden nicht gespeichert.
- **Zweck:** allowlist-basierte Mock-Moodle-Discovery, idempotente Registrierung,
  persönliches Modul und owner-scoped Abfragen implementieren.
- **Inspiziert:** Task-4-Brief, Providerverträge, EF-Constraints, bestehende
  Module/User-Handler und Testfixtures.
- **Geändert:** `backend/src/Infrastructure/ExternalCourses/`, zugehörige
  Application-Resultate/Interfaces und Infrastructure-Tests; Task-4-Bericht.
- **Entscheidungen:** feste Mock-URLs bleiben Produktionsliterale statt
  Testprojekt-Abhängigkeit; Scanstatus wird aus Lease/ScanRuns abgeleitet;
  Querysignaturen sind owner-scoped und asynchron.
- **Tests:** ursprüngliches RED wegen fehlender Contracts/Handler; nach zwei
  SQLite-`DateTimeOffset`-Korrekturen zunächst 28/28, nach Reviewfix final
  31/31 fokussierte ExternalCourse-Tests.
- **Commits:** `51c621a feat: register mock Moodle courses`;
  `4d5a6a4 test: cover external course conflict recovery`.
- **Review/Fixrunde:** zwei Important-Testlücken in Unique-Conflict-Retry und
  Active-Lease-Priorität; Fixrunde 1 ergänzte deterministische View-/Trigger-
  Tests und Mutation-Checks; Re-Review sauber. Ein Minor zur caller-seitigen
  Tracker-Bereinigung bleibt für den Gesamt-Review.
- **Unterbrechungen/Effizienz:** erster GREEN-Lauf scheiterte in zwei Tests an
  SQLite-DateTimeOffset-Sortierung; ein paralleler SQLite-Race-Ansatz wurde wegen
  Locks verworfen; erster Fixturn endete vor Änderungen am Agenten-Nutzungslimit,
  danach Übernahme durch frischen Agenten.

### 29. August 2026 — Task 5 — gemeinsamer idempotenter Scan

- **Agent/Sitzung:** Subagent, Task-5-Implementierer; separater Reviewer;
  Fixagent; frischer Verifikationsagent; `/root/task_5_rereview` (Re-Review).
- **Zweck:** Lease-geschützten gemeinsamen Scan, Snapshot-Diff, pro Abonnement
  deduplizierte Aufgaben und atomaren Erfolgszustand implementieren.
- **Inspiziert:** Task-5-Brief, Domain-/Application-Verträge, EF-Konfiguration,
  Registrationhandler und ExternalCourse-Testscenario.
- **Geändert:** Scanhandler, `StudyTask`-Synchronisierung, Scanresultate,
  kontrollierbarer Provider und ExternalCourse-Tests; Task-5-Bericht.
- **Entscheidungen:** Scan-Summary zählt Inhaltsidentitäten; erfolgreiche
  Inhalts-/Task-/Link-/Run-/Lease-Änderungen sind atomar; Provider-Content-IDs
  werden genau einmal vor Validierung, Diff und Persistenz kanonisiert.
- **Tests:** ursprüngliches RED wegen fehlendem Handler/Synchronisierung;
  zunächst Domain 53/53, Application 6/6, Infrastructure 44/44 und Scan 13/13;
  nach Fix final Infrastructure 46/46.
- **Commits:** `634947e feat: scan shared courses idempotently`;
  `721216f fix: canonicalize external content identities`.
- **Review/Fixrunde:** ein Important-Fund zur Prüfung roher IDs vor späterer
  Domain-Trimmung; Fixrunde 1 ergänzte zwei Regressionstests und einmalige
  Kanonisierung; Re-Review sauber.
- **Unterbrechungen/Effizienz:** Fixagent erreichte nach RED/GREEN-Bericht und
  Änderungen, aber vor Commit, sein Nutzungslimit; ein frischer Agent prüfte und
  committete denselben erhaltenen Worktree-Stand.

### 29. August 2026 — Task 6 — robuste Scanänderungen und Fehlerpfade

- **Agent/Sitzung:** `/root/task_6_robustness` (Subagent, Implementierung und
  erster Fixturn), `/root/task_6_review` (Review),
  `/root/task_6_fix_verify` (laufende Übernahme der Fixverifikation).
- **Zweck:** offene Aufgaben synchronisieren, erledigte Aufgaben erhalten,
  Late-Subscriber materialisieren, fehlende Inhalte markieren, sichere
  Fehleraudits/Lease-Cleanup und Datenbank-Konkurrenz belegen.
- **Inspiziert:** Task-6-Brief, Scan-/Registrationhandler, Domain-ScanRun,
  kontrollierbarer Provider, SQLite-Fixture und ExternalCourse-Tests.
- **Geändert:** `ScanRun`, Scan-/Registrationhandler, zugehörige Domain- und
  Infrastructure-Tests sowie Task-6-Bericht. In Review-Fixrunde 1 bisher nur
  drei Infrastructure-Testdateien; kein Produktcode.
- **Entscheidungen:** Provider-/Validierungsfehler liefern sichere Outcomes;
  Cancellation verwendet `scan_cancelled`, unerwartete Fehler `scan_failed` und
  werden nach best-effort Cleanup erneut geworfen; Konkurrenztest verwendet
  unabhängige DbContexts gegen dieselbe Datenbank.
- **Tests vor Review:** Task-RED Domain 2 fehlgeschlagen; Infrastructure 9
  fehlgeschlagen; Transaktions-Mutation 1 fehlgeschlagen. Final vor Review:
  Domain 55/55, Application 6/6, Infrastructure 61/61, gesamt 122/122.
- **Commits:** `07b81a1 feat: preserve course state across scan changes`;
  `468a715 test: harden external scan guarantees`.
- **Review/Fixrunde:** Review `CHANGES_REQUESTED`, keine Critical- oder belegten
  Produktionsfehler; drei Important-Testlücken: Past-Due-Filter nicht
  mutationsempfindlich, Description-Synchronisierung nicht belegt, Fehlercode-
  Matrix/Last-Success-Erhalt unvollständig. Ein Minor: umgebungsbedingtes
  `NU1900`. Fixrunde 1 stärkte die drei Testbereiche; fokussiert 63/63 und volle
  Infrastructure-Suite 63/63 bestanden. Kein Per-Task-Re-Review nach Wechsel des
  Ausführungsmodus; ein Gesamt-Review bleibt ausstehend.
- **Unterbrechungen/Effizienz:** erster Fixturn änderte die drei Testdateien,
  endete dann am Agenten-Nutzungslimit vor Tests, Bericht und Commit; der
  unverlorene Worktree wurde an `/root/task_6_fix_verify` übergeben. Dieser wurde
  auf Benutzeranweisung gestoppt; eine gerade aktive Mutation des Due-Date-Filters
  wurde erkannt und exakt auf den Commitstand zurückgesetzt.

## Tasks 7–12

Ab hier erhält jeder Task zusätzlich eine ausdrückliche Kennzeichnung, ob die
Produktarbeit inline durch den Main-Agenten oder durch einen Subagenten erfolgte.

### 29. August 2026 — Task 7 — Schutz und Quellenmetadaten

- **Ausführung:** vollständig inline durch den Main-Agenten; keine Subagents und
  kein Per-Task-Reviewer.
- **Zweck:** Moodle-gesteuerte Tasks und verknüpfte Module schützen sowie
  Quellenmetadaten additiv ausgeben.
- **Inspiziert:** Task-7-Planabschnitt, vorhandene Task-/Module-Interfaces,
  Infrastructure-Handler, API-Endpunkte und betroffene API-Tests.
- **Geändert:** Task-/Module-Applicationverträge und Resultate,
  `StudyTaskHandler`, `ModuleHandler`, Task-/Module-API-Modelle und Endpunkte,
  beide vorhandenen API-Testdateien sowie neuer
  `ExternalSourceProtectionTests.cs`; zusätzlich ignorierter Task-7-Brief.
- **Entscheidungen:** Statusänderungen bleiben erlaubt; Update/Delete werden
  geschützt; Quellenfelder sind additiv; bestehendes Verhalten außerhalb
  verknüpfter Moodle-Daten bleibt erhalten.
- **Tests:** erwartetes RED mit 15 Compilefehlern wegen fehlender Outcomes und
  Metadaten; erster Handler-GREEN 5/6, dann zwei nacheinander belegte
  SQLite-`DateTimeOffset`-Sortierfehler in Task- und Module-Query; minimale
  Client-Sortierung führte zu 6/6 fokussierten Schutztests. Breite
  Infrastructure-Suite 69/69. Fokussierte Task-/Module-API-Suite kompiliert,
  scheitert aber wie die Baseline mit 31/31 Fällen vor Endpunktausführung an
  unvollständiger JWT-Konfiguration. Solution-Build: 0 Fehler, ein
  umgebungsbedingtes `NU1900`.
- **Commit:** `3c080d1 feat: protect Moodle-managed study data`.
- **Review/Fixrunde:** kein Per-Task-Review auf Benutzeranweisung; genau ein
  Gesamt-Review bleibt für das Ende. Die beiden SQLite-Korrekturen folgten dem
  vollständig gelesenen `systematic-debugging`-Ablauf.
- **Unterbrechungen/Effizienz:** Der API-RED-Aufruf wurde vom Tool zunächst als
  lange laufend gemeldet, lieferte beim kontrollierten Abruf aber nach 1,8 s den
  erwarteten Compilefehler. Die 31 identischen JWT-Stacktraces haben wenig
  Zusatznutzen; deshalb keine wiederholte volle API-Suite und Build als
  planmäßiger Ersatznachweis.

### 29. August 2026 — Task 8 — geschützte Moodle-Kurs-API

- **Ausführung:** vollständig inline durch den Main-Agenten; keine Subagents und
  kein Per-Task-Reviewer.
- **Zweck:** vier authentifizierte Kursrouten mit owner-scoped Handleraufrufen,
  stabilen JSON-Modellen und sicheren Status-/Fehlerabbildungen bereitstellen.
- **Inspiziert:** Task-8-Planabschnitt, Designspezifikation zur API,
  ExternalCourse-Applicationinterfaces/-resultate, `Program.cs`, vorhandene
  API-Testfactory-/JWT-Muster und `JwtOptions`.
- **Geändert:** neue `Api/ExternalCourses/ExternalCourseModels.cs` und
  `ExternalCourseEndpoints.cs`, `Api/Program.cs` sowie neue
  `Api.Tests/ExternalCourses/ExternalCourseEndpointsTests.cs`.
- **Entscheidungen:** exakte Routen unter `/api/course-subscriptions`; alle vier
  erfordern Authentifizierung; 201/200 für Created/Existing; sichere
  `invalid_course_url`/`unsupported_course_url`; fremde Abonnements 404;
  `scan_in_progress` 409; externe/ungültige Snapshots 502 mit ausschließlich
  Application-Safe-Code. Mock-Provider singleton, drei Handler scoped.
- **Tests:** erwartetes Compile-RED wegen fehlendem API-Namespace. Erster GREEN-
  Versuch: 17/17 Hoststarts `JWT configuration was not found`; nach Signing-Key-
  Bootstrap 17/17 `incomplete or invalid`; Ursache über
  `systematic-debugging` auf frühes `Program`-Binding vor dem Testhost-
  Configuration-Callback eingegrenzt. Vollständiger, mit bestehenden Tests
  identischer Bootstrap plus weiterhin komplettes In-Memory-Dictionary führte
  zu 17/17 fokussierten API-Tests. Solution-Build: 0 Fehler, ein
  umgebungsbedingtes `NU1900`.
- **Commit:** `c4b7322 feat: expose Moodle course APIs`.
- **Review/Fixrunde:** kein Per-Task-Review; ein Gesamt-Review bleibt nach
  Task 12. Zwei aufeinander aufbauende Test-Harness-Korrekturen, keine Änderung
  am Produkt-Bootstrap oder am bekannten Baselinefehler.
- **Unterbrechungen/Effizienz:** Zwei Läufe erzeugten jeweils 17 gleichartige
  JWT-Stacktraces; nach eindeutiger Ursachenbestätigung wurde nur die fokussierte
  Suite wiederholt und keine volle API-Suite gestartet.

### 29. August 2026 — Task 9 — typisierter Frontend-Client

- **Ausführung:** vollständig inline durch den Main-Agenten; keine Subagents und
  kein Per-Task-Reviewer.
- **Zweck:** TypeScript-Verträge und vier dünne HTTP-Methoden für die Task-8-API
  bereitstellen.
- **Inspiziert:** Task-9-Planabschnitt, `apiClient.ts`, bestehende Task-/Module-
  Service- und Testmuster, Frontend-TypeScript-/Vite-Konfiguration.
- **Geändert:** neue `externalCourseModels.ts`, `externalCourseService.ts` und
  `__tests__/externalCourseService.spec.ts`.
- **Entscheidungen:** API-JSON wird ohne zusätzliche Transformation typisiert;
  Subscription-IDs werden über `encodeURIComponent` geschützt; Pfade entsprechen
  exakt `/api/course-subscriptions` sowie `/contents` und `/scan`; Singleton über
  das Interface exportiert.
- **Tests:** erwartetes Import-RED wegen fehlendem Service; danach 1/1 Testdatei
  und 4/4 Routentests grün; `pnpm type-check` grün.
- **Commit:** `1e41522 feat: add Moodle course client`.
- **Review/Fixrunde:** kein Per-Task-Review; keine Fixrunde; ein Gesamt-Review
  bleibt nach Task 12.
- **Unterbrechungen/Effizienz:** keine; nur fokussierter Vitest-Lauf und danach
  der breitere Typecheck.

### 30. August 2026 — Task 10 — Moodle-Kursansicht und Navigation

- **Ausführung:** vollständig inline durch den Main-Agenten; keine Subagents und
  kein Per-Task-Reviewer.
- **Zweck:** geschützte, zweisprachige Kursregistrierung, Inhaltsanzeige und
  manuellen Scan als sichtbaren End-to-End-Workflow bereitstellen.
- **Inspiziert:** Task-10-Planabschnitt, Task-9-Client/-Modelle, bestehende
  Router-, App-, i18n- und Vue-Testmuster sowie der fertige Task-10-Diff.
- **Geändert:** `CourseRegistrationForm.vue` mit Test, `MoodleCoursesView.vue`
  mit Test, Router und Routertests, authentifizierte App-Navigation sowie
  deutsche und englische Übersetzungen.
- **Entscheidungen:** nur getrimmte HTTPS-Links akzeptieren; Inhalte nach
  Registrierung und Scan neu laden; alle fünf Scan-Kennzahlen anzeigen;
  Quellenlinks mit `noopener noreferrer`; Route `moodle-courses` authentifiziert.
- **Tests:** erwartetes Komponenten-Import-RED; erster Komponenten-GREEN-Lauf
  11/12 wegen einer zu whitespace-empfindlichen DOM-Textassertion, danach 12/12;
  Router 8/8. Beim Planabgleich zusätzlicher gezielter RED-Nachweis für die
  fehlende Kennzahl `newTaskEligibleCount`, danach View 7/7. Final relevante
  Frontend-Suite 24/24, `pnpm type-check` und `pnpm lint` grün; `git diff
  --check` ohne Fund.
- **Commit:** `0fc36cc feat: add Moodle course workflow`.
- **Review/Fixrunde:** kein Per-Task-Review; keine wiederholte Reviewrunde; ein
  Planabgleich vor Commit schloss die einzelne Scan-Summary-Lücke testgetrieben.
  Gesamt-Review bleibt nach Task 12.
- **Unterbrechungen/Effizienz:** keine Unterbrechung. Die breitere 24-Test-Suite
  lief nur nach fokussiertem GREEN; Typecheck und Lint wurden parallel ausgeführt.

### 30. August 2026 — Task 11 — Provenienz und Aktionsschutz im Frontend

- **Ausführung:** vollständig inline durch den Main-Agenten; keine Subagents und
  kein Per-Task-Reviewer.
- **Zweck:** Moodle-Quellen an erzeugten Aufgaben sichtbar machen und
  quellengesteuerte Task-/Modulaktionen in vorhandenen Ansichten sperren.
- **Inspiziert:** Task-11-Planabschnitt, Task-/Module-Modelle und Services,
  Dashboard-Fixture, beide vorhandenen Views/Tests sowie i18n-Strukturen.
- **Geändert:** Task-/Module-Modelle und Testfixtures, `StudyTasksView.vue`,
  `ModulesView.vue`, beide Viewtests sowie deutsche/englische Übersetzungen.
- **Entscheidungen:** externe Tasks zeigen Kurs und sicheren Quellenlink;
  Edit/Delete fehlen, Status bleibt verfügbar. Verknüpfte Module bleiben
  editierbar; Delete ist deaktiviert und der Handler öffnet auch programmatisch
  keinen Dialog; Hilfetext verweist auf einen künftigen Abmeldeablauf.
- **Tests:** erwartetes View-RED 2 fehlgeschlagen/17 bestanden; nach minimalem
  GREEN beide Viewdateien 19/19. Angepasste Service-/Dashboardtests 10/10.
  Vollständig: 20 Testdateien und 97/97 Tests, Typecheck, Lint und Build grün;
  `git diff --check` ohne Fund.
- **Commit:** `5ec8b3d feat: identify Moodle-managed tasks`.
- **Review/Fixrunde:** kein Per-Task-Review und keine Fixrunde; ein Gesamt-Review
  bleibt nach Task 12.
- **Unterbrechungen/Effizienz:** erster Commitversuch scheiterte an der
  Worktree-`index.lock`-Sandboxgrenze und wurde nach gezielter Freigabe einmal
  wiederholt. Die volle Frontend-Suite lief genau einmal nach fokussiertem GREEN.

### 30. August 2026 — Task 12 — Dokumentation und Gesamtabnahme

- **Ausführung:** inline durch den Main-Agenten; keine Subagents und kein
  Per-Task-Reviewer.
- **Zweck:** lokalen Fixture-Ablauf reproduzierbar dokumentieren und die
  vollständigen Protokollprüfungen ausführen.
- **Inspiziert:** Task-12-Planabschnitt, README, Mock-Provider, API-Startup/CORS,
  Appsettings/Launch-Profil, User-Secrets nur als gesetzte Schlüssel/Längen,
  `.env`-Ignore, EF-Migrationsstatus, laufende Ports und finaler Feature-Diff.
- **Geändert:** `Program.cs` lädt die zwei bekannten Appsettings in versteckten
  Content-Roots mit einem nicht-ausschließenden FileProvider und stellt danach
  Secrets-/Environment-/Command-Line-Priorität wieder her; README und beide
  Versuchslogs wurden korrigiert und vervollständigt.
- **Entscheidungen:** kein synthetischer Test für menschliche README-Prosa;
  primärer Link und Alias werden als ein kanonischer, vollständig lokaler Kurs
  dokumentiert. Kein externer Browser-Skill; der Benutzer führte den Sichtlauf
  selbst vollständig erfolgreich aus. Lokale DB-/Volume-/Passwort- und Vite-
  Cache-Zustände sind Setup-Probleme; der JSON-Provider-Ausschluss im versteckten
  `.worktrees`-Pfad war eine reproduzierbare Repository-Startlücke.
- **Tests:** vorhandener CORS-/Startup-Test als RED 0/2 wegen früher JWT-
  Validierung, nach Fix 2/2; ExternalCourse-API 17/17, gesamte API 65/65.
  Kontrollierter Start ohne JWT-/CORS-Overrides: Kestrel Development aktiv,
  Health 200 und CORS-Preflight 204 mit erlaubter Origin. EF listet alle vier
  Migrationen ohne Pending. Final Solution-Build 0 Fehler/zwei `NU1900`,
  Backend 195/195; Frontend Typecheck, Lint, 20 Dateien/97 Tests und Build grün.
- **Commit:** `0e0c073 fix: load local settings in isolated worktrees`;
  Evidenzcommit folgt separat.
- **Review/Fixrunde:** kein Per-Task-Review; genau ein Gesamt-Review folgt nach
  dem Task-12-Evidenzcommit.
- **Unterbrechungen/Effizienz:** fokussierter API- und voller Solution-Test
  scheiterten je einmal vor Teststart an gesperrten MSBuild-Named-Pipes und
  bestanden nach gezielter Sandboxfreigabe. Docker-Status war wegen gesperrtem
  Socket und fehlender lokaler `.env` nicht verfügbar; auf nachträgliche Browser-
  oder E2E-Installation wurde verzichtet. Der manuelle Start mit dem bisherigen
  README-Verzeichnispfad reproduzierte die JWT-Ausnahme; Secrets waren vorhanden
  und der Key 64 Zeichen lang. Eine erste Diagnose deutete den nur scheinbar
  laufenden Prozess fälschlich als erfolgreichen `.csproj`-Start. Redigierte
  Provider-Diagnostik belegte danach: Appsettings-Dateien existierten, wurden vom
  Standard-PhysicalFileProvider unter `.worktrees` aber als nicht existent
  behandelt. Zwei Shell-/Compilekorrekturen betrafen nur temporäre Diagnostik.
  Die ungetrackte 30-Byte-Datei `0` wurde nicht verändert.
