# Nachweise zur Projektstudie

Dieses Dokument bündelt die für das Paper relevanten Belege. Es ist eine
redaktionelle Zusammenfassung, kein neues Testprotokoll. Die technischen
Abschlusszahlen bleiben unverändert; die persönliche Bewertung wurde nach
erneuter Klärung der Kriterien durch den Benutzer überarbeitet.

Das kompakte Paket enthält das sechsseitige Paper, den Anhang, das vollständige
Versuchsprotokoll und diese Nachweise. Quellcode, Erzeugungsskripte, doppelte
Diagrammdateien und vollständige Arbeitslogs sind bewusst nicht enthalten.
Die vier Abbildungen sind bereits im Paper eingebettet.

## Technische Abschlussnachweise

| Nachweis | Matt | Superpowers |
| --- | --- | --- |
| Backendtests | 225 erfolgreich: 111 Domain, 63 API, 51 Infrastructure. | 195 erfolgreich. |
| Frontend | Tests, Lint und Build als erfolgreich bestätigt; Typprüfung ohne Fehler. | 97 Tests sowie Typprüfung, Lint und Build erfolgreich. |
| Sichtbarer Ablauf | Dokumentierter Playwright-Golden-Path. | Vom Benutzer bestätigter manueller Browser-Walkthrough. |
| Relevante Korrektur | Fehlende Produktionsmigration sowie Cleanup und dessen Zusammenspiel mit Reaktivierung wurden nachgebessert. | Lokale Startkonfiguration und im Review gefundene Implementierungs- beziehungsweise Testlücken wurden korrigiert. |

Grundlage: Q2, Q4 und Q6; der Golden-Path und die Vergleichsgrenzen sind auch in
Q7 zusammengeführt. Unterschiedliche Funktionsumfänge und Testbestände erlauben
keine Rangliste anhand der Testanzahl. Für die Paper-Überarbeitung wurden keine
neuen Produkttests ausgeführt.

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
oder Verbindung zwischen unterschiedlichen Kriterien. Die kontextabhängige
Empfehlung im Fazit stützt sich zusätzlich auf die Beobachtungen und Grenzen,
nicht auf einen rechnerischen Gesamtsieger.

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
