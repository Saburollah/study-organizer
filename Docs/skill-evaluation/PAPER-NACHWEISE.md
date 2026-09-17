# Nachweise zur Projektstudie

Dieses Dokument bündelt die für das Paper relevanten Belege. Es ist eine
redaktionelle Zusammenfassung, kein neues Testprotokoll. Die technischen
Abschlusszahlen bleiben unverändert; die persönliche Bewertung wurde nach
erneuter Klärung der Kriterien durch den Benutzer überarbeitet.

Das kompakte Paket enthält das Paper, den Anhang, das vollständige
Versuchsprotokoll und diese Nachweise. Quellcode, Erzeugungsskripte, doppelte
Diagrammdateien und vollständige Arbeitslogs sind bewusst nicht enthalten.
Die vier Abbildungen sind bereits im Paper eingebettet.

## Technische Abschlussnachweise

| Nachweis | Matt | Superpowers |
| --- | --- | --- |
| Backendtests | 225 erfolgreich: 111 Domain, 63 API, 51 Infrastructure. | 195 erfolgreich. |
| Frontendtests und Prüfungen | 94 Vitest-Tests in 20 Testdateien; Type-Check, Lint und Build erfolgreich. | 97 Vitest-Tests sowie Type-Check, Lint und Build erfolgreich. |
| Sichtbarer Ablauf | Ein dokumentierter Playwright-End-to-End-Test. | Vom Benutzer bestätigter manueller Browser-Walkthrough. |
| Relevante Korrektur | Fehlende Produktionsmigration sowie Cleanup und dessen Zusammenspiel mit Reaktivierung wurden nachgebessert. | Lokale Startkonfiguration und im Review gefundene Implementierungs- beziehungsweise Testlücken wurden korrigiert. |

Grundlage: Q2, Q4 und Q6; der Golden-Path und die Vergleichsgrenzen sind auch in
Q7 zusammengeführt. Unterschiedliche Funktionsumfänge und Testbestände erlauben
keine Rangliste anhand der Testanzahl. Für die Paper-Überarbeitung wurden keine
neuen Produkttests ausgeführt.

Das historische Matt-Protokoll bestätigte den erfolgreichen Vitest-Lauf, erfasste
aber keine Anzahl. Die 94 Tests in 20 Dateien wurden nachträglich mit
`vitest list` am gegenüber dem Matt-Abschlusscommit unveränderten Frontendstand
gezählt und als rekonstruierter Abschlussstand ausgewiesen.

## Git-Fakten und Zählregeln

Die Werte wurden direkt aus den festgeschriebenen Git-Ständen erneut ermittelt,
nicht aus älteren Zwischenständen der Logs übernommen. Die beiden Diffs sind
`e7d8b5e..ab8249c` und `e7d8b5e..a8801ff`.

| Messwert | Matt | Superpowers |
| --- | --- | --- |
| Commits ohne Merge-Commits | 32 | 28 |
| Merge-Commits, separat | 8 | 0 |
| Alle geänderten Dateien | 139 | 92 |
| Handgeschriebene Produktdateien | 71 | 50 |
| Produktcode hinzugefügt / entfernt | +6.555 / -121 Zeilen | +3.240 / -70 Zeilen |

**Reproduzierbare Zählung:** `git rev-list --count --no-merges <Bereich>`
zählt erreichbare Nicht-Merge-Commits; `git diff --shortstat <Bereich>` liefert
alle geänderten Dateien. Die Produktcode-Zeile summiert `git diff --numstat`
für `backend/src/` ohne `Infrastructure/Persistence/Migrations/` sowie
`frontend/src/` ohne `__tests__` und `.spec.`-Dateien. Sie umfasst Änderungen
zwischen den Endständen, nicht kumulierte Bearbeitungsschritte, gesamte LOC
oder ausschließlich neues Feature-Verhalten. Tests, Dokumentation, Build- und
Konfigurationsdateien sowie generierte EF-Migrationen gehören nicht zu dieser
Produktcode-Zeile. Die Gesamtdateizahl umfasst dagegen alle geänderten Dateien.

Die Git-Historie zeigt bei Matt den Entscheidungsbranch `experiment/matt`,
anschließende Feature-Branches und mehrere PR-Merges. Superpowers wurde auf
dem isolierten Branch `experiment/superpowers` mit aufeinanderfolgenden
Task-Commits umgesetzt. Keine dieser Strategien ist allein ein Qualitätsbeleg.
Bearbeitungszeit, Tokenverbrauch und identische historische Modellkonfigurationen
wurden nicht für beide Versuche zuverlässig dokumentiert; daraus werden keine
Effizienzvergleiche abgeleitet.

## Ausführungsumgebung

Beide Implementierungsversuche wurden mit Codex als Agenten- und
Ausführungsumgebung durchgeführt. Diese Angabe beruht auf dem Arbeitskontext
des Autors, nicht auf einer vollständig archivierten Konfigurationshistorie.
Die historischen Modellversionen, Bearbeitungszeiten und Tokenverbräuche sind
nicht für beide Versuche zuverlässig dokumentiert; daraus wird kein
Leistungsvergleich abgeleitet. Die weiter unten genannten Modelle betreffen
ausschließlich die ergänzende Codeprüfung.

## Ergänzende statische Codeprüfung

Die ergänzende Prüfung verglich die Produktstände `ab8249c` (Matt) und
`a8801ff` (Superpowers) mit dem gemeinsamen Start `e7d8b5e`. Standards und
Spezifikation wurden getrennt durch KI-Agenten geprüft; ein zweites Modell
öffnete die Kandidatenfundstellen erneut und bestätigte, korrigierte oder
verwarf die Aussagen. Die abgeschlossenen Erstprüfungen nutzten
`gpt-5.6-luna`, die Gegenprüfung `gpt-5.6-terra`. Diese Modellangaben gelten
nur für den ergänzenden Review, nicht für die historischen Implementierungen.
Es wurden weder Produkttests erneut ausgeführt noch Produktfehler behoben.
Die statische Gegenprüfung ist eine zusätzliche Plausibilitätskontrolle,
kein unabhängiger experimenteller Beweis und keine vollständige Fehlerfreiheit.

| Befund | Code- und Testfundstellen | Anforderungsbezug |
| --- | --- | --- |
| Matt: fehlende Herkunftsanzeige in der Aufgabenkarte | `ab8249c`, `frontend/src/views/tasks/StudyTasksView.vue`, Zeilen 346-421; API-Metadaten in `backend/src/Application/Tasks/StudyTaskResult.cs`, Zeilen 6-30; Test in `frontend/src/views/tasks/__tests__/StudyTasksView.spec.ts`, Zeilen 304-344 prüft Erhalt, nicht Quellenanzeige. | Gemeinsamer, nachträglich rekonstruierter FR-07. Issue #77 fordert die Quellenanzeige nicht ausdrücklich; daher kein behaupteter Verstoß gegen dieses Ticket. |
| Superpowers: veralteter Scanstatus | `a8801ff`, `frontend/src/views/externalCourses/MoodleCoursesView.vue`, Zeilen 83-102 und 162-218; zugehöriger Test, Zeilen 126-161 prüft Zähler, nicht den aktualisierten Kartenstatus. | Bestätigtes Design, Zeilen 243-245 und 257-265: Scanantwort und Kurskarte zeigen den Scanstatus. |
| Superpowers: nicht passende Übersetzungsschlüssel | `a8801ff`, `backend/src/Domain/ExternalCourses/ExternalCourseEnums.cs`, Zeilen 16-21; `ExternalCourseQueryHandler.cs`, Zeilen 81-93; View, Zeilen 237-239; DE/EN-Locales, jeweils Zeilen 145-149. Backend liefert `NotAnAssignment` und `MissingStructuredDeadline`, Locales definieren andere Namen. | Bestätigtes Design, Zeile 276: neue sichtbare Texte in Deutsch und Englisch. |

[Matt-Aufgabenansicht am Prüfstand](https://github.com/Saburollah/study-organizer/blob/ab8249cf6345dd765714bbf87f649d55c0bcefe7/frontend/src/views/tasks/StudyTasksView.vue#L346) ·
[Superpowers-Kursansicht am Prüfstand](https://github.com/Saburollah/study-organizer/blob/a8801ff3727a24864bc198d53305a1938c4fdb41/frontend/src/views/externalCourses/MoodleCoursesView.vue#L83) ·
[Superpowers-Prüfgründe am Prüfstand](https://github.com/Saburollah/study-organizer/blob/a8801ff3727a24864bc198d53305a1938c4fdb41/backend/src/Domain/ExternalCourses/ExternalCourseEnums.cs#L16)

**Weitere Reviewgrenzen.** Superpowers demonstriert im festgeschriebenen Mock
keinen expliziten PDF-Fall; das ist eine Nachweislücke zum gemeinsamen FR-05,
nicht ein Beweis, dass das Modell keine PDF-Ressource darstellen kann, und kein
Verstoß gegen die eigene engere Fixture-Spezifikation. Die PostgreSQL- und
SQLite-Nachweise zu NFR-03 sind nicht identisch und erlauben keine direkte
Qualitätsrangliste.

**Standards und Wartbarkeit, getrennt von Produktabweichungen.** Wiederholte
SourceUpdate-Logik bei Matt sowie breite Orchestrierungs-Handler bei beiden
Varianten sind Refactoring-Möglichkeiten, keine bestätigten Funktionsfehler.
Superpowers speichert unerwartete Scanfehler sicher, wirft die Ausnahme aber
erneut; ein API-Test und eine definierte Ausnahmebehandlung wären sinnvoll.
Ein öffentliches Datenleck wurde nicht nachgewiesen. Die zunächst vermutete
Matt-URL-Autorisierungslücke wurde ebenfalls nicht als belegter Fehler bestätigt.

## Bestätigte persönliche Bewertung

Die Skala reicht von 1 (sehr schlecht) bis 5 (sehr gut), mit 3 als gemischtem
Urteil. Die aktuellen Werte stammen aus der abschließenden Besprechung mit dem
Benutzer. Die Begründungen stehen im Paper; Herkunft und methodische Grenzen
erläutert der Anhang. Q2 und Q4 enthalten weiterhin die ursprünglichen Urteile.

| Kriterium | Matt | Superpowers |
| --- | --- | --- |
| Verständlichkeit | 4 | 5 |
| Kontrolle | 4 | 4 |
| Lerngewinn | 5 | 4 |
| Angemessener Aufwand | 4 | 3 |
| Vertrauen | 5 | 4 |
| Wiederaufnahme | 4 | 4 |
| Anpassbarkeit | 4 | 4 |

Überarbeitet wurden drei Kriterien: Vertrauen aufgrund hilfreicher Rückfragen
beziehungsweise des Praxistests; Anpassbarkeit anhand der Umsetzbarkeit eigener
Vorgaben; Aufwand anhand hilfreicher Arbeit und erlebter Wartezeiten. Die übrigen
vier Bewertungen blieben gleich. Die historischen Logs wurden nicht umgeschrieben.

## Anforderungen und Abbildungen

Das Paper enthält jetzt alle **FR-01 bis FR-07** und **NFR-01 bis NFR-07**.
Die ausführlichen Abnahmeszenarien, Variantenregeln und Zuordnungen zu den
Originalquellen stehen in Abschnitt B des Anhangs. Die Matrix ist eine
Zusammenfassung historischer Anforderungen, kein neuer Abnahmelauf.

| Abbildung | Grundlage | Aussage und Grenze |
| --- | --- | --- |
| 1 - Gemeinsamer Scan | Domänenmodell und Design: Q2, Q3, Q5. | Ein Abruf kann berechtigte Abonnenten getrennt versorgen; die drei Personen sind ein Beispiel. |
| 2 - Versuchsaufbau | Gemeinsamer Start, Versionen und getrennte Ausführung: Q1; abweichende Umfänge: Q2, Q4, Q5. | Gleiche Codebasis, aber keine identischen Versuchsbedingungen und kein Produktmerge. |
| 3 - Arbeitsweisen | Beobachtete Klärungs-, Planungs- und Reviewabläufe: Q2, Q4, Q6. | Zeigt Schwerpunkte der Versuche; beide Suiten nutzen Planung, Tests und Reviews. |
| 4 - Bewertungsprofil | Die 14 bestätigten Punkte der vorstehenden Tabelle. | Vergleich der persönlichen Einschätzung, keine objektive Qualitäts- oder Leistungsmessung. |

Der Plot und die Bewertungstabellen verwenden dieselben bestätigten Werte:
Matt ist bei Lerngewinn, Aufwand und Vertrauen höher bewertet, Superpowers bei
Verständlichkeit. Kontrolle, Wiederaufnahme und Anpassbarkeit sind gleich.
Die Skala reicht vollständig von 1 bis 5; es gibt keine Glättung, Gewichtung
oder Verbindung zwischen unterschiedlichen Kriterien. Die im Fazit formulierte
Hybridhypothese stützt sich zusätzlich auf die beobachteten Stärken und Grenzen,
nicht auf einen rechnerischen Gesamtsieger. Ob die Kombination beide Vorteile
ohne doppelten Prozessaufwand erhält, bleibt Gegenstand eines Folgeversuchs.

## Quellenübersicht

Die Kennungen Q1 bis Q7 entsprechen dem Anhang. Q1 ist vollständig im Paket
enthalten. Die weiteren Originale bleiben im Projektarchiv; die Links führen
in der ZIP-Fassung auf festgeschriebene GitHub-Stände. Für deren vollständige
Lektüre können Internetzugang und Repository-Berechtigung erforderlich sein.
Die Links wurden aus den lokal geprüften Git-Ständen abgeleitet; die öffentliche
Erreichbarkeit wird nicht vorausgesetzt.

## Q1

**Versuchsprotokoll.** Dokumentiert Forschungsziel, Ausgangsbasis, Rollen,
Versionen, geplanten Ablauf und Bewertungsskala. Die ursprünglich geforderte
Umfangsgleichheit wurde nicht vollständig erreicht; der Anhang grenzt dies ab.

[Vollständiges Versuchsprotokoll](experiment-protocol.md)

## Q2

**Matt-Beobachtungsprotokoll.** Belegt die Entscheidungsrunden, Review-Funde,
Korrekturen, Abschlussprüfungen und sieben Benutzerbewertungen. Besonders
relevant sind „Review und Korrekturen“ und „Matt-Bewertung“: Der
Spezifikationsreview fand trotz geschlossenem Haupt-Issue zwei fehlende
Cleanup-Kriterien. Der finale unabhängige Spezifikations-Re-Review fiel wegen
eines Nutzungslimits aus; stattdessen erfolgte ein eigener Matrixabgleich.

[Original: Matt-Beobachtungsprotokoll](references/matt/Docs/skill-evaluation/matt-observations.md)

## Q3

**Matt-Domänenmodell und Protokollstand.** Belegen die Trennung von externem
Kurs, Abonnement, Kursinhalt und persönlicher Aufgabe sowie die geplanten
Vergleichsbedingungen. Matt umfasst einen größeren Lebenszyklus mit
asynchronen Scans, Historie, Reaktivierung und Cleanup. Dies begrenzt den
direkten Vergleich mit Superpowers.

[Original: Matt-Domänenmodell](references/matt/CONTEXT.md) ·
[Original: Matt-Protokollstand](references/matt/Docs/skill-evaluation/experiment-protocol.md)

## Q4

**Superpowers-Beobachtungsprotokoll.** Belegt Brainstorming, Plan, TDD,
Implementierung und Benutzerreflexion. Die Abschnitte S3 bis S5 dokumentieren
die Abschlusszahlen, den manuellen Walkthrough und die sieben Bewertungen.
Nach Task 6 wurde auf Wunsch des Benutzers von Subagenten auf Inline-Ausführung
umgestellt. Lokale Startprobleme beeinflussten das ursprüngliche Urteil zur
Anpassbarkeit. Die aktuelle Neubewertung unterscheidet diese Startprobleme von
der Anpassung der Arbeitsweise an eigene Vorgaben.

[Original: Superpowers-Beobachtungsprotokoll](references/superpowers/Docs/skill-evaluation/superpowers-observations.md)

## Q5

**Bestätigtes Superpowers-Design.** Belegt den synchronen manuellen Ablauf,
Mock-Quelle, Abnahmekriterien und Vertrauensregeln. Automatisch entstehen
Aufgaben nur für Assignments mit strukturierter Frist; andere Inhalte erfordern
Prüfung. Reale Moodle-Zugänge, Scheduler und vollständiger
Cleanup-/Reaktivierungslebenszyklus gehören nicht zu diesem Schnitt.

[Original: Superpowers-Design](references/superpowers/Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md)

## Q6

**Superpowers-Aktivitätslog.** Ergänzt Q4 um Ausführungsschritte und technische
Abschlussnachweise. Relevant sind Task 12, Abschlussreview und S5. Der reale
Start und Browser-Walkthrough deckten Konfigurationsprobleme auf, die vorherige
grüne Tests allein nicht ausgeschlossen hatten.

[Original: Superpowers-Aktivitätslog](references/superpowers/Docs/skill-evaluation/agent-activity-log.md)

## Q7

**Faktenvergleich.** Führt die historischen Befunde zusammen und trennt Fakten,
Benutzerurteile und Agenteninterpretation. Er dokumentiert unterschiedliche
Feature-Umfänge und die Grenzen von Testzahlen, Git-Messwerten und Bewertungen.
Als sekundäre Auswertung ergänzt er die Originalprotokolle, ersetzt sie aber
nicht.

[Original: Faktenvergleich](references/comparison/Docs/skill-evaluation/matt-vs-superpowers-comparison.md)
