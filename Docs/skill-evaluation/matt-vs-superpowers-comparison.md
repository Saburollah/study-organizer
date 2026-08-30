# Faktenvergleich: Matt Pocock Skills und Obras Superpowers

## Status und Zweck

Dieses Dokument ist die faktenbasierte Vergleichsgrundlage für ein späteres
deutschsprachiges Bewerbungs-Paper. Es ist **noch nicht das Paper** und spricht
keinen künstlichen Gesamtsieger aus. Der Vergleich bewertet primär die beiden
Arbeitsabläufe. Produktumfang, Testzahlen und Codemenge dienen als Kontext,
weil die Versuche trotz identischem Ausgangscommit unterschiedliche fachliche
Schnitte umgesetzt haben.

Die Aussagen sind als **beobachteter Fakt**, **persönliche Bewertung des
Benutzers** oder **Interpretation des Agents** gekennzeichnet. Die Kürzel in
eckigen Klammern verweisen auf den Nachweiskatalog am Dokumentende.

## Vergleichsmethode

### Beobachtete Fakten

- Beide Versuche gehen auf den gemeinsamen Produktstand
  `e7d8b5ecc3fe75a38655f16c4636328bb1598d57` zurück. Der abschließende
  Matt-Evidenzstand ist `ab8249cf6345dd765714bbf87f649d55c0bcefe7`, der
  abschließende Superpowers-Reflexionsstand vor diesem Vergleich ist
  `a8801ff3727a24864bc198d53305a1938c4fdb41`. Für beide liefert
  `git merge-base` exakt `e7d8b5e`. [G1]
- Der Matt-Versuch wurde über einen Dokumentationsbranch, mehrere
  Implementierungsbranches und Pull Requests aufgebaut. Der Superpowers-Versuch
  blieb im separaten Worktree auf `experiment/superpowers`. Diese
  Ausführungsformen werden nicht nachträglich vereinheitlicht. [M1] [S1] [G2]
- Mechanisch abgeleitete Git-Werte werden immer zwischen `e7d8b5e` und dem
  jeweils festgeschriebenen Abschlussstand berechnet. Fehlende Prozesswerte
  werden nicht aus Commitzahlen geschätzt. [G3]

### Persönliche Bewertungen

Die sieben Bewertungen stammen ausschließlich vom Benutzer. Ihre Beispiele
werden aus den beiden Beobachtungslogs übernommen und nicht durch eine
Agentenbewertung ersetzt. [M1] [S1]

### Interpretation des Agents

Interpretationen werden erst nach den Fakten und Benutzerwerten formuliert.
Sie beantworten die Frage, welcher Workflow unter welchen Bedingungen besser
passt. Ein höherer Mittelwert oder mehr Code gilt nicht automatisch als höhere
Qualität.

## Zentrale Vergleichsgrenze: unterschiedliche Feature-Schnitte

### Beobachteter Fakt

Beide Versuche bearbeiten denselben Problemraum – einen lokalen Moodle-Mock,
Kursregistrierung, gemeinsame externe Inhalte, persönliche Aufgaben und
Deduplizierung –, aber ihre bestätigten Detailentscheidungen unterscheiden sich
wesentlich. [M1] [S2]

| Dimension | Matt | Superpowers |
| --- | --- | --- |
| Modulzuordnung | Eine Course Subscription verwendet ein vorhandenes persönliches Study Module; pro Modul höchstens eine Subscription. | Beim ersten Abonnement wird automatisch ein persönliches Modul mit dem Kursnamen erzeugt. |
| Aufgabenmodell | Persönliche Task-Felder werden durch Source Updates nicht überschrieben; Import- und Dismissed-Zustände werden getrennt modelliert. | Nur Assignment plus strukturierte Frist erzeugt automatisch eine Aufgabe; offene Moodle-Tasks werden synchronisiert und sind außer ihrem Status nicht lokal veränderbar. |
| Scanvertrag | Servereigener, asynchroner gemeinsamer Scan Run mit `202`, Polling und persistierter Historie. | Synchroner manueller Scan über das persönliche Abonnement. |
| Lebenszyklus | `Pending`, `Active`, `Ended`, Reaktivierung, Snapshots und 30-Tage-Cleanup gehören zum Schnitt. | Abonnementlöschung, Cleanup, Scheduler und echte Moodle-Zugänge sind ausdrücklich ausgeschlossen. |
| Nachweise | Echte PostgreSQL-Integrations- und Parallelitätstests sowie ein Playwright-Golden-Path. | Domain-/API-/Infrastructure-/Vue-Tests, kontrollierter lokaler Start und ein vollständiger manueller Browser-Walkthrough. |

Matt dokumentiert diese Entscheidungen in den zehn Wayfinder-/Grilling-
Durchläufen, sieben ADRs und der Akzeptanzmatrix. Superpowers dokumentiert den
kleineren Schnitt in einer bestätigten Designspezifikation und einem Plan mit
zwölf Aufgaben. [M1] [S2] [S3]

### Interpretation des Agents

Ein direkter Produktwettbewerb wäre unfair: Matts höhere Datei-, Zeilen- und
Backend-Testzahl enthält zusätzliche fachliche und betriebliche Anforderungen.
Umgekehrt belegt Superpowers mit weniger Produktumfang nicht, dass derselbe
Matt-Schnitt günstiger hätte umgesetzt werden können. Vergleichbar sind vor
allem Fragenführung, Nachvollziehbarkeit, TDD-Disziplin, Reviewverhalten,
Steuerungsaufwand und die vom Benutzer erlebte Qualität.

## Objektive Messwerte

### Beobachtete Fakten

| Messwert | Matt | Superpowers | Vergleichbarkeit und Nachweis |
| --- | --- | --- | --- |
| Agentensitzungen | Nicht zuverlässig erfasst | 1 Hauptsitzung; zusätzlich 22 Subagent-Aufrufe | Nicht direkt vergleichbar. [M2] [S1] |
| Rückfragen | Das Protokoll nennt neun Entscheidungs-Grillings, das Beobachtungslog enthält zehn nummerierte Entscheidungsdurchläufe; Einzelfragen nicht vollständig summiert | 31 insgesamt, davon 17 Produkt-/Architekturfragen | Matts Log nennt allein für zwei Durchläufe 35 und 36 Fragen; bereits die Rundenzahl ist zwischen den Matt-Artefakten nicht einheitlich, daher kein exakter Gesamtwert. [M1] [M2] [S1] |
| Vom Benutzer korrigierte Annahmen | Nicht zuverlässig erfasst; mindestens eine korrigierte Prototyp-Antwort ist beschrieben | 1: der vermeintlich erfolgreiche Backend-Start | Nur die Superpowers-Zahl wurde systematisch geführt. [M1] [S1] |
| Planungs-/Entscheidungsdokumente | 11 nach Matt-Protokoll: Architekturnotiz, `CONTEXT.md`, sieben ADRs, Protokoll und Log; zusätzlich GitHub-Issues | 2: Designspezifikation und Implementierungsplan | Matts externe Issue-Artefakte sind in der Zahl nicht vollständig enthalten. [M2] [S1] |
| Umsetzungseinheiten | 7: sechs Feature-Tickets plus Deployment-Follow-up | 12 Planaufgaben | Die Einheiten unterscheiden sich stark in Größe und Umfang. [M2] [S1] |
| Wesentliche Korrekturschleifen | 2: fehlende Produktionsmigration und fehlender Cleanup | 2 als Implementierungsfehlversuche gezählte Fälle | Definitionen sind nicht vollständig identisch; beide Logs nennen die konkreten Fälle. [M1] [M2] [S1] |
| Neu angelegte Tests | Nettozahl nicht vollständig erhoben; final 225 Backend-Tests, erfolgreiche Frontendtests und ein Playwright-Pfad. Gegen die gemeinsame 60-Test-Backend-Baseline sind mindestens 165 Backendtests hinzugekommen. | 155 neue Tests protokolliert; final 195 Backend- und 97 Frontendtests | Matts exakte Frontend-Nettozahl fehlt, daher kein fairer Gesamtzahlvergleich. [M1] [M2] [S1] |
| Review-Funde | Erstprüfung: 2 klare Standardsabweichungen, 4 Ermessensfunde und 1 P1-Spezifikationsfund; Re-Review: 2 P2-Standardsfunde | 1 Critical, 6 Important, 4 Minor; Critical/Important und ein Minor behoben, drei Minor akzeptiert | Schweregradskalen und Reviewzeitpunkte unterscheiden sich. [M2] [S1] |
| Nicht erfüllte Kriterien beim ersten Abschlussreview | 2 Matrixzeilen: Cleanup-Frist und Cleanup-/Reaktivierungsrennen | Kein gleichartiger einzelner Erst-Review-Wert; frühere Per-Task-Reviews fanden Defekte/Testlücken, der einmalige Abschlussreview keine offenen Critical/Important-Funde | Eine numerische Gleichsetzung wäre irreführend. [M1] [S1] |
| Manuelle Benutzerzeit | Nicht zuverlässig erfasst | Nicht separat erhoben | Kein Vergleich möglich. [M2] [S1] |
| Git-Zeitfenster, keine aktive Arbeitszeit | 20.08.2026 22:11 bis 27.08.2026 14:59, mechanisch ca. 160 h 48 min | 27.08.2026 20:59 bis 30.08.2026 14:34, mechanisch ca. 65 h 35 min | Enthält Offline- und Wartezeit; nicht als Produktivität interpretieren. [G4] |
| Alle geänderten Dateien | 139 | 92 | Gegen die festgeschriebenen Abschlussstände. [G3] |
| Alle Zeilenänderungen | +18.868 / −226 | +11.204 / −86 | Enthält Dokumentation und generierte Migrationen. [G3] |
| Produktdateien `backend/` + `frontend/` | 115 | 84 | Mechanischer Scope-Indikator. [G3] |
| Produktzeilen `backend/` + `frontend/` | +17.606 / −160 | +8.368 / −79 | Matts generierte Migrationsdateien und größerer Scope erhöhen den Wert. [G3] |
| Commits | 40 insgesamt, davon 32 ohne Merge-Commits | 28, alle ohne Merge-Commit im Versuchsbranch | Ergänzender Workflow-Footprint, kein Qualitätsmaß. [G5] |

Die im Matt-Protokoll früher notierte Zeilenzahl `+18.843/−219` liegt sieben
Zeilen und 25 Ergänzungen unter dem neu berechneten Endstand. Für diese Tabelle
gilt der reproduzierbare Diff gegen den späteren, ratingshaltigen Commit
`ab8249c`. [M2] [G3]

### Interpretation des Agents

Matt erzeugte deutlich mehr dauerhafte Artefakte und Produktcode. Das passt zum
größeren Lebenszyklus- und Betriebsumfang, zeigt aber auch den vom Benutzer mit
3/5 bewerteten Aufwand. Superpowers war kompakter, benötigte jedoch 22
Subagent-Aufrufe und 33 Umgebungsfreigaben; der geringere Git-Diff bedeutet also
nicht automatisch geringeren Agentenverbrauch. [M1] [S1]

## Subjektive Bewertung des Benutzers

### Persönliche Bewertung

| Kriterium | Matt | Superpowers | Differenz aus Sicht des Benutzers |
| --- | ---: | ---: | --- |
| Verständlichkeit | 4 | 5 | Superpowers +1 |
| Kontrolle | 4 | 4 | gleich |
| Lerngewinn | 5 | 4 | Matt +1 |
| Angemessener Aufwand | 3 | 5 | Superpowers +2 |
| Vertrauen | 4 | 5 | Superpowers +1 |
| Wiederaufnahme | 4 | 4 | gleich |
| Anpassbarkeit | 4 | 3 | Matt +1 |
| **Arithmetischer Mittelwert** | **4,00** | **4,29** | nur Übersicht, kein Siegerwert |

Für Matt begründete der Benutzer den höheren Lerngewinn mit dem Weg von der
dynamischen Moodle-Struktur über Adapter und stabile Identitäten bis zu
gemeinsamem Scannen und persönlicher Projektion. Den Aufwand von 3/5 begründete
er mit neun Grillings, zahlreichen Issues und mehreren Reviewrunden. [M1]

Für Superpowers waren der kurze, saubere Ablauf, die gute Struktur und der
erfolgreiche manuelle End-to-End-Test besonders positiv. Die Anpassbarkeit fiel
mit 3/5 schwächer aus, weil das Backend zunächst nicht startete und der Benutzer
mehrere lokale Fehler selbst korrigieren musste. Die Wiederaufnahme war gut,
verbrauchte aber viel Agentenkontingent. [S1]

### Interpretation des Agents

Die Werte sprechen für unterschiedliche Stärken, nicht für einen universellen
Sieger. Matt erzeugte beim Benutzer mehr fachlich-architektonischen Lerngewinn
und wurde als anpassbarer erlebt. Superpowers war verständlicher, im Aufwand
angemessener und lieferte nach dem manuellen Walkthrough mehr subjektives
Vertrauen. Kontrolle und Wiederaufnahme wurden gleich bewertet.

## Vergleich der Arbeitsabläufe

### 1. Anforderungsklärung und Architektur

**Beobachtete Fakten:** Matt führte zehn dokumentierte Entscheidungsdurchläufe
durch und behandelte Identität, Task-Lebenszyklus, Scanfehler, Autorisierung,
Retention, Datenmodell, API und Testmatrix getrennt. Sieben ADRs und
`CONTEXT.md` hielten die Entscheidungen fest. Superpowers stellte 17
Produkt-/Architekturfragen, präsentierte fünf Designabschnitte und verdichtete
sie in eine Spezifikation. [M1] [M2] [S1] [S2]

**Interpretation des Agents:** Matt ist stärker, wenn schwer rückgängig zu
machende Domänenentscheidungen früh und dauerhaft nachvollziehbar werden
müssen. Superpowers ist stärker, wenn ein begrenzter vertikaler Schnitt schnell
zu einer zusammenhängenden, bestätigten Spezifikation werden soll. Matts Tiefe
vergrößerte den MVP; Superpowers übersah zunächst lokale Startup-, Schema- und
Cache-Grenzen.

### 2. Planung und Ausführung

**Beobachtete Fakten:** Matt zerlegte die Arbeit in sechs Feature-Tickets und
ein Deployment-Follow-up, umgesetzt über einzelne PRs. Superpowers schrieb
einen mehr als 1.500 Zeilen langen Plan mit zwölf Aufgaben. Tasks 1–6 liefen
subagentengesteuert; auf Benutzerwunsch wurden Tasks 7–12 inline ausgeführt.
[M1] [S1] [S3]

**Interpretation des Agents:** Matts vertikale Tickets passen gut zu sichtbarer
Team- und GitHub-Arbeit, verlangen aber Verwaltungsdisziplin. Der
Superpowers-Plan ist für einen frischen Agenten sehr ausführbar, kann aber selbst
zum kostenintensiven Artefakt werden. Der Wechsel auf Inline-Ausführung senkte
bei Superpowers den Review- und Subagentenaufwand, ohne den Plan aufzugeben.

### 3. TDD und technische Nachweise

**Beobachtete Fakten:** Beide Versuche dokumentieren testgetriebene Entwicklung.
Matt nutzte echte PostgreSQL-Integrations- und Parallelitätstests und erreichte
225 grüne Backendtests; Frontendtests, Lint und Build wurden vom Benutzer als
grün bestätigt. Superpowers dokumentiert die RED-/GREEN-Schritte pro Aufgabe,
195 grüne Backendtests, 97 grüne Frontendtests, Typecheck, Lint, Builds und den
manuellen E2E-Walkthrough. [M1] [S1] [S4]

**Interpretation des Agents:** Matt liefert stärkere Datenbank- und
Nebenläufigkeitsnähe. Superpowers liefert eine genauer gezählte, schichtenweite
Testchronik und eine stärkere lokale Sichtabnahme. Wegen des unterschiedlichen
Scopes darf aus 225 gegenüber 195 kein Qualitätsranking entstehen.

### 4. Review und Fehlerentdeckung

**Beobachtete Fakten:** Matts getrennte Standards- und Spezifikationsreviews
fanden Terminologieabweichungen und zwei zunächst fehlende Cleanup-Kriterien,
obwohl das Haupt-Issue schon geschlossen war. Der finale unabhängige
Spezifikationsagent fiel wegen eines Nutzungslimits aus; der Agent prüfte die
Matrix deshalb selbst. Superpowers fand in frühen Per-Task-Reviews unter
anderem eine mögliche Speicherung unsicherer Fehlertexte und eine
Kanonisierungslücke. Nach dem vom Benutzer verlangten Inline-Wechsel gab es
keine Per-Task-Reviewer mehr; ein finaler Inline-Review fand keine offenen
Critical-/Important-Probleme. [M1] [S1] [S4]

**Interpretation des Agents:** Matts Reviewtrennung ist der stärkere Mechanismus
gegen „grün, aber unvollständig“. Superpowers reagierte schneller innerhalb
kleiner TDD-Schritte, verlor durch den bewusst geänderten Ausführungsmodus aber
unabhängige Reviewbreite. In beiden Versuchen blieben Umgebungsgrenzen zunächst
außerhalb der fachlichen Reviewmatrix: bei Matt die Produktionsmigration, bei
Superpowers der versteckte Worktree samt lokalem DB-/Cache-Zustand.

### 5. Menschliche Kontrolle, Wiederaufnahme und Nutzungseffizienz

**Beobachtete Fakten:** Der Benutzer bewertete Kontrolle und Wiederaufnahme bei
beiden Suiten mit 4/5. Matt hinterließ Issues, ADRs, Glossar und PR-Historie;
Superpowers hinterließ Spezifikation, Ausführungsplan, Task-Ledger,
Aktivitätslog und Beobachtungslog. Matt beschreibt hohen Verwaltungsaufwand.
Bei Superpowers wurden hoher Agentenkontingentverbrauch, 22 Subagent-Aufrufe und
33 Umgebungsfreigaben dokumentiert. [M1] [S1]

**Interpretation des Agents:** Matt optimiert stärker für organisationsweite
Nachvollziehbarkeit; Superpowers stärker für die Übergabe an weitere Agenten.
Beide Formen können teuer werden: Matt durch menschliche Issue-/ADR-Pflege,
Superpowers durch detaillierte Pläne, Agentenwechsel und wiederholte
Verifikation.

## Gemeinsame technische Qualitätsaussagen

### Beobachtete Fakten

| Qualitätsziel | Matt-Nachweis | Superpowers-Nachweis |
| --- | --- | --- |
| Stabiler gemeinsamer Kurs und Deduplizierung | Domain-/Schema- und Scan-Orchestrierungscommits `8d2db67`, `4cd5f41`; PostgreSQL-Tests im Abschlussstand | ExternalCourse-Constraints, Registration-/Scanhandler und Tests; Commits `17f3022`, `51c621a`, `634947e` |
| Autorisierung persönlicher Zugriffe | ADR 0004, API-Commit `0a87547`, 404-Regeln und API-Tests | owner-scoped Handler/API, Commit `c4b7322`, ExternalCourse-API 17/17 |
| Parallelität und Fehlererhalt | Scan-Orchestrierung `4cd5f41`, Cleanup-Fix `1ba9ec4`, echte PostgreSQL-Rennentests | Lease-/Fehlerszenarien in `07b81a1` und `468a715`, Infrastructure final 69/69 |
| Sichtbarer End-to-End-Pfad | Playwright-Golden-Path `336a6ab`; erfolgreiche Frontendchecks `f94edb3` | Kursansicht `0fc36cc`, Provenienz `5ec8b3d`, vom Benutzer bestätigter Browser-Walkthrough |
| Betriebs-/Startgrenze | Produktionsmigrationsbundle `45644e0` nach Deployment-Lücke | Worktree-Appsettings-Fix `0e0c073`; Root-Start Health 200 und CORS 204 |
| Abschlussprüfungen | 225 Backendtests; Frontendtests, Lint und Build grün laut Benutzerbestätigung | 195 Backendtests, 97 Frontendtests, Typecheck, Lint und beide Builds grün |

Die Baseline war trotz identischem Code nicht umgebungsidentisch: Matt
dokumentierte eine vollständig grüne Backend-Baseline. Im versteckten
Superpowers-Worktree scheiterten dagegen alle 43 API-Tests an fehlender
JWT-Konfiguration, weil der Standard-FileProvider die Appsettings ausblendete.
Das wurde erst in Task 12 reproduziert und mit `0e0c073` repariert. Dieser
Unterschied darf weder Matt als Codevorteil noch Superpowers als Featurefehler
zugerechnet werden. [M1] [S1] [S4]

## Empfehlung

### Interpretation des Agents

Für einen **klar begrenzten vertikalen Schnitt** wie den vom Benutzer im
Superpowers-Versuch gewählten lokalen E2E-Ablauf ist Superpowers die passendere
Standardwahl. Der Workflow erhielt 5/5 für Verständlichkeit, angemessenen
Aufwand und Vertrauen, erzeugte die kompaktere Spezifikations-/Planstruktur und
führte den Schnitt erfolgreich bis zum manuellen Browsernachweis. Diese
Empfehlung ist kontextbezogen und kein Gesamtsieg. [S1]

Für eine **langfristige, risikoreiche Integrationsplattform** mit asynchronen
Scans, Retention, Reaktivierung, echten PostgreSQL-Rennen, Deployment und vielen
Beteiligten ist Matts Suite die passendere Wahl. Wayfinder, Grilling, ADRs und
der getrennte Spezifikationsreview machten genau solche schwer rückgängig zu
machenden Fragen sichtbar. Der Preis ist höherer Frage-, Issue-, Review- und
Dokumentationsaufwand, den der Benutzer mit 3/5 für Angemessenheit bewertete.
[M1]

Für künftige Projekte lautet die Empfehlung daher:

1. **Superpowers als Default** für begrenzte, ausführbare Produktinkremente mit
   klarer TDD-Kette und schneller lokaler Abnahme verwenden.
2. **Matt gezielt einsetzen**, wenn Datenlebenszyklus, Compliance,
   Nebenläufigkeit, betriebliche Übergänge oder organisationsweite
   Entscheidungshistorie das Hauptrisiko bilden.
3. Unabhängig von der Suite früh eine gemeinsame **Environment-Abnahme**
   ergänzen: Content-Root, Secrets, CORS, Migration History, echte Datenbank,
   Cache und Produktionsmigration. Beide Versuche fanden eine solche Lücke erst
   spät. [M1] [S1]
4. Für das spätere Paper nicht behaupten, eine Suite produziere generell
   „besseren Code“. Belegt ist nur, dass sie in diesen zwei verschieden großen
   Schnitten andere Stärken, Kosten und Fehlersuchmechanismen zeigte.

## Einschränkungen

### Beobachtete Fakten

- Die Versuche liefen nacheinander; Lerneffekte aus Matt konnten Superpowers
  beeinflussen. [P1]
- Modell, Agentenumgebung, Sandbox und verfügbare Werkzeuge sind zusätzliche
  Einflussfaktoren. [P1]
- Eine Person lieferte alle subjektiven Bewertungen. [P1]
- Matt- und Superpowers-Schnitt unterscheiden sich fachlich und technisch.
  [M1] [S2]
- Agentensitzungen, Matt-Einzelfragen, aktive Arbeitszeit, manuelle Benutzerzeit
  und Matts genaue Frontend-Netto-Testzahl wurden nicht gleichartig erfasst.
  [M2] [S1]
- Der Matt-Versuch nutzte GitHub-Issues/PRs und den normalen Repositorypfad,
  Superpowers einen versteckten Worktree und zeitweise Subagenten. [M1] [S1]
- Beide Schnitte verwenden einen Mock und beweisen keine reale Moodle-
  Kompatibilität. [P1]

### Interpretation des Agents

Die Ergebnisse sind eine belastbare Fallstudie, aber kein kontrolliertes
Benchmark mit statistischer Allgemeingültigkeit. Für das Bewerbungs-Paper
sollten konkrete Mechanismen und Beispiele im Mittelpunkt stehen, nicht der
Mittelwert von sieben Bewertungen.

## Nachweiskatalog

- **[P1] Versuchsprotokoll:**
  `Docs/skill-evaluation/experiment-protocol.md`, insbesondere „Faire
  Vergleichsbedingungen“, „Objektive Messwerte“ und „Einschränkungen“.
- **[M1] Matt-Beobachtungslog:**
  `ab8249c:Docs/skill-evaluation/matt-observations.md`; enthält zehn
  Entscheidungsdurchläufe, Implementierung, Review, Abschlussprüfungen und die
  Benutzerbewertung 4/4/5/3/4/4/4.
- **[M2] Matt-Protokollstand:**
  `ab8249c:Docs/skill-evaluation/experiment-protocol.md`, Tabellen
  „Objektive Messwerte“ und „Subjektive Bewertung“.
- **[S1] Superpowers-Beobachtungslog:**
  `Docs/skill-evaluation/superpowers-observations.md` im Commit `a8801ff`;
  enthält objektive Messwerte, Tasks 1–12, S4/S5 und die Benutzerbewertung
  5/4/4/5/5/4/3.
- **[S2] Superpowers-Design:**
  `Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md`, insbesondere
  Ziel, ausgeschlossener Umfang, Vertrauensregeln und Scanablauf; Commit
  `3dc5ff5`.
- **[S3] Superpowers-Plan:**
  `Docs/superpowers/plans/2026-08-28-moodle-end-to-end.md`; zwölf Aufgaben;
  Commit `11a8135`.
- **[S4] Superpowers-Aktivitätslog:**
  `Docs/skill-evaluation/agent-activity-log.md`, insbesondere Task 12,
  Abschlussreview und S5.
- **[G1] Gemeinsame Basis:** Ausgaben von
  `git merge-base e7d8b5e ab8249c` und
  `git merge-base e7d8b5e a8801ff`, jeweils
  `e7d8b5ecc3fe75a38655f16c4636328bb1598d57`.
- **[G2] Historie:** `git log --reverse e7d8b5e..ab8249c` und
  `git log --reverse e7d8b5e..a8801ff`; Matt endet im Ratingscommit
  `ab8249c`, Superpowers im Reflexionscommit `a8801ff`.
- **[G3] Reproduzierbare Diffmessung:** `git diff --shortstat` und
  `git diff --numstat` von `e7d8b5e` zu `ab8249c` beziehungsweise `a8801ff`,
  zusätzlich jeweils mit `-- backend frontend`. Ergebnis: alle Dateien
  Matt 139/+18.868/−226, Superpowers 92/+11.204/−86; Produktdateien Matt
  115/+17.606/−160, Superpowers 84/+8.368/−79.
- **[G4] Git-Zeitfenster:** Autorzeitpunkte `05d2609` bis `ab8249c` und
  `1c4b35a` bis `a8801ff`; keine aktive Arbeitszeitmessung.
- **[G5] Commitmessung:** `git rev-list --count` und
  `git rev-list --count --no-merges` ab `e7d8b5e`: Matt 40/32,
  Superpowers 28/28.

## Noch nicht Bestandteil dieses Dokuments

- endgültige Gliederung und Sprache des Bewerbungs-Papers;
- Literatur- oder Webrecherche;
- Merge, Push, Pull Request oder Integration einer Implementierung;
- Entscheidung, welche Produktvariante später nach `main` übernommen wird.
