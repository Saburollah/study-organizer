# Agentische Skill-Suites in der Softwareentwicklung

## Eine praxisnahe Fallstudie mit Matt Pocock Skills und Obras Superpowers

**Autor:** Saburollah Safari

**Projekt:** Study Organizer

**Stand:** 30. August 2026

## Kurzfassung

In dieser Fallstudie untersuche ich, wie zwei Skill-Suites einen KI-Agenten bei
der Entwicklung eines anspruchsvollen Webapp-Features steuern. Als Fallbeispiel
diente eine Moodle-nahe Kursintegration für den Study Organizer. Die Anwendung
soll externe Kursinhalte erkennen, Änderungen von früheren Scans unterscheiden,
doppelte Verarbeitung vermeiden und daraus persönliche Aufgaben für mehrere
Benutzer ableiten.

Ich setzte zunächst die Skills von Matt Pocock ein, darunter Wayfinder,
Grilling, Domain Modeling, Prototype, TDD und Code Review. Danach führte ich von
demselben Ausgangscommit ein getrenntes Experiment mit Obras Superpowers durch.
Beide Versionen wurden getestet, dokumentiert und anhand derselben sieben
Kriterien bewertet. Das Ergebnis ist kein universeller Sieger: Superpowers
eignete sich in diesem Versuch besser für ein klar begrenztes, ausführbares
Produktinkrement. Die Matt-Suite war stärker, als langfristige
Architekturentscheidungen, Datenlebenszyklen, Nebenläufigkeit und eine
ausführliche Entscheidungshistorie im Mittelpunkt standen.

Die wichtigste Erkenntnis ist deshalb nicht, welche Suite „besser“ ist, sondern
wann welcher Arbeitsmodus angemessen ist. Ich schlage einen kombinierten Ansatz
vor: Superpowers als schlanken Standardprozess und zusätzliche Matt-Methoden als
gezielte Vertiefung bei risikoreichen oder schwer rückgängig zu machenden
Entscheidungen.

## 1. Ausgangsproblem

Die ursprüngliche Idee klang einfach: Ein Benutzer registriert einen Moodle-Kurs
im Study Organizer. Die Anwendung prüft regelmäßig, ob neue Übungsaufgaben oder
Dateien vorhanden sind, und legt daraus persönliche Aufgaben an. Bei genauerer
Betrachtung entstehen jedoch mehrere Architekturprobleme.

Moodle-Kurse besitzen keine einheitliche sichtbare Struktur. Eine Übung kann als
PDF, als normale Datei, als Link, als Moodle-Aktivität oder als Textinhalt
erscheinen. Deshalb reichen wenige fest verdrahtete REST-Methoden nicht aus. Die
Anwendung benötigt eine stabile eigene Sicht auf externe Kurse und austauschbare
Zugangswege. Diese Trennung lässt sich durch Adapter ausdrücken: Ein Adapter
übersetzt die jeweilige Moodle- oder Mock-Struktur in ein internes, einheitliches
Modell.

Hinzu kommen fachliche Regeln:

- Ein bereits bekannter Inhalt darf beim nächsten Scan nicht erneut als Aufgabe
  angelegt werden.
- Wenn drei Personen denselben Kurs abonnieren, soll der Kurs nur einmal
  gescannt werden. Das Ergebnis wird anschließend persönlich für alle drei
  Benutzer verarbeitet.
- Persönliche Daten wie Aufgabenstatus oder eigene Änderungen dürfen nicht durch
  spätere Quelldaten überschrieben werden.
- Ein LLM darf höchstens einen Vorschlag zur Klassifikation machen. Die
  Anwendung muss das Ergebnis anhand stabiler Regeln prüfen, weil ein normaler
  Hinweis sonst fälschlich als Aufgabe erkannt werden könnte.
- Während der Semesterferien muss die Architektur ohne aktiven realen Kurs
  testbar bleiben. Dafür wurde eine kontrollierte Mock-Moodle-Quelle verwendet.

Damit war das Feature ein geeignetes Experiment: Es verbindet
Softwarearchitektur, Domänenmodellierung, Datenhaltung, API, Frontend, Tests und
menschliche Produktentscheidungen.

## 2. Versuchsaufbau

Beide Skill-Suites wurden versioniert und über das zentrale Repository
`Saburollah/agent-skills` eingebunden. Der Matt-Versuch verwendete den Tag
`matt-v1.0`, der Superpowers-Versuch den Tag `superpowers-v6.3.0`. Beide
Experimente starteten vom gemeinsamen Commit `e7d8b5e`. Der Superpowers-Versuch
lief zusätzlich in einem eigenen Worktree. Dadurch blieben Produktcode,
Dokumentation und Git-Historie voneinander getrennt.

Vorab wurde ein gemeinsames
[Versuchsprotokoll](experiment-protocol.md) festgelegt. Es betrachtete nicht nur
den erzeugten Code, sondern auch:

- Qualität der Anforderungsklärung,
- Nachvollziehbarkeit von Architekturentscheidungen,
- TDD- und Reviewverhalten,
- menschlichen Steuerungsaufwand,
- technische Nachweise,
- Wiederaufnahme durch einen frischen Agenten.

Nach jedem Versuch bewertete ich sieben Kriterien von 1 bis 5 und begründete die
Werte mit konkreten Beispielen. Git-Messwerte wurden nur als Umfangsindikatoren
verwendet, nicht als Qualitätsmaß.

Die Feature-Schnitte waren nicht identisch. Matt realisierte den größeren
Lebenszyklus mit asynchronen Scans, Scan-Historie, Reaktivierung, Retention und
Parallelität. Superpowers setzte einen kleineren synchronen End-to-End-Schnitt
um. Ein direkter Vergleich von Codezeilen oder Testzahlen wäre deshalb unfair.
Verglichen wurden primär die Arbeitsweisen.

## 3. Versuch mit Matt Pocock Skills

Wayfinder zerlegte die unscharfe Idee zunächst in eine GitHub-Issue-Landkarte.
Die einzelnen Entscheidungen betrafen unter anderem persönliche
Kursabonnements, stabile externe Identitäten, Aufgabenlebenszyklen,
Parallelverhalten, Berechtigungen, Datenmodell, API und Abnahmekriterien.
Grilling prüfte diese Entscheidungen anhand konkreter Konfliktfälle. Domain
Modeling überführte die bestätigten Begriffe in `CONTEXT.md` und sieben ADRs.

Diese Vorgehensweise machte mehrere Probleme früh sichtbar. Besonders wichtig
war die Trennung zwischen einem gemeinsam gespeicherten externen Kurs und den
persönlichen Aufgaben der Benutzer. Ebenfalls entscheidend waren stabile
externe Schlüssel und Datenbank-Constraints: Ein Vergleich nur über Dateinamen
oder URLs wäre bei Umbenennungen und parallelen Scans zu unsicher gewesen.

Ein UI-Prototyp stellte drei Registrierungsabläufe gegenüber. Erst nach dem
praktischen Vergleich wurde ein geführter Ablauf mit Kurslink, Modulwahl und
Ergebnisübersicht ausgewählt. Die spätere Implementierung folgte einer
Akzeptanzmatrix und wurde mit Domain-, API-, Frontend-, PostgreSQL-Integrations-
und Parallelitätstests sowie einem Playwright-Golden-Path geprüft.

Die Matt-Suite war besonders stark bei Fragen, deren falsche Antwort später
teuer wäre. Die getrennten Standards- und Spezifikationsreviews fanden unter
anderem fehlende Cleanup-Regeln, obwohl große Teile der Implementierung bereits
grün waren. Gleichzeitig wuchs der Umfang deutlich. Zehn dokumentierte
Entscheidungsdurchläufe, zahlreiche Issues, sieben ADRs und mehrere Reviewrunden
waren lehrreich, aber für einen ersten Mock-Schnitt aufwendig.

Meine Bewertung lautete:

| Kriterium | Wert |
| --- | ---: |
| Verständlichkeit | 4 |
| Kontrolle | 4 |
| Lerngewinn | 5 |
| Angemessener Aufwand | 3 |
| Vertrauen | 4 |
| Wiederaufnahme | 4 |
| Anpassbarkeit | 4 |

Der hohe Lerngewinn entstand vor allem durch die nachvollziehbare Kette von der
dynamischen Moodle-Struktur über Adapter und stabile Identitäten bis zum
gemeinsamen Scan und zur persönlichen Projektion. Der niedrigere Aufwandswert
zeigt die Kehrseite dieser Tiefe.

## 4. Versuch mit Obras Superpowers

Der Superpowers-Versuch begann mit Brainstorming und einer bestätigten
Designspezifikation. Anschließend erzeugte `writing-plans` einen ausführbaren
Plan mit zwölf Aufgaben. TDD strukturierte jede Aufgabe als
Red-Green-Refactor-Schritt. Zunächst wurde subagentengesteuert gearbeitet;
später wechselte ich bewusst zur Inline-Ausführung, weil sie für die verbleibenden
Aufgaben übersichtlicher und sparsamer war.

Superpowers führte den begrenzten Schnitt konsequent durch Domain, Persistenz,
API und Vue-Frontend. Nur ein als Assignment normalisierter Inhalt mit einer
strukturierten Frist erzeugte automatisch eine Aufgabe. Freitext-Datumsangaben
und LLM-Erkennung blieben bewusst ausgeschlossen. Damit wurde die
Vertrauensgrenze klar und testbar.

Der Prozess erzeugte 195 grüne Backendtests und 97 grüne Frontendtests. Build,
Typecheck und Lint waren erfolgreich. Den wichtigsten Vertrauensnachweis lieferte
der manuelle Browser-Walkthrough. Dabei wurde zugleich eine Schwäche sichtbar:
In dem versteckten Worktree wurden vorhandene Appsettings vom
`PhysicalFileProvider` zunächst nicht geladen. Zusätzlich mussten lokale
Datenbank-, Migrations- und Vite-Cache-Zustände korrigiert werden. Die fachlichen
Tests waren bereits grün, aber die Anwendung startete in der realen lokalen
Umgebung noch nicht zuverlässig. Erst der Sichttest machte diese Grenze
sichtbar.

Meine Bewertung lautete:

| Kriterium | Wert |
| --- | ---: |
| Verständlichkeit | 5 |
| Kontrolle | 4 |
| Lerngewinn | 4 |
| Angemessener Aufwand | 5 |
| Vertrauen | 5 |
| Wiederaufnahme | 4 |
| Anpassbarkeit | 3 |

Superpowers wirkte kurz, sauber und gut ausführbar. Spezifikation, Plan und
Task-Ledger erleichterten die Fortsetzung. Die geringere Bewertung der
Anpassbarkeit beruht auf den lokalen Startproblemen und dem zusätzlichen
Agentenkontingent: Im Log wurden 22 Subagent-Aufrufe und 33
Umgebungsfreigaben erfasst.

## 5. Vergleich der Stärken und Grenzen

Der arithmetische Mittelwert betrug 4,00 für Matt und 4,29 für Superpowers. Er
dient nur der Übersicht. Wegen der unterschiedlichen Feature-Schnitte, des
Lerneffekts zwischen den Versuchen und der Bewertung durch nur eine Person ist
er kein Qualitätsranking.

Superpowers war im begrenzten Schnitt verständlicher und im Aufwand
angemessener. Der detaillierte Plan ließ sich schrittweise ausführen, und der
manuelle End-to-End-Test erhöhte das Vertrauen. Die Suite eignet sich daher gut
für ein klar beschriebenes Produktinkrement, das in kurzer Folge geplant,
testgetrieben umgesetzt und lokal abgenommen werden soll.

Matt bot mehr Tiefe bei Architektur und Entscheidungshistorie. Wayfinder machte
Abhängigkeiten sichtbar, Grilling deckte Randfälle auf, und ADRs hielten die
Entscheidungen langfristig fest. Diese Stärke ist besonders relevant bei
gemeinsam genutzten Daten, Nebenläufigkeit, Autorisierung, Retention oder
Deployment. Für kleine Features kann derselbe Prozess jedoch zu viele Fragen,
Artefakte und Folgethemen erzeugen.

Auch die Reviewmechanismen unterschieden sich. Matt trennte Standards- und
Spezifikationsreview und fand dadurch Fälle von „technisch grün, fachlich noch
unvollständig“. Superpowers fand Fehler früh innerhalb kleiner TDD-Schritte und
führte am Ende einen Gesamt-Review durch. Nach dem Wechsel zur Inline-Ausführung
fehlte allerdings ein Teil der unabhängigen Reviewbreite.

Beiden Versuchen gemeinsam ist eine wichtige Warnung: Gute Unit- und
Integrationstests ersetzen keine frühe Prüfung der tatsächlichen
Ausführungsumgebung. Bei Matt wurde eine Produktionsmigrationslücke spät
entdeckt; bei Superpowers betraf die späte Lücke Worktree-Konfiguration,
Datenbankzustand und Cache.

## 6. Eigene Lernerkenntnisse

Ich habe aus dem Experiment fünf übertragbare Erkenntnisse gewonnen.

Erstens muss ein dynamisches externes System hinter einer stabilen eigenen
Schnittstelle liegen. Adapter schützen die Domäne davor, Moodle-spezifische
Strukturen in allen Schichten zu verteilen.

Zweitens sind Identität und Deduplizierung zentrale Fachfragen und keine bloßen
Implementierungsdetails. Nur mit stabilen externen Schlüsseln und
Datenbankregeln kann ein Kurs einmal gescannt und für mehrere Benutzer sicher
wiederverwendet werden.

Drittens benötigt LLM-Unterstützung eine überprüfbare Vertrauensgrenze. Ein LLM
kann unbekannte Inhalte klassifizieren oder Toolaufrufe vorschlagen, darf aber
nicht ungeprüft persönliche Aufgaben erzeugen. Deterministische Regeln,
strukturierte Felder und nachvollziehbare Fehlerfälle bleiben notwendig.

Viertens ist Dokumentation ein Arbeitsmittel. Issues, ADRs, Spezifikationen und
Task-Ledger ermöglichen es einem neuen Agenten oder Teammitglied, Entscheidungen
fortzuführen, statt sie erneut zu erraten.

Fünftens bedeutet ein grüner Testlauf noch nicht, dass ein Feature für einen
Benutzer funktioniert. Der manuelle End-to-End-Test deckte eine reale
Umgebungsgrenze auf, die in den fachlichen Tests nicht sichtbar war.

## 7. Verbesserungsvorschlag

Aus beiden Ansätzen würde ich einen risikobasierten Hybridprozess bilden:

1. **Superpowers als Standard:** Brainstorming, bestätigte Spezifikation,
   ausführbarer Plan, TDD und lokaler End-to-End-Nachweis bilden den normalen
   Weg für begrenzte Produktinkremente.
2. **Architektur-Eskalation nach festen Kriterien:** Matt-Wayfinder, Grilling
   und ADRs werden ergänzt, sobald gemeinsame Identität, Nebenläufigkeit,
   Berechtigungen, Datenaufbewahrung, Compliance oder schwer rückgängig zu
   machende Schnittstellen betroffen sind.
3. **Frühe Environment-Abnahme:** Noch vor der vollständigen Implementierung
   startet ein kleiner Smoke-Test Backend, Datenbank und Frontend im vorgesehenen
   Worktree. Er prüft Content-Root, Secrets, CORS, Migration History, Cache und
   Produktionsmigration.
4. **Gemeinsames Messschema:** Rückfragen, Agentenwechsel, Freigaben,
   Review-Funde, manuelle Benutzerzeit und Testnachweise werden für beide
   Workflows automatisch und gleichartig protokolliert.
5. **Explizite Abschlussmatrix:** Technische Tests, fachliche Akzeptanzkriterien
   und sichtbares Benutzerverhalten erhalten jeweils einen eigenen Nachweis.

Damit bleibt der normale Ablauf schlank, ohne bei risikoreichen Entscheidungen
auf die Tiefe der Matt-Suite zu verzichten.

## 8. Fazit

Das Experiment zeigt, dass Agent Skills mehr als wiederverwendbare Prompts sind.
Sie formen die Zusammenarbeit zwischen Mensch und Agent: welche Fragen gestellt
werden, wann Code entsteht, wie Entscheidungen dokumentiert und wie Ergebnisse
geprüft werden.

Für klar begrenzte, ausführbare Features würde ich Superpowers als
Standardprozess wählen. Für langfristige Integrationen mit komplexem
Datenlebenszyklus, Parallelität und vielen Beteiligten würde ich die
Matt-Methoden gezielt ergänzen. Entscheidend ist nicht die maximale Zahl von
Skills oder Artefakten, sondern ein Prozess, dessen Tiefe zum Risiko der
Entscheidung passt.

Diese Fallstudie ist kein kontrolliertes Benchmark und beweist noch keine reale
Moodle-Kompatibilität. Sie zeigt jedoch anhand von Code, Tests, Git-Historie,
Reviews und Benutzerbewertung, wie zwei unterschiedliche Agentenmethoden in
einem realen Softwareprojekt wirken und wie sich daraus ein verbesserter,
kontextabhängiger Entwicklungsprozess ableiten lässt.

## Nachweise

- [Versuchsprotokoll](experiment-protocol.md)
- [Matt-Beobachtungsprotokoll im Abschlusscommit](https://github.com/Saburollah/study-organizer/blob/ab8249c/Docs/skill-evaluation/matt-observations.md)
- [Superpowers-Beobachtungslog](superpowers-observations.md)
- [Detaillierter Faktenvergleich](matt-vs-superpowers-comparison.md)
- [Agenten-Aktivitätslog](agent-activity-log.md)
- Gemeinsamer Ausgangspunkt: Commit `e7d8b5e`
- Superpowers-Vergleichscommit: `866e724`
