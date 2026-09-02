# Anhang: Methodik, Anforderungen und Nachweise

Dieser Anhang ergänzt die kompakte Projektstudie. Er enthält Details für die fachliche Nachprüfung; das Hauptpapier konzentriert sich auf Problem, Entscheidungen, Ergebnisse und Handlungsempfehlung.

## A. Einordnung des Vergleichs

Die Versuche starteten von demselben Produktcommit, liefen aber nacheinander und in unterschiedlichen Arbeitsumgebungen. Die später bestätigten Funktionsumfänge wichen voneinander ab. Die ursprüngliche Protokollforderung gleicher Akzeptanzkriterien wurde daher nicht vollständig erreicht. Dies wird als Einschränkung behandelt, nicht nachträglich vereinheitlicht.

Die hier zusammengefassten FR/NFR sind eine nachträgliche Rekonstruktion aus historischen Quellen. Sie ersetzen weder die bestätigten Variantenregeln noch ein neu ausgeführtes Abnahmeprotokoll. Es wurden für die redaktionelle Überarbeitung keine Produkttests wiederholt.

## B. Vollständige Anforderungen

### B.1 Funktionale Anforderungen (FR)

Die folgende Matrix rekonstruiert den gemeinsamen Vergleichskern aus Protokoll, Logs und bestätigtem Design. Sie ist **keine nachträglich als vorab vereinbart ausgegebene Spezifikation**. Jede FR beschreibt beobachtbares Verhalten und ein überprüfbares Abnahmeszenario. Variantenregeln stehen anschließend separat.

| ID | Anforderung | Abnahmeszenario |
| --- | --- | --- |
| FR-01 | Ein angemeldeter Benutzer muss einen unterstützten Mock-Kurs mit einem persönlichen Abonnement verbinden können. | Gültiger Kurslink erzeugt eine Zuordnung zum eigenen Modul; unbekannter Link wird ohne fachliche Teilanlage abgelehnt. |
| FR-02 | Bekannte Links auf denselben Kurs müssen auf dieselbe externe Kursidentität abgebildet werden. | Zwei Alias-Links ergeben genau einen gemeinsamen Kurs; erneute Registrierung desselben Benutzers erzeugt kein zweites Abonnement. |
| FR-03 | Ein berechtigter Benutzer muss einen manuellen Scan auslösen können. | Ein aufgabenfähiger Inhalt erzeugt bei drei aktiven Abonnenten je eine persönliche Aufgabe; der gemeinsame Scan ruft die Quelle nur einmal ab. |
| FR-04 | Neue, geänderte und unveränderte Inhalte müssen anhand stabiler externer Schlüssel unterschieden werden. | Unveränderter Wiederholungsscan erzeugt keine zusätzliche Aufgabe; Umbenennung erhält die Inhaltsidentität. |
| FR-05 | PDF- und Nicht-PDF-Inhalte müssen innerhalb des normalisierten Quellenmodells berücksichtigt werden. | Fixtures decken unterschiedliche Inhaltsarten ab; die Dateiendung allein entscheidet nicht über die Aufgabenerzeugung. |
| FR-06 | Ein Scanfehler muss sichtbar gemeldet werden, ohne den letzten erfolgreichen Kurszustand durch Fehlerdaten zu ersetzen. | Timeout oder ungültige Antwort ergibt einen sicheren Fehlerzustand; bisherige Inhalte und persönliche Aufgaben bleiben erhalten. |
| FR-07 | Registrierung, Scanergebnis und persönliche Aufgaben müssen über die Weboberfläche nachvollziehbar sein. | Der Benutzer kann den Ablauf vom Kurslink bis zur persönlichen Aufgabe durchlaufen und deren externe Herkunft erkennen. |

**Verbindliche Varianten statt scheinbar identischer Anforderungen:**

| Fachliche Entscheidung | Matt | Superpowers |
| --- | --- | --- |
| Modulzuordnung | Vorhandenes persönliches Modul auswählen. | Persönliches Modul automatisch anlegen. |
| Aufgabenerzeugung | Importierte Aufgaben dürfen ohne Frist existieren. | Automatisch nur Assignment plus strukturierte Frist; sonst „Prüfung erforderlich“. |
| Änderungen | Quelldaten überschreiben persönliche Planungsfelder nicht; getrennte Source Updates. | Offene quellengesteuerte Aufgaben werden synchronisiert; erledigte bleiben unverändert. |
| Scan und Lebenszyklus | Asynchroner Scan mit Statusabfrage, Historie, Reaktivierung und Cleanup. | Synchroner manueller Scan; Abmeldung, Cleanup und Scheduler ausgeschlossen. |

Diese Unterschiede stammen aus Q2 bis Q5. Insbesondere sind tägliches Moodle-Polling, echte Zugänge, LLM-Aufrufe und automatische Benachrichtigungen **kein Ergebnis beider Versuche**.

### B.2 Nichtfunktionale Anforderungen (NFR)

NFR beschreiben Qualitätsbedingungen. Sie werden nicht mit unscharfen Wörtern wie „schnell“ oder „sicher“ bewertet, sondern mit konkreten Prüfkriterien. Die Nachweisarten beziehen sich auf dokumentierte Abschlussstände; für diese Paper-Revision wurden keine Produkttests erneut ausgeführt.

| ID / Qualitätsziel | Prüfkriterium | Dokumentierter Nachweis / Grenze |
| --- | --- | --- |
| NFR-01 Autorisierung | Ein Benutzer ohne passendes eigenes Abonnement darf Inhalte weder lesen noch scannen; fremde Kennungen geben keine persönlichen Daten preis. | Owner-Prüfungen und negative API-Tests. Q2, Q4, Q5 |
| NFR-02 Konsistenz | Ein erfolgreicher Scan übernimmt zusammengehörige Änderungen vollständig; ein Fehler darf keine halben Aufgabenbestände hinterlassen. | Transaktions-, Rollback- und Fehlerfälle. Q2, Q4 |
| NFR-03 Wiederholung und Parallelität | Identische Wiederholung erzeugt null zusätzliche Aufgaben; pro Kurs läuft höchstens ein Scan gleichzeitig. | Deduplizierungs- und Konkurrenztests; Matt mit PostgreSQL, Superpowers unter anderem mit SQLite-Testfixtures. Keine identischen Datenbanknachweise. Q2, Q4 |
| NFR-04 Testbarkeit | Zeit, Quellzustand und Fehler müssen im Test steuerbar sein, ohne einen echten laufenden Moodle-Kurs zu benötigen. | Deterministische Fixtures, injizierte Zeit und kontrollierte Fehlerszenarien. Q2, Q5 |
| NFR-05 Datenschutz | Geteilte Kursdaten und Fehlerausgaben dürfen keine persönlichen Tokens oder realen Zugangsdaten enthalten. | Fiktive Fixtures, sichere Fehlercodes, Reviews. Kein Penetrationstest und keine vollständige Sicherheitszertifizierung. Q2, Q4 |
| NFR-06 Nachvollziehbarkeit | Eine wichtige Entscheidung muss auf Regel, Implementierungsstand und Prüfbeleg zurückgeführt werden können. | Matt: Issues, ADRs, Matrix. Superpowers: Spezifikation, Plan, Task-Ledger und Logs. Q2 bis Q6 |
| NFR-07 Lokale Ausführbarkeit | Build, Typprüfung, Lint und Tests müssen erfolgreich sein; der dokumentierte Start muss zum bedienbaren Ablauf führen. | Abschlusslogs und Sichtabnahme. Anfangs gab es Umgebungsfehler; „grün“ bedeutete nicht durchgehend warnungsfrei. Q2, Q4, Q6 |

Die FR/NFR sind über diese Verweise rückverfolgbar, jedoch kein neu ausgeführtes Abnahmeprotokoll. Für Antwortzeit, Durchsatz und maximalen Speicherverbrauch wurden weder gemeinsame Grenzwerte noch Lastmessungen erhoben. Eine Performancebewertung wäre daher unbelegt.


## C. Herkunft und Grenzen der Bewertung

Die Punktwerte stammen vom Benutzer und Autor der Fallstudie. Die Ausgangsskala wurde im Versuchsprotokoll festgehalten: 1 sehr schlecht, 2 eher schlecht, 3 gemischt, 4 gut, 5 sehr gut. Kriterien waren Verständlichkeit, Kontrolle, Lerngewinn, angemessener Aufwand, Vertrauen, Wiederaufnahme und Anpassbarkeit. Der Agent unterstützte die Fragenführung und sprachliche Dokumentation. Es handelt sich nicht um eine unabhängige Fremdbewertung.

Nach erneuter Klärung der Kriterien überarbeitete der Benutzer Vertrauen,
Anpassbarkeit und angemessenen Aufwand; die übrigen vier Urteile bestätigte er.
Die im Paper verwendeten Reihen lauten **Matt 4/4/5/4/5/4/4** und
**Superpowers 5/4/4/3/4/4/4**. Die historischen Originalreihen
Matt 4/4/5/3/4/4/4 und Superpowers 5/4/4/5/5/4/3 bleiben in den Versuchslogs
erhalten. Es handelt sich um eine nachträgliche persönliche Neubewertung,
nicht um neue technische Versuchsergebnisse.

Alle Kriterien werden gleich gewichtet. Die Übernahme und rechnerische
Auswertung festgelegter Werte sind bei gleichen Eingaben reproduzierbar;
die persönlichen Urteile sind es nicht notwendigerweise. Wegen der ordinalen
Skala, einer einzelnen bewertenden Person und unterschiedlicher Feature-Umfänge
wird im Hauptpapier kein Gesamtsieger aus einem Mittelwert abgeleitet.

Die Gründe für die bestätigten Werte stehen in der Tabelle des Hauptpapers;
die Versuchslogs dokumentieren die ursprünglichen Beobachtungen und Urteile.
Es gab keine vorab definierten numerischen Schwellen pro Kriterium. Solche
Schwellen werden nicht rückwirkend erfunden. Vor einer Wiederholung wären
gemeinsame Kriterienanker und einheitliche Aufgaben sinnvoll, etwa ein
identischer Neustartauftrag zur Bewertung der Wiederaufnahme.

Anpassbarkeit bewertet nun gezielt die Umsetzung eigener Vorgaben statt
lokaler Startprobleme. Vertrauen beruht auf hilfreicher Klärung beziehungsweise
dem erfolgreichen Praxistest. Beim Aufwand stehen der Nutzen der Arbeit und
die erlebten Unterbrechungen im Vordergrund. Lerngewinn und Vertrauen sind
subjektiv und nicht mit einem Wissenstest oder einer Fehlerquote gleichzusetzen.
Eine vollständige Token- und Zeitbilanz liegt nicht vor; Wartezeiten begründen
deshalb keinen gemessenen Verbrauchsvergleich.

## D. Leseschlüssel zur Kurzfassung

Das Hauptpapier enthält alle sieben FR und sieben NFR mit denselben IDs wie
dieser Anhang. Hier bleiben die ausführlicheren Szenarien, Variantenregeln und
Nachweisgrenzen zugänglich.

- **Abbildung 1:** Die Architekturabbildung zeigt einen aufgabenfähigen Inhalt
  und drei berechtigte Abonnenten. Sie veranschaulicht das gemeinsame Prinzip,
  nicht alle Variantenregeln.
- **Abbildung 2:** Der Versuchsaufbau zeigt gleiche Codebasis, feste
  Skill-Versionen und getrennte Umsetzungen. Die Versuche liefen nacheinander;
  Umfang, Umgebung und Vorwissen waren nicht identisch. Grundlage: Q1 bis Q5.
- **Abbildung 3:** Der Workflowvergleich „Zwei Wege von der Klärung zur Abnahme“
  zeigt die beobachteten Arbeitsschwerpunkte beider Suiten. Planung, Tests und
  Reviews gehören zu beiden Abläufen; keine dieser Methoden wird ausschließlich
  einer Suite zugeschrieben. Grundlage: Q2, Q4 und Q6.
- **Abbildung 4:** Der Punktvergleich übernimmt genau die 14 bestätigten
  Bewertungen aus Abschnitt C und der Nachweisdatei. Kreis steht für Matt,
  Raute für Superpowers. Die horizontale Position zeigt den Wert von 1 bis 5;
  ein kleiner vertikaler Versatz hält gleiche Werte sichtbar. Verbindungen
  bestehen nur innerhalb eines Kriteriums, nicht als Verlauf über die Kriterien.
  Höhere Punkte bedeuten eine günstigere persönliche Einschätzung. Sie belegen
  weder objektiv bessere Software noch allgemeine Überlegenheit.

Der empfohlene kombinierte Prozess wurde nicht als zusätzlicher Versuch ausgeführt. Im Superpowers-Versuch wurden keine Matt-Skills eingesetzt.

## Quellen und mitgelieferte Nachweise

Das kompakte Quellenpaket enthält das vollständige Versuchsprotokoll und die
[für das Paper relevanten Nachweise](PAPER-NACHWEISE.md). Die weiteren Q-Quellen
werden dort zusammengefasst und mit ihren Originalständen verlinkt. Vollständige
Arbeitslogs und technische Erzeugungsdateien bleiben im Repository; sie sind
nicht Teil des Bewerbungspakets. Dort dokumentiert
`references/source-manifest.json` die unveränderten Exporte und ihre Prüfsummen.
Die FR/NFR-Zusammenstellung, Abbildungen und Erläuterungen zur Bewertungsmethodik
sind Ergänzungen der Überarbeitung, keine nachträglich veränderten Versuchslogs.

- **Q1:** [Versuchsprotokoll](experiment-protocol.md), Stand `a8801ff`. Enthält Forschungsziel, ursprüngliche Vergleichsregeln, Versionen und Bewertungsskala. Die darin erwartete Umfangsgleichheit wurde nicht erreicht.
- **Q2:** [Matt-Beobachtungsprotokoll](references/matt/Docs/skill-evaluation/matt-observations.md), Stand `ab8249c`. Maßgeblich: Entscheidungsdurchläufe, Review und Korrekturen, Bewertung durch den Benutzer.
- **Q3:** [Matt-Domänenmodell](references/matt/CONTEXT.md) und [Matt-Versuchsprotokoll](references/matt/Docs/skill-evaluation/experiment-protocol.md), Stand `ab8249c`.
- **Q4:** [Superpowers-Beobachtungslog](references/superpowers/Docs/skill-evaluation/superpowers-observations.md), Stand `a8801ff`. Maßgeblich: Baseline, Tasks 1 bis 12, S5 und Bewertungstabelle.
- **Q5:** [Bestätigtes Superpowers-Design](references/superpowers/Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md), Stand `a8801ff`. Enthält Scope, Vertrauensregeln und 15 Akzeptanzkriterien.
- **Q6:** [Superpowers-Aktivitätslog](references/superpowers/Docs/skill-evaluation/agent-activity-log.md), Stand `a8801ff`. Maßgeblich: Task 12, Abschlussreview und S5.
- **Q7:** [Faktenvergleich](references/comparison/Docs/skill-evaluation/matt-vs-superpowers-comparison.md), Stand `866e724`. Ergänzende Git-Messwerte und explizite Vergleichsgrenzen; sekundäre Auswertung, kein Ersatz für Q1 bis Q6.

[Kompakte Nachweise und Quellenübersicht](PAPER-NACHWEISE.md)
