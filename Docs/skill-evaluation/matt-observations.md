# Beobachtungsprotokoll: Matt-Pocock-Skills

## Rahmen

- Projekt: Study Organizer
- Experiment-Branch: `experiment/matt`
- Startdatum: 20.08.2026
- Unveränderter Vergleichspunkt: `e7d8b5e`
- Matt-Konfiguration: `05d2609`
- Untersuchte Funktion: Moodle-Kurse erkennen und neue Übungsaufgaben als Tickets anlegen

## Baseline vor der Feature-Implementierung

Die Konfigurationsänderungen betrafen nur Dokumentation und Agent-Anweisungen. Der Programmcode wurde noch nicht verändert.

| Prüfung | Ergebnis |
|---|---|
| Backend-Build | Erfolgreich |
| Backend-Tests | Erfolgreich |
| Frontend-Type-Check | Erfolgreich |
| Oxlint | 0 Fehler, 0 Warnungen |
| ESLint | Erfolgreich |
| Frontend-Tests | Erfolgreich |
| Frontend-Produktions-Build | Erfolgreich, 86 Module, 313 ms |
| Git-Arbeitsverzeichnis danach | Sauber |

## Beobachtungen während des Experiments

### Wayfinder

Wayfinder wurde verwendet, um die unscharfe Moodle-Idee in eine Entscheidungslandkarte zu überführen.

Ergebnis:

- GitHub-Landkarte: https://github.com/Saburollah/study-organizer/issues/67
- Sechs Entscheidungstickets wurden als native Unter-Issues erstellt.
- Abhängigkeiten zeigen, welche Entscheidungen zuerst getroffen werden müssen.
- Die erste bearbeitbare Front besteht aus den Issues 68, 69 und 73.
- Während der Kartierung wurde bewusst noch kein Feature-Code geschrieben.

Was Wayfinder gut konnte:

- Ziel und Umfang wurden ausdrücklich festgelegt.
- Reale Moodle-Anmeldung, Polling, LLM und Benachrichtigungen wurden klar ausgeschlossen.
- Architekturfragen wurden von Implementierungsaufgaben getrennt.
- Abhängigkeiten zwischen Entscheidungen wurden sichtbar.
- Die Planung ist für andere Personen auf GitHub nachvollziehbar.

Grenzen und Aufwand:

- Die erstmalige Einrichtung mit Labels, Dokumentation und GitHub CLI benötigt Zeit.
- Native Unter-Issues und Abhängigkeiten erfordern relativ komplizierte GitHub-API-Befehle.
- Für ein kleines Feature könnte die Zahl der Issues zu groß wirken.
- Wayfinder trifft fachliche Entscheidungen nicht selbst, sondern benötigt Antworten eines Menschen.

#### Erster Entscheidungsdurchlauf

Die Landkarte wurde erneut mit Wayfinder geöffnet. Das erste freie Ticket „Persönliches Study Module für ein Kursabonnement festlegen“ wurde vor der Bearbeitung einem Bearbeiter zugewiesen.

Grilling klärte schrittweise:

- Ein Kursabonnement verwendet ein vorhandenes persönliches Study Module.
- Pro Study Module ist höchstens ein Abonnement erlaubt.
- Ein Benutzer kann denselben External Course nur einmal abonnieren.
- Persönliche Moduldaten werden nicht durch externe Kursdaten überschrieben.
- Ein Kurswechsel benötigt ein bewusstes Beenden des alten Abonnements.

Die Entscheidung wurde als Kommentar dokumentiert, das Ticket geschlossen und in der Landkarte verlinkt. Die neu entdeckte Frage zum External Course ohne Abonnenten wurde als eigenes Unter-Issue angelegt.

Positiv war die klare Konzentration auf genau eine Entscheidung. Nachteilig war der hohe Verwaltungsaufwand durch Zuweisung, Kommentar, Schließen, Aktualisieren der Landkarte und Erstellen eines Folgetickets.

#### Zweiter Entscheidungsdurchlauf

Das Ticket „Stabile Identität für externe Kurse und Inhalte festlegen“ wurde mit Wayfinder, Grilling und Domain Modeling bearbeitet.

Eine lesende Code-Erkundung stellte zuerst fest, dass das Projekt interne Guids verwendet, aber noch keine externen Schlüssel, Unique Constraints oder Datenbanktests für parallele Deduplizierung besitzt.

Grilling trennte anschließend:

- interne Study-Organizer-Identität,
- externe Kursidentität,
- externe Inhaltsidentität,
- veränderliche Metadaten,
- und eine getrennte Änderungssignatur.

Fehlende oder doppelte externe Schlüssel wurden als Fehler des gesamten Scans festgelegt. Datenbank-Eindeutigkeit wurde zusätzlich zur Prüfung im Programm verlangt.

Positiv war, dass konkrete Fälle wie Umbenennung, verschiedene Kurslinks und parallele Scans die abstrakte Identitätsfrage verständlich machten. Nachteilig war, dass die technischen Empfehlungen sehr überzeugend wirken und deshalb leicht übernommen werden können, ohne alle Alternativen selbst ausreichend zu prüfen.

Die Entscheidung wurde in einem Auflösungskommentar, im Domain-Glossar und zusätzlich in einem ADR dokumentiert.

#### Dritter Entscheidungsdurchlauf

Das Ticket „Lebenszyklus importierter Aufgaben festlegen“ untersuchte Konflikte zwischen gemeinsamen Moodle-Daten und persönlicher Aufgabenplanung.

Die Code-Erkundung zeigte, dass normale Study Tasks heute vollständig bearbeitet, abgeschlossen, wieder geöffnet und physisch gelöscht werden können. Importherkunft, externe Links, Feldherkunft, Tombstones und Zustände für extern entfernte Inhalte fehlen noch.

Grilling behandelte konkrete Fälle:

- persönliche Bearbeitung einer importierten Aufgabe,
- externe Änderungen an bereits abgeschlossenen Aufgaben,
- persönliches Löschen und spätere Scans,
- extern entfernte und wieder erschienene Inhalte,
- Ende eines Kursabonnements,
- und bewusstes Wiederherstellen eines Imports.

Als Ergebnis wurden persönliche Aufgaben und gemeinsame Quelldaten klar getrennt. Source Update und Dismissed Import wurden als neue Fachbegriffe eingeführt.

Stark war die systematische Behandlung von Randfällen, die sonst erst als Fehler während der Implementierung sichtbar geworden wären. Schwach war, dass aus einem kleinen Mock-Feature zusätzliche Zustände und Datenstrukturen entstanden. Der Skill verbessert Robustheit, kann aber den Umfang einer ersten Version deutlich vergrößern.

#### Vierter Entscheidungsdurchlauf

Das Ticket „Fehler-, Wiederholungs- und Parallelverhalten eines Scanlaufs festlegen“ untersuchte Atomarität, Idempotenz, gleichzeitige Aufrufe und Fehlerzustände.

Die Code-Erkundung zeigte, dass das Backend bereits Cancellation Tokens weiterreicht, aber noch keine expliziten Transaktionen, Scan-Sperren, Retry-Logik, globalen Fehlerverträge oder Datenbanktests für Parallelität besitzt.

Grilling entschied unter anderem:

- höchstens einen aktiven Scan Run pro External Course,
- vollständiges Abrufen und Validieren vor der Datenbanktransaktion,
- atomare Übernahme aller fachlichen Änderungen,
- Erhalt des letzten erfolgreichen Course Snapshot bei Fehlern,
- keine automatische Wiederholung im Mock-Schnitt,
- stabile Fehlerkategorien und sichere Logs,
- ablaufende Sperren für abgestürzte Scans,
- und echte PostgreSQL-Integrationstests.

Stark war, dass der Skill nicht nur den Erfolgsfall, sondern auch Abbruch, Timeout, Serverabsturz, verlorene Antworten und Datenbankrennen untersuchte. Schwach war, dass für einen manuellen Mock-Scan bereits anspruchsvolle Betriebsmechanismen entstehen. Die Planung wird sicherer, kann aber zu einer technisch schweren ersten Version führen.

#### Fünfter Entscheidungsdurchlauf

Das Ticket „Berechtigungen für gemeinsam gespeicherte externe Kurse festlegen“ untersuchte die Sicherheitsgrenze zwischen gemeinsam gespeicherten Kursdaten und persönlichen Benutzerdaten.

Die Code-Erkundung zeigte, dass das Projekt Zugriffe bisher über den angemeldeten Benutzer und den Besitzer eines Study Module schützt. Study Tasks übernehmen diese Berechtigung über ihr Modul. Fremde und nicht vorhandene Ressourcen liefern einheitlich 404, aber für gemeinsam gespeicherte Kurse existieren noch keine eigenen Regeln oder Policies.

Grilling entschied unter anderem:

- Die aktive Course Subscription ist die Zugangsgrenze zum gemeinsamen External Course.
- Ein External Course besitzt keinen Benutzer als Eigentümer.
- Nur der Eigentümer eines Study Module darf dessen Subscription verwalten.
- Jeder Abonnent muss seinen externen Kurszugriff selbst nachweisen.
- Externe Zugangsdaten bleiben persönlich und werden nicht geteilt.
- Jeder aktive Abonnent darf einen gemeinsamen Scan Run auslösen.
- Ein validierter Scan wird einmal für alle aktiven Subscriptions verarbeitet.
- Andere Abonnenten, ihre Anzahl, Module, Aufgaben und der Scan-Auslöser bleiben verborgen.
- Fremde und unbekannte Kursressourcen liefern einheitlich 404.
- Es gibt weder ein globales Kursverzeichnis noch eine Administrator-Ausnahme im MVP.

Stark war, dass der Skill Datenschutz- und IDOR-Risiken sichtbar machte, die durch gemeinsam gespeicherte Daten entstehen. Die Entscheidungen konnten außerdem an das bereits vorhandene Owner-Prüfungsmuster des Projekts angelehnt werden. Schwach war erneut der hohe Umfang der Fragerunden: Für einen Mock-Prototyp wurden bereits Regeln für eine spätere echte Moodle-Autorisierung diskutiert.

Die Entscheidung wurde im GitHub-Ticket, im Domain-Glossar und im ADR `Docs/adr/0004-gemeinsame-kurse-durch-subscriptions-autorisieren.md` dokumentiert.

#### Sechster Entscheidungsdurchlauf

Das Ticket „Registrierung und manuellen Scan als UI-Ablauf prototypisieren“ untersuchte, welcher kleinste Benutzerablauf Kursregistrierung, Modulzuordnung, ersten Scan sowie Erfolgs- und Fehlerzustände verständlich macht.

Der Prototype-Skill erzeugte drei strukturell unterschiedliche Varianten innerhalb der vorhandenen Modulansicht:

- einen geführten Drei-Schritte-Assistenten,
- eine modulzentrierte Verbindung direkt auf Modulkarten,
- und eine zweigeteilte Kurszentrale mit Kennzahlen und Scan-Verlauf.

Alle Varianten verwendeten ausschließlich lokalen Mock-Zustand. Der vollständige Prototyp wurde auf dem Wegwerf-Branch `codex/prototype-course-registration-flow` im Commit `2ee108b` gesichert.

Nach dem praktischen Test wurde ein geführter Ablauf mit Kurslink zuerst, anschließender Modulwahl und einer Ergebnisübersicht mit Kennzahlen und Scan-Verlauf gewählt.

Positiv war, dass konkrete Varianten verglichen und gute Teile verschiedener Entwürfe kombiniert werden konnten. Nachteilig war der hohe Aufwand für Wegwerfcode, Prüfungen und einen separaten Branch.

#### Siebter Entscheidungsdurchlauf

Das Ticket „Lebenszyklus eines External Course ohne Abonnenten festlegen“ untersuchte die Aufbewahrung gemeinsam gespeicherter Kursdaten nach dem Ende der letzten Subscription.

Die Code-Erkundung zeigte, dass Module und Aufgaben bisher hart gelöscht werden, Module ihre Aufgaben per Cascade entfernen und noch keine Soft-Delete-, Archivierungs-, Retention- oder Cleanup-Infrastruktur existiert. Gleichzeitig verlangen die bisherigen Domain-Entscheidungen stabile externe Identitäten und dauerhafte Verbindungen zwischen persönlichen Imported Study Tasks und gemeinsamen External Learning Contents.

Grilling entschied unter anderem:

- Nach Ende der letzten aktiven Course Subscription wird der External Course inaktiv.
- Inaktive Kurse sind weder sichtbar noch scanbar.
- Persönliche externe Zugangsdaten werden sofort entfernt.
- Notwendige Identitäts- und Referenzhistorie bleibt erhalten.
- Vorübergehende Quelldaten erhalten eine konfigurierbare Schonfrist von zunächst 30 Tagen.
- Ein lokaler periodischer Cleanup setzt die Frist durch.
- Vollständige Löschung ist nur ohne aktive Subscription, laufenden Scan und persönliche Referenz erlaubt.
- Cleanup und Reaktivierung prüfen den Zustand atomar; eine neue gültige Subscription verhindert die Löschung.
- Cleanup ist pro Kurs atomar und idempotent.
- Reaktivierung benötigt einen neuen Zugriffsnachweis und einen frischen Scan.
- Beendete Subscriptions bleiben erhalten, solange persönliche Importhistorie sie benötigt.

Stark war, dass der Skill den Konflikt zwischen Datensparsamkeit, Deduplizierung und persönlicher Historie sichtbar machte. Schwach war, dass aus einem kleinen Mock-Feature zusätzliche Zustände, ein Cleanup-Job und Parallelitätsregeln entstanden. Die 30 Tage sind außerdem nur ein technischer Ausgangswert und keine rechtlich geprüfte Aufbewahrungsfrist.

Nach dieser Entscheidung wurde der vorherige Nebel konkret. Für Datenmodell, API-Vertrag sowie Akzeptanzkriterien und Testmatrix wurden drei neue, voneinander abhängige Wayfinder-Tickets erstellt.

Die Entscheidung wurde im GitHub-Ticket, im Domain-Glossar und im ADR `Docs/adr/0005-inaktive-externe-kurse-befristet-aufbewahren.md` dokumentiert.

#### Achter Entscheidungsdurchlauf

Das Ticket „Datenmodell und Persistenzregeln des Moodle-Vertikalschnitts festlegen“ übersetzte die zuvor getroffenen fachlichen Entscheidungen in ein relationales PostgreSQL-Modell.

Grilling entschied unter anderem:

- External Course, Course Subscription, Scan Run, Course Snapshot und External Learning Content werden getrennt gespeichert.
- Course Subscriptions besitzen die Zustände `Pending`, `Active` und `Ended`.
- Ein aktiver Kurs kann seinen aktuellen Snapshot für weitere Abonnenten wiederverwenden.
- Normalisierte Snapshot Items bewahren den vollständigen historischen Kurszustand.
- Ein gemeinsamer persönlicher Importzustand bildet `Imported` und `Dismissed` gegenseitig ausschließend ab.
- Source Updates überschreiben keine persönlichen Task-Daten.
- Zusammengesetzte Fremdschlüssel verhindern Verbindungen zwischen unterschiedlichen Kursen.
- Partielle Unique Indizes erlauben höchstens einen laufenden Scan und einen aktuellen Snapshot pro Kurs.
- PDF wird über Inhaltstyp und Medientyp erkannt, nicht durch eine eigene PDF-Tabelle.
- Externe Signaturen werden zentral und versioniert aus normalisierten Daten berechnet.
- Private Tokens und benutzerspezifische URLs dürfen nicht in gemeinsamen Tabellen gespeichert werden.
- Nach der Aufbewahrungsfrist bleiben bei persönlich referenzierten Inhalten nur stabile Identitäten erhalten.

Stark war, dass Grilling mehrere Widersprüche zwischen UI-Ablauf, Autorisierung und Reaktivierung sichtbar machte. Besonders wichtig war der neue Zustand `Pending`: Ohne ihn müsste eine fehlgeschlagene Registrierung entweder gelöscht oder ein noch nicht bestätigter Kurs vorzeitig freigegeben werden. Die Fragen führten außerdem zu Datenbankregeln, die Idempotenz und Mandantentrennung nicht nur der Anwendung überlassen.

Schwach war der sehr große Umfang von 35 Fragen. Einige Entscheidungen wie zusammengesetzte Fremdschlüssel, partielle Indizes und Signaturformat liegen bereits nahe an der Implementierung und können den ersten Mock-Schnitt komplizierter machen. Die Empfehlungen des Agents haben die Auswahl außerdem stark in Richtung eines robusten, aber aufwendigeren Modells gelenkt.

Die Entscheidung wurde im Domain-Glossar und im ADR `Docs/adr/0006-relationales-modell-fuer-kursimporte.md` dokumentiert. Die ADRs zur Autorisierung und Reaktivierung wurden wegen des neuen Zustands `Pending` präzisiert.

#### Neunter Entscheidungsdurchlauf

Das Ticket „API-Vertrag für Kursregistrierung und manuelle Scans festlegen“ übersetzte das Domain-Modell und den ausgewählten UI-Prototyp in einen minimalen, authentifizierten HTTP-Vertrag.

Grilling entschied unter anderem:

- Kursregistrierung und Scans werden ausschließlich über das persönliche Study Module angesprochen.
- Es gibt kein öffentliches Kursverzeichnis und keine API über frei eingebbare External-Course-IDs.
- Registrierung, Wiederverwendung und Reaktivierung erfolgen über einen idempotenten `PUT`-Endpunkt.
- Ein gemeinsamer laufender Scan wird wiederverwendet und mit `202 Accepted`, `Location` und `Retry-After` zurückgegeben.
- Nach seiner Erstellung gehört der Lebenszyklus eines Scan Runs dem Server und nicht mehr dem auslösenden HTTP-Request.
- Adapterfehler werden als sichere, persistierte Scan-Ergebnisse dargestellt.
- Technische Request- und Zustandsfehler verwenden stabile maschinenlesbare Fehlercodes.
- Fremde und unbekannte Ressourcen liefern einheitlich `404`, damit keine fremden Kursdaten offengelegt werden.
- Die Kursübersicht enthält nur persönliche Informationen, den letzten erfolgreichen Snapshot und höchstens zehn Scans seit der eigenen Aktivierung.
- Gemeinsame Inhaltszähler und persönliche Auswirkungen eines Scans werden getrennt dargestellt.
- Importierte Study Tasks erhalten optionale Quelleninformationen; persönliche Titel, Beschreibungen und Fälligkeiten werden nicht automatisch überschrieben.
- Für den Mock-Schnitt werden noch kein zusätzliches Rate Limit und kein `Idempotency-Key` eingeführt.

Stark war, dass der Skill Widersprüche zwischen gemeinsamem Scan, persönlicher Autorisierung, HTTP-Abbruch und UI-Polling sichtbar machte. Besonders wichtig war die Erkenntnis, dass das Schließen eines Browsers keinen Scan beenden darf, den auch andere Abonnenten verwenden.

Schwach war erneut der große Umfang von 36 Fragen. Einige Regeln wie stabile Fehlercodes, Retry-Header und Scan-Historiengrenzen liegen bereits nahe an der technischen Implementierung. Die ausführliche Spezifikation verbessert die Testbarkeit, erhöht aber den Aufwand des zunächst kleinen Mock-Vertikalschnitts.

Die Entkopplung gemeinsamer Scans vom auslösenden HTTP-Request wurde im ADR `Docs/adr/0007-gemeinsame-scans-vom-http-request-entkoppeln.md` dokumentiert.

### Grilling

Grilling wurde verwendet, um das Ziel und die Grenzen des ersten Schnitts festzulegen.

Entschieden wurde:

- Nur eine kontrollierte Mock-Moodle-Quelle wird verwendet.
- Der Scan wird zunächst manuell gestartet.
- PDF- und Nicht-PDF-Inhalte werden berücksichtigt.
- Ein externer Kurs wird einmal gescannt und von mehreren Benutzern abonniert.
- Aufgaben bleiben persönlich.
- Fälligkeitsdaten dürfen fehlen.
- Eine kleine Benutzeroberfläche gehört zum vertikalen Schnitt.
- Echte Benachrichtigungen bleiben zunächst außerhalb des Umfangs.

Stärke:

Grilling machte versteckte Annahmen sichtbar und zwang zu konkreten Entscheidungen.

Schwäche:

Viele Fragen hintereinander können anstrengend sein. Empfehlungen des Agents helfen, können die Entscheidung des Menschen aber auch beeinflussen.

### Weitere verwendete Skills

#### Prototype

Prototype wurde verwendet, um drei unterschiedliche UI-Abläufe mit lokalem Mock-Zustand direkt in der bestehenden Modulansicht vergleichbar zu machen.

Stärke:

Konkrete, bedienbare Varianten erleichtern Entscheidungen stärker als reine Beschreibungen. Besonders hilfreich war, dass der Benutzer gute Teile verschiedener Varianten kombinieren konnte.

Schwäche:

Ein Prototyp kann trotz Wegwerfcharakter viel Code und Prüfaufwand erzeugen. Seine Komponenten besitzen bewusst keine Produktionsqualität und dürfen nicht unverändert in die eigentliche Implementierung übernommen werden.

#### Domain Modeling

Domain Modeling trennte das persönliche Study Module klar vom gemeinsam gespeicherten External Course. Geklärte Begriffe und Beziehungen wurden sofort in `CONTEXT.md` festgehalten.

Für die schwer rückgängig zu machende Trennung interner und externer Identitäten wurde zusätzlich der ADR `Docs/adr/0001-interne-und-externe-identitaeten-trennen.md` erstellt.

Die Trennung gemeinsamer Quelldaten von persönlicher Planung wurde im ADR `Docs/adr/0002-externe-quelldaten-von-persoenlicher-planung-trennen.md` festgehalten.

Atomarität und Serialisierung von Kursscans wurden im ADR `Docs/adr/0003-kursscans-atomar-und-pro-kurs-serialisieren.md` dokumentiert.

Die Berechtigungsgrenze für gemeinsam gespeicherte Kurse wurde im ADR `Docs/adr/0004-gemeinsame-kurse-durch-subscriptions-autorisieren.md` festgehalten.

Die befristete Aufbewahrung und sichere Reaktivierung inaktiver Kurse wurde im ADR `Docs/adr/0005-inaktive-externe-kurse-befristet-aufbewahren.md` festgehalten.

Das relationale Modell für gemeinsame Kursdaten und persönliche Importzustände wurde im ADR `Docs/adr/0006-relationales-modell-fuer-kursimporte.md` dokumentiert.

Die Entkopplung gemeinsamer Scans vom auslösenden HTTP-Request wurde im ADR `Docs/adr/0007-gemeinsame-scans-vom-http-request-entkoppeln.md` festgehalten.

Stärke:

Die Sprache bleibt zwischen Dokumentation, Issues und späterem Code konsistent.

Schwäche:

Das sofortige Aktualisieren des Glossars unterbricht den Gesprächsfluss und erzeugt zusätzlichen Dokumentationsaufwand.

### Implementierung

Noch nicht begonnen.

## Vorläufige Bewertung

Wayfinder und Grilling haben den Umfang und die offenen Entscheidungen gut sichtbar gemacht. Eine endgültige Bewertung ist erst nach Spezifikation und Implementierung möglich.