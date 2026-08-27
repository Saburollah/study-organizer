# Versuchsprotokoll: Matt Pocock Skills und Superpowers

## Forschungsziel

Dieses Experiment vergleicht zwei agentische Entwicklungsworkflows an einem realen Feature des Study Organizers:

- Matt Pococks Engineering Skills
- Obras Superpowers

Der Vergleich bewertet nicht nur den erzeugten Code. Er untersucht auch Anforderungsklärung, Architekturarbeit, Planung, Testdisziplin, Review-Qualität, menschlichen Steuerungsaufwand und die Wiederaufnahme der Arbeit in einem frischen Agentenkontext.

Das Ergebnis des Experiments ist die Tatsachengrundlage für ein deutschsprachiges Bewerbungs-Paper. Beobachtungen werden während der Arbeit festgehalten; Schlussfolgerungen entstehen erst nach beiden Versuchen.

## Test-Feature

Studierende registrieren einen External Course im Study Organizer. Das System erkennt neue oder geänderte External Learning Contents, verhindert Duplikate und stellt sie Benutzern mit aktiver Course Subscription als Imported Study Tasks bereit.

Die Architekturfragen sind in [`../moodle-architecture-notes.md`](../moodle-architecture-notes.md) dokumentiert. Diese Notiz ist eine neutrale fachliche Eingabe für beide Skill-Suites und keine fertige Spezifikation.

## Ausgangsstand

| Merkmal | Wert |
| --- | --- |
| Projekt-Branch für den Matt-Versuch | `experiment/matt` |
| Ausgangscommit vor den neuen Dokumenten | `2f38578d63f988b18a749ac7c0045158fd0ed45b` |
| Zentrales Skills-Repository | `https://github.com/Saburollah/agent-skills.git` |
| Matt-Skill-Commit | `f6de92c45098088741afdba6dbb275199803cf78` |
| Matt-Version | `matt-v1.0` |
| Superpowers-Version | Vor dem Superpowers-Versuch festschreiben |
| Datum | 20. August 2026 |

Vor dem ersten Feature-Code werden die neutralen Experimentdokumente committed und der endgültige gemeinsame Ausgangscommit notiert. Der Superpowers-Versuch startet nach Möglichkeit von demselben Commit in einem getrennten Branch oder Worktree.

## Faire Vergleichsbedingungen

1. Beide Skill-Suites erhalten dieselbe anfängliche Feature-Beschreibung und dieselbe Architektur-Arbeitsnotiz.
2. Beide Versuche starten von demselben dokumentierten Codezustand, sofern keine technische Abhängigkeit dagegen spricht.
3. Jeder Versuch verwendet ausschließlich die projektspezifische Suite seines Branches. Erkenntnisse dürfen im Paper verglichen, aber Implementierungsartefakte werden vor Abschluss des Vergleichs nicht zwischen den Versuchsbranches kopiert.
4. Produktentscheidungen des Benutzers bleiben in beiden Versuchen gleich. Neue Fragen und die Art, wie eine Suite sie entdeckt, werden protokolliert.
5. Beide Versuche müssen dieselben vereinbarten Akzeptanzkriterien erfüllen.
6. Beide Versuche führen dieselben Baseline- und Abschlussprüfungen aus.
7. Zeit, Rückfragen, Korrekturen und Fehlversuche werden auch dann notiert, wenn sie ungünstig wirken.

Wenn die beiden Versuche nicht denselben Feature-Schnitt implementieren, wird dies als Einschränkung des Vergleichs im Paper ausdrücklich genannt.

## Rollen

### Benutzer

Der Benutzer entscheidet Produktumfang, Datenschutz, gewünschtes Verhalten und akzeptable Trade-offs. Er erklärt zentrale Entscheidungen in eigenen Worten und bewertet nach jeder Phase Verständlichkeit, Kontrolle und Aufwand.

Der Benutzer prüft insbesondere:

- ob Fragen verständlich und relevant waren,
- ob vorgeschlagene Entscheidungen seinem Ziel entsprechen,
- ob das sichtbare Verhalten der Anwendung stimmt,
- welche Erklärung oder Entscheidung er selbst neu verstanden hat.

### Agent

Der Agent folgt der aktiven Skill-Suite, untersucht Code und Dokumentation, implementiert den vereinbarten Umfang und protokolliert Befehle, Fehler, Korrekturen und Review-Funde.

Vom Agenten formulierte Reflexionen werden als Entwurf gekennzeichnet. Persönliche Lernergebnisse werden vom Benutzer bestätigt oder in eigenen Worten ergänzt.

## Gemeinsame Baseline

Vor jeder Implementierung werden mindestens folgende Prüfungen ausgeführt und ihre Ergebnisse notiert:

```bash
dotnet build backend/StudyOrganizer.sln
dotnet test backend/StudyOrganizer.sln

cd frontend
pnpm type-check
pnpm lint
pnpm exec vitest run
pnpm build
```

Ein bereits vor dem Experiment vorhandener Fehler wird als Baseline-Fehler dokumentiert und nicht der Skill-Suite zugerechnet.

## Matt-Versuch

### M0 — Repository-Setup

`setup-matt-pocock-skills` konfiguriert Issue-Tracker, Domain-Dokumentation und Triage-Vokabular. Der aktive Skill-Commit und der gemeinsame Ausgangscommit sind dokumentiert.

**Abgeschlossen, wenn:** Die von Matts Skills benötigten Repository-Dokumente vorhanden sind und ein frischer Agent den konfigurierten Issue-Tracker sowie die Domain-Dokumentation finden kann.

### M1 — Wayfinder

Wayfinder benennt das Ziel des Moodle-Vorhabens, erstellt eine Entscheidungslandkarte und trennt sofort beantwortbare Fragen von noch unscharfer Arbeit. Wayfinder produziert Entscheidungen, keinen Feature-Code.

**Abgeschlossen, wenn:** Das Ziel, die aktuelle Entscheidungsfront, blockierende Entscheidungen, noch unscharfe Bereiche und der ausgeschlossene Umfang sichtbar dokumentiert sind.

### M2 — Klärung und Spezifikation

`grill-with-docs` klärt den ersten implementierbaren vertikalen Schnitt. `to-spec` verdichtet die bestätigten Entscheidungen zu einer Spezifikation.

**Abgeschlossen, wenn:** Der Benutzer die Spezifikation verstanden und bestätigt hat, alle Akzeptanzkriterien beobachtbares Verhalten beschreiben und offene Architekturentscheidungen entweder gelöst oder ausdrücklich ausgeschlossen sind.

### M3 — Tickets

`to-tickets` zerlegt die Spezifikation in vertikale, einzeln überprüfbare Schnitte mit expliziten Abhängigkeiten.

**Abgeschlossen, wenn:** Jedes Ticket einen eigenständig prüfbaren Nutzen besitzt, in einen frischen Agentenkontext passt und seine Blocker benennt.

### M4 — Implementierung

`implement` arbeitet die freigegebenen Tickets mit TDD ab. Typprüfung und fokussierte Tests laufen regelmäßig; die vollständigen Prüfungen laufen am Ende.

**Abgeschlossen, wenn:** Alle vereinbarten Akzeptanzkriterien nachweisbar erfüllt sind und keine ungeklärten Testfehler verbleiben.

### M5 — Review und Reflexion

`code-review` prüft Standards und Spezifikation getrennt. Der Benutzer bewertet den vollständigen Matt-Ablauf.

**Abgeschlossen, wenn:** Review-Funde bearbeitet oder begründet akzeptiert, Abschlussprüfungen protokolliert und alle Bewertungsfelder ausgefüllt sind.

## Superpowers-Versuch

Vor diesem Versuch wird eine feste Superpowers-Version im zentralen Skills-Repository veröffentlicht und im Projektbranch festgeschrieben. Matt-Skills sind in diesem Versuch nicht aktiv.

### S0 — Isolation und Baseline

Ein separater Branch oder Worktree startet vom dokumentierten gemeinsamen Ausgangscommit. Skill-Version und Baseline-Prüfungen werden protokolliert.

### S1 — Brainstorming

Superpowers `brainstorming` klärt das Feature und präsentiert den Entwurf zur Bestätigung.

### S2 — Arbeitsumgebung und Plan

`using-git-worktrees` isoliert die Arbeit. `writing-plans` erzeugt den ausführbaren Implementierungsplan.

### S3 — Implementierung und TDD

`subagent-driven-development` oder `executing-plans` führt den Plan aus. `test-driven-development` erzwingt Red-Green-Refactor.

### S4 — Review und Abschluss

`requesting-code-review` prüft die Arbeit. `finishing-a-development-branch` führt die Abschlussprüfungen aus und präsentiert die Integrationsoptionen.

### S5 — Reflexion

Der Benutzer bewertet Superpowers mit denselben Kriterien wie den Matt-Versuch.

**Der Superpowers-Versuch ist abgeschlossen, wenn:** dieselben Akzeptanzkriterien geprüft, alle Abschlussprüfungen protokolliert und alle Bewertungsfelder ausgefüllt sind.

## Objektive Messwerte

Für jeden Versuch werden folgende Werte erfasst:

| Messwert | Matt | Superpowers |
| --- | ---: | ---: |
| Agentensitzungen | Nicht zuverlässig erfasst |  |
| Rückfragen an den Benutzer | Neun dokumentierte Entscheidungs-Grillings; Einzelfragen nicht zuverlässig summiert |  |
| Vom Benutzer korrigierte Annahmen | Nicht zuverlässig erfasst |  |
| Planungs- und Entscheidungsdokumente | 11: Architekturnotiz, `CONTEXT.md`, 7 ADRs, Protokoll und Beobachtungslog |  |
| Implementierungstickets beziehungsweise Planaufgaben | 7: sechs Feature-Tickets und ein Deployment-Follow-up |  |
| Fehlgeschlagene oder wiederholte Implementierungsversuche | 2 wesentliche Korrekturschleifen: Produktionsmigration und fehlender Cleanup |  |
| Neu angelegte automatisierte Tests | Nettozahl nicht zuverlässig erfasst; Abschlussstand 225 Backend-Tests plus Frontend und Playwright |  |
| Review-Funde nach Schweregrad | Erstprüfung: Standards 2 klare Abweichungen und 4 Ermessensfunde, Spezifikation 1 P1-Fund; Re-Review: 2 P2-Standardsfunde |  |
| Nicht erfüllte Akzeptanzkriterien beim ersten Review | 2 Matrixzeilen: Cleanup-Frist und Cleanup/Reaktivierungs-Rennen |  |
| Gesamte verstrichene Arbeitszeit | Nicht zuverlässig erfasst |  |
| Manuelle Arbeitszeit des Benutzers | Nicht zuverlässig erfasst |  |
| Geänderte Dateien | 139 gegenüber `e7d8b5e` (einschließlich generierter Migrationen und Dokumentation) |  |
| Hinzugefügte und entfernte Codezeilen | +18.843 / −219 gegenüber `e7d8b5e` |  |

Codezeilen und Dateizahl beschreiben nur den Umfang. Sie sind kein eigenständiges Qualitätsmaß.

## Subjektive Bewertung

Der Benutzer vergibt nach jedem Versuch Werte von 1 bis 5:

| Kriterium | Bedeutung | Matt | Superpowers |
| --- | --- | ---: | ---: |
| Verständlichkeit | Ich konnte den Prozess und die Entscheidungen nachvollziehen. |  |  |
| Kontrolle | Ich konnte wichtige Entscheidungen selbst treffen. |  |  |
| Lerngewinn | Ich verstehe Architektur und Implementierung danach besser. |  |  |
| Angemessener Aufwand | Der Prozess war für die Aufgabe weder zu leicht noch zu schwer. |  |  |
| Vertrauen | Tests und Nachweise geben mir Vertrauen in das Ergebnis. |  |  |
| Wiederaufnahme | Ein frischer Agent konnte die Arbeit leicht fortsetzen. |  |  |
| Anpassbarkeit | Der Workflow ließ sich sinnvoll an das Projekt anpassen. |  |  |

Bewertungsskala:

- 1 — sehr schlecht
- 2 — eher schlecht
- 3 — gemischt
- 4 — gut
- 5 — sehr gut

Zu jeder Bewertung wird mindestens ein konkretes Beispiel notiert.

## Beobachtungslog pro Phase

Nach jeder Phase wird folgender Block ausgefüllt:

```markdown
### <Suite und Phase>

- Beginn/Ende:
- Verwendete Skills:
- Erzeugte Artefakte:
- Gestellte Rückfragen:
- Wichtige Entscheidungen:
- Fehlversuche oder Blocker:
- Korrekturen durch den Benutzer:
- Besonders hilfreich:
- Unnötig oder zu aufwendig:
- Vom Skill übersehen:
- Was ich selbst gelernt habe:
- Nachweis: Commit, Issue, Testausgabe oder Dateipfad
```

## Technische Qualitätsprüfung

Am Ende jedes Versuchs werden zusätzlich geprüft:

- Akzeptanzkriterien gegen das sichtbare Verhalten,
- Domain-Regeln und Deduplizierung,
- idempotente wiederholte Scans,
- Verhalten bei Fehlern und ungültigen externen Antworten,
- Schutz vor doppelter Verarbeitung desselben Kurses,
- keine vertraulichen Daten in Code, Logs oder Test-Fixtures,
- Backend- und Frontend-Gesamttests,
- Build, Typprüfung und Linting.

## Einschränkungen des Experiments

- Die Skill-Suites werden nacheinander verwendet; Lernfortschritt aus dem ersten Versuch kann den zweiten beeinflussen.
- Modell, Agentenumgebung und verfügbare Werkzeuge können das Ergebnis beeinflussen.
- Subjektive Bewertungen stammen von einer Person.
- Ein Mock-Moodle beweist die eigene Architektur, aber noch keine Kompatibilität mit einer realen Universität.
- Unterschiedliche Feature-Schnitte verringern die direkte Vergleichbarkeit.

Diese Einschränkungen werden im Paper berücksichtigt und nicht als Ergebnis der Skill-Suites dargestellt.

## Freigabe vor dem Start

Der Matt-Versuch beginnt erst, wenn folgende Punkte bestätigt sind:

- [x] Neutrale Feature-Beschreibung und Architektur-Arbeitsnotiz sind vorhanden.
- [x] Gemeinsamer Ausgangscommit ist festgeschrieben.
- [x] Matt-Version ist festgeschrieben.
- [x] Baseline-Prüfungen wurden ausgeführt und protokolliert.
- [x] Beobachtungslog für Matt ist angelegt.
- [x] Der Benutzer versteht, dass Wayfinder Entscheidungen vorbereitet und noch keinen Feature-Code schreibt.

Der Superpowers-Versuch beginnt später mit derselben Checkliste und einer festgeschriebenen Superpowers-Version.
