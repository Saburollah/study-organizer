# Agentische Skill-Suites in der Softwareentwicklung

## Eine Fallstudie mit Matt Pocock Skills und Obras Superpowers

**Saburollah Safari | Study Organizer | Überarbeitete Fassung: 31. August 2026**

## Kurzfassung

Wie helfen Agent Skills dabei, aus einer unscharfen Feature-Idee überprüfbare Software zu entwickeln? Untersucht wurden zwei aufeinanderfolgende Entwicklungsversuche einer Moodle-nahen Kursintegration: zuerst mit Matt Pococks Skills, danach ausschließlich mit Obras Superpowers. Gemeinsame Grundlage waren ein festgeschriebener Codezustand und eine kontrollierte Mock-Quelle. Die tatsächlich umgesetzten Funktionsumfänge unterschieden sich jedoch.

Die Fallstudie verbindet versionierte Artefakte, dokumentierte Tests und Reviews mit der Bewertung eines einzelnen Benutzers. Matt machte komplexe Fachentscheidungen ausführlich nachvollziehbar; Superpowers strukturierte den kleineren End-to-End-Schnitt klar. Daraus folgt keine allgemeine Rangfolge. Die Empfehlung lautet, Prozessumfang und Reviewtiefe am Risiko auszurichten. Funktionale und nichtfunktionale Anforderungen, Bewertungsgrenzen und ein vollständiges Quellenpaket machen die Herleitung prüfbar. Das ausführliche Fazit steht bewusst vor den persönlichen Lernerfahrungen.

## 1. Problemstellung und Architekturidee

### 1.1 Vom Kurslink zur persönlichen Aufgabe

Der Study Organizer verwaltet persönliche Lernmodule und Aufgaben. Die Erweiterung soll einen externen Kurs registrieren, Inhalte wiederholt abrufen und neue Lernaufgaben für Abonnenten bereitstellen. Aus Sicht des Benutzers bedeutet dies: Kurs verbinden, Scan starten und relevante Aufgaben im eigenen Modul sehen.

Schwierig ist nicht die Anzahl der REST-Endpunkte. Entscheidend sind wechselnde Inhaltsformen, stabile Identität, Zugriffsrechte und die Verarbeitung von Änderungen. Ein anderer Titel oder Link darf keine neue Aufgabe erzeugen, wenn die externe Inhaltsidentität gleich bleibt. Drei berechtigte Abonnenten sollen gemeinsame Quelldaten nutzen, ihre persönlichen Aufgaben aber getrennt behalten.

Eine offizielle Schnittstelle könnte den Zugriff vereinfachen; sie würde diese fachlichen Regeln nicht ersetzen. Im Versuch wurde deshalb zuerst die interne Verarbeitung mit einem Mock geprüft. Eine reale Moodle-Anbindung und LLM-Erkennung wurden nicht implementiert. Aussagen über deren Zuverlässigkeit wären durch diesen Versuch nicht gedeckt. [Q1, Q2, Q5]

### 1.2 Gemeinsame Daten, persönliche Verarbeitung

![Abbildung 1: Ein gemeinsamer Scan erzeugt getrennte persönliche Aufgaben für drei berechtigte Abonnenten.](figures/01-gemeinsamer-scan.png)

*Abbildung 1. Konzeptioneller gemeinsamer Kern beider Versuche. Ein aufgabenfähiger Inhalt führt bei drei berechtigten Abonnenten zu je einer persönlichen Aufgabe. Die genaue Aufgabenfähigkeit und die Synchronisierung unterscheiden sich zwischen den Varianten. Eigene Darstellung nach Q2, Q3 und Q5; kein Nachweis realer Moodle-Kompatibilität.*

Der Adapter übersetzt die Quelle in normalisierte Inhalte. Ein vollständiges, validiertes Ergebnis wird mit dem bisherigen Zustand verglichen und atomar übernommen. Die gemeinsame Speicherung spart Abrufe; das persönliche Abonnement bleibt die Zugriffsgrenze. Wiederholungssicherheit und Datenbank-Eindeutigkeit verhindern Duplikate. Die Architektur trennt damit Quellenformat, gemeinsame Identität und persönliche Nutzung.

## 2. Versuchsaufbau und Anforderungen

### 2.1 Isolierung und tatsächliche Vergleichsbedingungen

Beide Versuche gingen vom Produktcommit `e7d8b5e` aus. Die Skills wurden aus dem zentralen Repository `Saburollah/agent-skills` über ein Git-Submodul eingebunden: Matt mit `matt-v1.0` und Skill-Commit `f6de92c`, Superpowers mit `superpowers-v6.3.0` und `a419016`. Maßgeblich ist der im Projekt gespeicherte Submodul-Commit; ein später bewegter Branch aktualisiert ein Projekt nicht von selbst. Diese Trennung ermöglicht gemeinsame Pflege bei projektspezifisch kontrollierten Updates. [Q1]

![Abbildung 2: Getrennte Versuche ab demselben Ausgangscommit und Zusammenführung ausschließlich der Nachweise.](figures/02-versuchsaufbau.png)

*Abbildung 2. Die Zweige zeigen getrennte Codehistorien, keine gleichzeitige Durchführung. Matt wurde zeitlich zuerst verwendet. Unterschiedliche Umfänge und der Lernübertrag bleiben Vergleichseinschränkungen. Eigene Darstellung nach Q1 bis Q6.*

Das Protokoll sah gleiche Produktentscheidungen und Akzeptanzkriterien vor. Diese Vorgabe wurde nicht vollständig eingehalten: Matt realisierte mehr Lebenszyklus- und Betriebsverhalten; Superpowers einen engeren Schnitt. Auch die Umgebung war nicht identisch: Im versteckten Superpowers-Worktree scheiterten bereits vor der Feature-Implementierung API-Tests am Konfigurationszugriff. Deshalb handelt es sich um eine vergleichende Fallstudie, nicht um ein kontrolliertes A/B-Experiment. [Q1, Q4]

### 2.2 Funktionale Anforderungen (FR)

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

### 2.3 Nichtfunktionale Anforderungen (NFR)

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

## 3. Beobachtete Ergebnisse

### 3.1 Matt: Entscheidungstiefe mit Verwaltungsaufwand

Wayfinder machte aus der offenen Moodle-Idee eine GitHub-Landkarte. Grilling behandelte Identität, Berechtigungen, Aufgabenlebenszyklus, Fehler und Nebenläufigkeit. Domain Modeling hielt Begriffe und sieben Architekturentscheidungen fest; ein Wegwerfprototyp erlaubte den Vergleich von drei UI-Abläufen. Das Log enthält zehn nummerierte Entscheidungsdurchläufe, während das Protokoll neun Grillings nennt. Diese Zählabweichung bleibt offen und wird nicht zu einem exakten Frageaufwand umgerechnet. [Q2, Q3]

Der getrennte Spezifikationsreview fand zwei fehlende Cleanup-Kriterien trotz bereits geschlossenem Haupt-Issue. Zusätzlich wurde eine Produktionsmigrationslücke korrigiert. Der Abschluss dokumentiert 225 erfolgreiche Backendtests sowie vom Benutzer bestätigte Frontendtests, Lint und Build. Ein automatisierter Playwright-Pfad gehörte zum Umfang. Die fehlende unabhängige finale Spezifikationsantwort wegen eines Nutzungslimits wurde durch einen agentenseitigen Selbstabgleich ersetzt, nicht als unabhängiger Review ausgegeben. [Q2]

### 3.2 Superpowers: ausführbarer Plan mit Umgebungsgrenzen

Brainstorming führte zu einem bestätigten Design; der Implementierungsplan enthielt zwölf Aufgaben. Die ersten sechs wurden subagentengesteuert bearbeitet, die restlichen auf Benutzerwunsch inline. Das Log dokumentiert 22 Subagent-Aufrufe und 33 Umgebungsfreigaben. Diese Zähler sind keine Tokenmessung und erlauben keinen direkten Kostenvergleich mit Matt. [Q4]

Am Abschluss standen 195 erfolgreiche Backendtests und 97 erfolgreiche Frontendtests, dazu Build, Typecheck und Lint. Die Zahlen beschreiben Gesamtbestände, nicht ausschließlich neu erzeugte Tests. Beim manuellen Browser-Walkthrough wurden lokale Startprobleme sichtbar. Eine zunächst zu optimistische Startdiagnose wurde korrigiert; der nachgewiesene Fix betraf das Laden von Appsettings im versteckten Worktree. PostgreSQL-Zustand und Frontend-Cache verursachten weitere lokale Schwierigkeiten. [Q4, Q6]

## 4. Bewertung: Herkunft, Begründung und Reproduzierbarkeit

### 4.1 Wer bewertete was?

Die sieben Punktewerte stammen vom Benutzer und Autor dieser Fallstudie, nicht vom Agenten oder von Testsoftware. Matt wurde am 27. August, Superpowers am 30. August 2026 bewertet. Der Agent strukturierte die Fragen und formulierte Erklärungen aus; diese Unterstützung kann die Bewertung beeinflusst haben. Die Beispiele dokumentieren Erlebnisse und Vorgänge, keine unabhängige Fremdbewertung. [Q2, Q4]

Die vorher festgelegte Skala lautete: **1 = sehr schlecht, 2 = eher schlecht, 3 = gemischt, 4 = gut, 5 = sehr gut**. Es gab damals keine kriterienspezifischen, numerischen Schwellen wie „vier Punkte bei höchstens fünf Rückfragen“. Eine solche Regel wird jetzt nicht rückwirkend erfunden.

Bewertet wurden Verständlichkeit des Prozesses, Kontrolle über Entscheidungen, eigener Lerngewinn, Angemessenheit des Aufwands, Vertrauen in die Nachweise, Wiederaufnahme und Anpassbarkeit. Die Originalwerte bleiben unverändert. Ein fehlender Wert würde als „nicht erhoben“ gelten, nicht als null oder drei.

### 4.2 Begründung je Punktwert

In der folgenden Tabelle stehen Originalpunktwert, dokumentierte Grundlage und die Grenze der Interpretation nebeneinander. Die Erläuterungen sind Paraphrasen aus Q2 und Q4, keine erfundenen Interviewzitate.

| Kriterium | Matt: Punkte und Grundlage | Superpowers: Punkte und Grundlage | Einordnung |
| --- | --- | --- | --- |
| Verständlichkeit | **4**: Wayfinder, ADRs und Akzeptanzmatrix machten Entscheidungen nachlesbar. | **5**: Der Benutzer erlebte den Ablauf als klar, sauber und leicht nachvollziehbar. | Eindruck einer Person; keine Messung von Verständnisfehlern. |
| Kontrolle | **4**: Produktentscheidungen wurden selbst getroffen; eine falsche Prototyp-Auswahl wurde korrigiert. | **4**: Architektur und Wechsel von Subagenten zu Inline-Ausführung konnten selbst bestimmt werden. | Gleiche Wertung bedeutet nicht gleich viele Eingriffe. |
| Lerngewinn | **5**: Verständnis von Adapter, Identität, gemeinsamem Scan und persönlicher Projektion. | **4**: Zusammenspiel von Backend, Datenbank und Frontend wurde klarer. | Kein Wissenstest; Matt war zuerst und vermittelte Vorwissen für Versuch zwei. |
| Angemessener Aufwand | **3**: Viele Entscheidungsrunden, Issues und Reviews erschienen für den Mock-Schnitt aufwendig. | **5**: Die Struktur wurde für den kleineren Schnitt als sehr passend empfunden. | Kein Zeit- oder Tokenvergleich; der Funktionsumfang unterscheidet sich. |
| Vertrauen | **4**: Tests und Abschlussprüfungen überzeugten; die unabhängige finale Spezifikationsantwort fehlte. | **5**: Der selbst durchgeführte End-to-End-Test war der wichtigste Vertrauensbeleg. | Subjektives Vertrauen ist weder Fehlerfreiheit noch Security-Score. |
| Wiederaufnahme | **4**: Issues, ADRs, Glossar und Logs halfen bei getrennten Sitzungen. | **4**: Plan und Logs halfen bei der Fortsetzung; der Kontingentverbrauch blieb spürbar. | Kein standardisierter Neustartversuch mit gleicher Zeitmessung. |
| Anpassbarkeit | **4**: Integration mit GitHub, Projektregeln, PostgreSQL-Tests und Mock-Adapter. | **3**: Lokale Startprobleme und eigene Korrekturen prägten das Urteil. | Bei Superpowers teilweise Umgebungsbewertung statt reiner Workflowflexibilität. |

Bei Superpowers wurde „Anpassbarkeit“ zunächst ohne ausreichendes Verständnis mit vier bewertet. Nach Erklärung wurde der Wert ausdrücklich auf drei korrigiert; genau dieser bestätigte Endwert wird verwendet. Das ist eine dokumentierte Korrektur, keine nachträgliche Optimierung zugunsten einer Suite. [Q4, Abschnitt S5]

### 4.3 Was ist deterministisch, was nicht?

**Deterministisch ist die Auswertung vorhandener Werte.** Die Reihenfolge der Kriterien ist fest. Alle sieben Werte werden gleich gewichtet; es werden keine Kriterien ausgelassen und keine Werte nachträglich angepasst. Die Summen betragen 28 und 30. Daraus ergeben sich 28 / 7 = **4,00** für Matt und 30 / 7 = **4,29** für Superpowers, auf zwei Dezimalstellen gerundet. Dieselben Eingaben ergeben dieselbe Tabelle und Rechnung.

**Nicht deterministisch ist das persönliche Urteil.** Andere Personen oder derselbe Benutzer zu einem anderen Zeitpunkt könnten anders bewerten. Die Skala ist ordinal: Der Abstand zwischen drei und vier ist nicht nachweislich genauso groß wie zwischen vier und fünf. Der Mittelwert ist deshalb nur eine deskriptive Orientierung, kein Siegerwert, keine Prozentangabe und kein statistischer Beweis.

Für eine zukünftige Wiederholung sollten Kriterienanker vorher vereinbart werden. Beispielsweise könnte bei „Kontrolle“ eine dokumentierte, erfolgreich umgesetzte Benutzerkorrektur als Beobachtungsbeleg dienen; bei „Wiederaufnahme“ ein identischer Neustartauftrag mit messbarer Zeit bis zum ersten korrekten Arbeitsschritt. Das wäre eine Verbesserung des nächsten Versuchs, keine neue Berechnung der historischen Punkte.

## 5. Fazit und Handlungsempfehlungen

### 5.1 Antwort auf die Untersuchungsfrage

Die untersuchten Skill-Suites halfen in diesem Projekt auf unterschiedliche Weise: Matt strukturierte vor allem die Entdeckung und Dokumentation schwieriger Entscheidungen; Superpowers die Umsetzung eines begrenzten, bestätigten Entwurfs. Beide Versuche führten zu lauffähigen lokalen Ergebnissen mit dokumentierten Tests. Die Daten reichen jedoch nicht aus, eine Suite allgemein als schneller, günstiger oder qualitativ überlegen zu erklären. Dafür fehlen identischer Funktionsumfang, unabhängige Wiederholungen und vollständige Zeit- sowie Tokenmessungen.

Der praktische Wert liegt somit nicht im Unterschied von 0,29 Bewertungspunkten. Er liegt in den beobachteten Mechanismen: explizite Entscheidungsklärung, kleine überprüfbare Umsetzungsschritte, sichtbare Benutzerkorrekturen und Abgleich gegen Anforderungen. Diese Mechanismen lassen sich auf neue Aufgaben übertragen. Die konkreten Resultate bleiben an dieses Projekt, diesen Benutzer und die verwendete Umgebung gebunden.

### 5.2 Wann die Matt-Methoden besonders nützlich erscheinen

Für Integrationen mit geteilter Identität, persönlichen Daten und langfristigem Lebenszyklus würde ich eine gezielte Matt-Vertiefung bevorzugen. Hier sind scheinbar kleine Entscheidungen dauerhaft wirksam: Was identifiziert einen Kurs? Darf ein Source Update persönliche Daten überschreiben? Was geschieht nach dem letzten Abonnement? Wann darf ein Cleanup Daten entfernen? Werden solche Fragen erst während der Implementierung geklärt, können Datenmodell, API und Tests gleichzeitig verändert werden müssen.

Wayfinder, Grilling und ADRs machten diese Beziehungen in meinem Versuch sichtbar. Besonders aussagekräftig war der Cleanup-Fund im Spezifikationsreview: Eine grüne Teilimplementierung genügte nicht, weil vereinbartes Verhalten fehlte. Die Akzeptanzmatrix erlaubte, die Lücke konkret zu benennen und nachzuprüfen. Daraus leite ich einen Nutzen der dokumentierten Soll-Ist-Prüfung ab, nicht die Behauptung, nur diese Suite könne fachliche Vollständigkeit prüfen.

Die Grenze ist die Gefahr einer übergroßen ersten Version. Jede zusätzliche Antwort kann neue Zustände, Tabellen oder Betriebsregeln auslösen. Gerade ein lernender Benutzer kann überzeugend formulierte Empfehlungen übernehmen, ohne die Kosten ausreichend abzuwägen. Ich würde deshalb für jede Vertiefung eine Stop-Regel verlangen: Welche Entscheidung ist für das aktuelle Inkrement wirklich nötig, und welche wird mit dokumentiertem Risiko vertagt? Gute Architekturarbeit ist nicht identisch mit maximaler Vorabplanung.

### 5.3 Wann Superpowers der passendere Ausgangspunkt erscheint

Für einen überschaubaren vertikalen Schnitt würde ich mit dem hier erprobten Superpowers-Ablauf beginnen: Ziel klären, Entwurf bestätigen, einen ausführbaren Plan schreiben, testgetrieben umsetzen und den sichtbaren Gesamtweg abnehmen. Diese Folge war für mich gut verständlich. Sie verband Backend, Persistenz und Frontend, ohne den gesamten späteren Moodle-Betrieb vorwegzunehmen. Das begründet meine persönliche Präferenz für ähnliche Aufgaben, aber keinen universellen Standard.

Auch dieser Weg braucht Grenzen. Ein sehr langer Implementierungsplan, viele Subagent-Aufrufe und wiederholte Gesamttests können erheblichen Aufwand verursachen. Der Wechsel zur Inline-Ausführung zeigte, dass die Ausführungsform bewusst gesteuert werden muss. Er reduzierte die Zahl weiterer delegierter Schritte, beseitigte aber zugleich einen Teil der unabhängigen Reviewperspektive. Ohne gemessene Token- und Zeitdaten lässt sich daraus keine belastbare Einsparquote ableiten.

Ein kompakterer Funktionsumfang darf außerdem nicht mit höherer Qualität verwechselt werden. Superpowers musste im Versuch beispielsweise keinen vollständigen Cleanup- und Reaktivierungslebenszyklus liefern. Matt deswegen seine größere Code- oder Testmenge vorzuwerfen, wäre methodisch ebenso falsch, wie Superpowers für weniger Dateien zum Effizienzsieger zu erklären. Verglichen werden müssen passende Anforderungen und deren Nachweise, nicht die bloße Größe des Ergebnisses.

### 5.4 Was beide Workflows verbessern sollten

Der wichtigste gemeinsame Verbesserungsbedarf liegt an den Übergängen: vom bestätigten Entwurf zur vollständigen Implementierung und von grünen Tests zur tatsächlich nutzbaren Umgebung. Im Matt-Versuch waren Produktionsmigration und Cleanup kritische Übergänge. Im Superpowers-Versuch betraf es unter anderem Appsettings im versteckten Worktree. Ein gestarteter Prozess war zunächst zu früh als erfolgreicher Start eingeordnet worden. Erst ein konkreter Funktionsnachweis korrigierte diese Einschätzung.

Deshalb würde ich eine frühe Umgebungsabnahme vorsehen: Ein kleiner, dokumentierter Ablauf startet die Anwendung mit dem vorgesehenen Konfigurationsweg, prüft Datenbank und Migrationen und öffnet eine geschützte Funktion im Browser. Am Ende wird genau dieser Ablauf erneut geprüft. Das ersetzt keine fachlichen Tests, ergänzt aber eine bisher zu spät sichtbare Grenze. Fehler werden getrennt als Produkt-, Umgebungs- oder Ausführungsfehler dokumentiert.

Ebenso wichtig ist eine Abschlussmatrix. Jede freigegebene FR und NFR erhält einen passenden Beleg oder den Status „offen“. Ein geschlossenes Issue, eine positive Agentenaussage oder ein grüner Build darf diesen Nachweis nicht ersetzen. Review muss daher zwei Fragen beantworten: Ist die Implementierung technisch vertretbar, und erfüllt sie den vereinbarten Umfang? Ein Selbstreview bleibt als solcher sichtbar, wenn ein unabhängiger Review wegen Kontingentmangels ausfällt.

### 5.5 Konkreter Vorschlag für den nächsten Projekteinsatz

Ich schlage einen risikobasierten kombinierten Prozess vor. Dieser Vorschlag wurde **noch nicht als dritter Versuch getestet**; im Superpowers-Experiment wurden keine Matt-Skills beigemischt.

1. **Eingang klären:** Ein kleines Ziel, seine FR/NFR und ausgeschlossene Funktionen schriftlich festlegen. Unklare Begriffe vor der Umsetzung erklären.
2. **Risiko prüfen:** Bei geteilter Identität, Autorisierung, Nebenläufigkeit, Datenaufbewahrung oder schwer änderbaren Schnittstellen gezielte Entscheidungsfragen und bei Bedarf ein ADR ergänzen.
3. **Klein ausführen:** Einen vertikalen Ablauf testgetrieben umsetzen. Delegation nur dort einsetzen, wo Aufgaben wirklich unabhängig sind oder eine zusätzliche Reviewperspektive benötigt wird.
4. **Evidenz schließen:** Anforderungen mit Tests, Review und einem sichtbaren Benutzerpfad verbinden. Fehlende Nachweise offen ausweisen.
5. **Erfahrung sichern:** Entscheidungen, bestätigte Bewertungen, Versionsstände und verbleibende Risiken in einem vollständigen Übergabepaket speichern.

Für die Wirksamkeit dieses Vorschlags wären in einer Folgeuntersuchung derselbe Feature-Schnitt, vergleichbare Umgebungen und vorab definierte Bewertungsanker notwendig. Besonders interessant wäre, ob gezielte Architekturvertiefung die Zahl späterer Korrekturen senkt, ohne den ersten Lieferumfang unverhältnismäßig zu vergrößern. Genau das wurde in der vorliegenden Fallstudie noch nicht quantitativ gemessen.

### 5.6 Grenzen und abschließende Entscheidung

Die Versuche fanden nacheinander statt. Das Wissen aus Matt beeinflusste den späteren Superpowers-Versuch, und dieselbe Person entschied Anforderungen und bewertete die Ergebnisse. Modellverhalten, Agentenumgebung, Sandbox und Skill-Suite sind keine unabhängig kontrollierten Faktoren. Auch die unterschiedliche Bedeutung, die ich bei „Anpassbarkeit“ einzelnen Startproblemen gab, begrenzt den Vergleich dieser Punkte.

Hinzu kam das begrenzte Token- beziehungsweise Nutzungskontingent. Deshalb konnten nicht beliebig viele unabhängige Ausführungen und Reviews durchgeführt werden. Die Logs belegen einen ausgefallenen Matt-Re-Review sowie unterbrochene beziehungsweise umgestellte Superpowers-Ausführung. Eine vollständige Tokenbilanz wurde nicht erhoben. Weder eine exakte Budgetzahl noch eine Kosteneinsparung wird nachträglich behauptet. Die sichtbaren Beschränkungen sind Teil des Ergebnisses.

Meine Schlussentscheidung lautet: Für das nächste ähnlich begrenzte Feature würde ich Superpowers als Ausgangspunkt wählen und komplexe Architekturfragen bewusst vertiefen. Für eine langfristige Integrationsplattform würde ich von Beginn an mehr Entscheidungshistorie und Spezifikationsreview einplanen. Ausschlaggebend sind die Risiken des Produkts und die prüfbaren Nachweise, nicht die Marke der Suite. Ein Mock beweist dabei nur den getesteten lokalen Ablauf; echte Moodle-Kompatibilität, Lastverhalten und ein wiederholter Vergleich bleiben weitere Arbeit.

## 6. Persönliche Lernerfahrungen

### 6.1 Architektur selbst verstehen

Vor dem Versuch dachte ich vor allem an einen Kurslink und eine neue PDF. Jetzt kann ich erklären, warum Quellenformat, stabile Identität und persönliche Aufgabe verschiedene Dinge sind. Ein Adapter schützt das interne Modell vor wechselnden Zugangswegen. Ein gemeinsamer Scan spart nur dann sinnvoll Arbeit, wenn Berechtigungen und persönliche Zustände trotzdem getrennt bleiben. Diese Erkenntnisse erklären meinen hohen Lernwert im Matt-Versuch.

### 6.2 Vorschläge nicht mit Entscheidungen verwechseln

Ich habe gelernt, technische Empfehlungen nicht nur mit einer Antwortoption zu bestätigen. Ich muss den Grund und die Auswirkung auf den Umfang verstehen. Auch ein LLM-Vorschlag wäre nicht automatisch fachlich richtig. Die Diskussion über strukturierte Fristen machte eine überprüfbare Vertrauensgrenze verständlich; eine LLM-Integration selbst habe ich in diesem Versuch nicht getestet.

### 6.3 Verantwortung und begrenztes Kontingent

Ich hatte nur ein begrenztes Token- beziehungsweise Nutzungskontingent zur Verfügung und konnte deshalb nicht so viele unabhängige Ausführungen durchführen, wie für einen belastbaren Benchmark wünschenswert wären. Das zwang mich, Prioritäten zu setzen und die Ausführungsform mitzuentscheiden. Der manuelle Browser-Test blieb besonders wichtig: Ich prüfte selbst, ob das Ergebnis tatsächlich funktioniert, statt eine grüne Zusammenfassung allein zu übernehmen.

Der Agent unterstützte Implementierung, Auswertung und sprachliche Überarbeitung dieses Papers. Die persönlichen Punktwerte stammen von mir; automatisierte Nachweise, meine Eindrücke und spätere methodische Vorschläge bleiben getrennt. Für künftige Projekte möchte ich Anforderungen früher begrenzen und meine Gründe direkt beim Entscheiden dokumentieren.

## Quellen und mitgelieferte Nachweise

Alle Q-Quellen liegen im Quellenpaket als Markdown-Dateien. Die Originalstände sind unveränderte Git-Exporte; `references/source-manifest.json` dokumentiert vollständige Commit-IDs und SHA-256-Prüfsummen. Die FR/NFR-Zusammenstellung, Abbildungen und Erläuterungen zur Bewertungsmethodik sind Ergänzungen dieser Überarbeitung, keine nachträglich veränderten Versuchslogs.

- **Q1:** [Versuchsprotokoll](experiment-protocol.md), Stand `a8801ff`. Enthält Forschungsziel, ursprüngliche Vergleichsregeln, Versionen und Bewertungsskala. Die darin erwartete Umfangsgleichheit wurde nicht erreicht.
- **Q2:** [Matt-Beobachtungsprotokoll](references/matt/Docs/skill-evaluation/matt-observations.md), Stand `ab8249c`. Maßgeblich: Entscheidungsdurchläufe, Review und Korrekturen, Bewertung durch den Benutzer.
- **Q3:** [Matt-Domänenmodell](references/matt/CONTEXT.md) und [Matt-Versuchsprotokoll](references/matt/Docs/skill-evaluation/experiment-protocol.md), Stand `ab8249c`.
- **Q4:** [Superpowers-Beobachtungslog](references/superpowers/Docs/skill-evaluation/superpowers-observations.md), Stand `a8801ff`. Maßgeblich: Baseline, Tasks 1 bis 12, S5 und Bewertungstabelle.
- **Q5:** [Bestätigtes Superpowers-Design](references/superpowers/Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md), Stand `a8801ff`. Enthält Scope, Vertrauensregeln und 15 Akzeptanzkriterien.
- **Q6:** [Superpowers-Aktivitätslog](references/superpowers/Docs/skill-evaluation/agent-activity-log.md), Stand `a8801ff`. Maßgeblich: Task 12, Abschlussreview und S5.
- **Q7:** [Faktenvergleich](references/comparison/Docs/skill-evaluation/matt-vs-superpowers-comparison.md), Stand `866e724`. Ergänzende Git-Messwerte und explizite Vergleichsgrenzen; sekundäre Auswertung, kein Ersatz für Q1 bis Q6.

[Lesereihenfolge, Paketinhalt und Reproduktionshinweise](PAPER-README.md)
