# Beobachtungsprotokoll: Matt-Pocock-Skills

## Rahmen

- Projekt: Study Organizer
- Experiment-Branch: `experiment/matt`
- Startdatum: 20.08.2026
- Unveränderter Vergleichspunkt: `e7d8b5e`
- Matt-Konfiguration: `05d2609`
- Untersuchte Funktion: Einen External Course aus einer Mock-Moodle-Quelle registrieren und neue External Learning Contents als Imported Study Tasks bereitstellen

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

- Eine Course Subscription verwendet ein vorhandenes persönliches Study Module.
- Pro Study Module ist höchstens eine Course Subscription erlaubt.
- Ein Benutzer kann denselben External Course nur einmal abonnieren.
- Persönliche Moduldaten werden nicht durch externe Kursdaten überschrieben.
- Ein Kurswechsel benötigt ein bewusstes Beenden der bestehenden Course Subscription.

Die Entscheidung wurde als Kommentar dokumentiert, das Ticket geschlossen und in der Landkarte verlinkt. Die neu entdeckte Frage zum Lebenszyklus eines External Course ohne aktive Course Subscription wurde als eigenes Unter-Issue angelegt.

Positiv war die klare Konzentration auf genau eine Entscheidung. Nachteilig war der hohe Verwaltungsaufwand durch Zuweisung, Kommentar, Schließen, Aktualisieren der Landkarte und Erstellen eines Folgetickets.

#### Zweiter Entscheidungsdurchlauf

Das Ticket „Stabile Identität für externe Kurse und Inhalte festlegen“ wurde mit Wayfinder, Grilling und Domain Modeling bearbeitet.

Eine lesende Code-Erkundung stellte zuerst fest, dass das Projekt interne Guids verwendet, aber noch keine externen Schlüssel, Unique Constraints oder Datenbanktests für parallele Deduplizierung besitzt.

Grilling trennte anschließend:

- interne Study-Organizer-Identität,
- External Course Identity,
- External Content Key,
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
- Ende einer Course Subscription,
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
- Nur der Eigentümer eines Study Module darf dessen Course Subscription verwalten.
- Jeder Benutzer mit Course Subscription muss seinen externen Kurszugriff selbst nachweisen.
- Externe Zugangsdaten bleiben persönlich und werden nicht geteilt.
- Jeder Benutzer mit aktiver Course Subscription darf einen gemeinsamen Scan Run auslösen.
- Ein validierter Scan Run wird einmal für alle aktiven Course Subscriptions verarbeitet.
- Andere Benutzer mit Course Subscription, ihre Anzahl, Module, Aufgaben und der Scan-Auslöser bleiben verborgen.
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

Das Ticket „Lebenszyklus eines External Course ohne Abonnenten festlegen“ untersuchte die Aufbewahrung gemeinsam gespeicherter Kursdaten nach dem Ende der letzten Course Subscription.

Die Code-Erkundung zeigte, dass Module und Aufgaben bisher hart gelöscht werden, Module ihre Aufgaben per Cascade entfernen und noch keine Soft-Delete-, Archivierungs-, Retention- oder Cleanup-Infrastruktur existiert. Gleichzeitig verlangen die bisherigen Domain-Entscheidungen stabile externe Identitäten und dauerhafte Verbindungen zwischen persönlichen Imported Study Tasks und gemeinsamen External Learning Contents.

Grilling entschied unter anderem:

- Nach Ende der letzten aktiven Course Subscription wird der External Course inaktiv.
- Inaktive Kurse sind weder sichtbar noch scanbar.
- Persönliche externe Zugangsdaten werden sofort entfernt.
- Notwendige Identitäts- und Referenzhistorie bleibt erhalten.
- Vorübergehende Quelldaten erhalten eine konfigurierbare Schonfrist von zunächst 30 Tagen.
- Ein lokaler periodischer Cleanup setzt die Frist durch.
- Vollständige Löschung ist nur ohne aktive Course Subscription, laufenden Scan Run und persönliche Referenz erlaubt.
- Cleanup und Reaktivierung prüfen den Zustand atomar; eine neue gültige Course Subscription verhindert die Löschung.
- Cleanup ist pro Kurs atomar und idempotent.
- Reaktivierung benötigt einen neuen Zugriffsnachweis und einen frischen Scan.
- Beendete Course Subscriptions bleiben erhalten, solange persönliche Importhistorie sie benötigt.

Stark war, dass der Skill den Konflikt zwischen Datensparsamkeit, Deduplizierung und persönlicher Historie sichtbar machte. Schwach war, dass aus einem kleinen Mock-Feature zusätzliche Zustände, ein Cleanup-Job und Parallelitätsregeln entstanden. Die 30 Tage sind außerdem nur ein technischer Ausgangswert und keine rechtlich geprüfte Aufbewahrungsfrist.

Nach dieser Entscheidung wurde der vorherige Nebel konkret. Für Datenmodell, API-Vertrag sowie Akzeptanzkriterien und Testmatrix wurden drei neue, voneinander abhängige Wayfinder-Tickets erstellt.

Die Entscheidung wurde im GitHub-Ticket, im Domain-Glossar und im ADR `Docs/adr/0005-inaktive-externe-kurse-befristet-aufbewahren.md` dokumentiert.

#### Achter Entscheidungsdurchlauf

Das Ticket „Datenmodell und Persistenzregeln des Moodle-Vertikalschnitts festlegen“ übersetzte die zuvor getroffenen fachlichen Entscheidungen in ein relationales PostgreSQL-Modell.

Grilling entschied unter anderem:

- External Course, Course Subscription, Scan Run, Course Snapshot und External Learning Content werden getrennt gespeichert.
- Course Subscriptions besitzen die Zustände `Pending`, `Active` und `Ended`.
- Ein aktiver External Course kann seinen aktuellen Course Snapshot für weitere Benutzer mit neuer Course Subscription wiederverwenden.
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

Stark war, dass der Skill Widersprüche zwischen gemeinsamem Scan, persönlicher Autorisierung, HTTP-Abbruch und UI-Polling sichtbar machte. Besonders wichtig war die Erkenntnis, dass das Schließen eines Browsers keinen Scan Run beenden darf, den auch andere Benutzer mit aktiver Course Subscription verwenden.

Schwach war erneut der große Umfang von 36 Fragen. Einige Regeln wie stabile Fehlercodes, Retry-Header und Scan-Historiengrenzen liegen bereits nahe an der technischen Implementierung. Die ausführliche Spezifikation verbessert die Testbarkeit, erhöht aber den Aufwand des zunächst kleinen Mock-Vertikalschnitts.

Die Entkopplung gemeinsamer Scans vom auslösenden HTTP-Request wurde im ADR `Docs/adr/0007-gemeinsame-scans-vom-http-request-entkoppeln.md` dokumentiert.

#### Zehnter Entscheidungsdurchlauf

Das Ticket „Akzeptanzkriterien und Testmatrix des Moodle-Vertikalschnitts festlegen“ verdichtete die vorausgehenden Entscheidungen zu beobachtbaren Abnahmekriterien.

Die Matrix ordnete jede normative Regel der engsten sinnvollen Nachweisebene zu: Domain-Test, PostgreSQL-Integrationstest, API-Test, Frontend-Test oder genau ein Playwright-Golden-Path. Sie verlangte keine pauschale Coverage-Zahl, sondern einen konkreten automatisierten Nachweis pro Regel. Für Zeit und Parallelität wurden eine injizierbare Uhr, deterministische Mock-Barrieren und echte PostgreSQL-Tests ohne Sleeps festgelegt.

Stark war die gemeinsame Abnahmegrenze für alle späteren Implementierungstickets. Der getrennte Review konnte dadurch nicht nur Codequalität, sondern auch fehlende fachliche Nachweise finden. Schwach war der Umfang: Die Matrix enthielt 31 Zeilen und machte den Mock-Schnitt deutlich größer als einen einfachen Demonstrator.

Die bestätigte Auflösung wurde als Kommentar in GitHub-Issue `#77` veröffentlicht. Sie war anschließend die normative Spezifikation für Tickets, Implementierung und Review.

### Grilling

Grilling wurde verwendet, um das Ziel und die Grenzen des ersten Schnitts festzulegen.

Entschieden wurde:

- Nur eine kontrollierte Mock-Moodle-Quelle wird verwendet.
- Der Scan wird zunächst manuell gestartet.
- PDF- und Nicht-PDF-Inhalte werden berücksichtigt.
- Ein External Course wird durch einen gemeinsamen Scan Run verarbeitet und von mehreren Benutzern über eigene Course Subscriptions abonniert.
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

Die Spezifikation wurde in sechs vertikale Umsetzungstickets zerlegt und in Abhängigkeitsreihenfolge implementiert:

- `#79` / PR `#81`: PostgreSQL- und Mock-Testinfrastruktur,
- `#82` / PR `#83`: Domainmodell und relationales Schema,
- `#84` / PR `#85`: Scan-Orchestrierung und Importverarbeitung,
- `#86` / PR `#87`: API-Vertrag, Authentifizierung und Datenschutz,
- `#88` / PR `#89`: Registrierungs- und Scan-Oberfläche,
- `#91` / PR `#92`: Playwright-Golden-Path.

Die Implementierung arbeitete testgetrieben. Besonders wertvoll waren echte PostgreSQL-Tests für Unique Constraints, atomare Scanübernahme, Idempotenz, Parallelität und Berechtigungsgrenzen. Der Mock-Adapter stellte mehrere Inhaltsversionen, kontrollierte Fehler und Synchronisationsbarrieren bereit. Damit konnten PDF-, Link- und Activity-Inhalte, Umbenennungen, Verschwinden und Wiedererscheinen sowie parallele Scan-Anforderungen reproduzierbar geprüft werden.

Nach dem ersten produktiven Deployment wurde eine Lücke außerhalb des fachlichen Codes sichtbar: Das Backend war deployt, aber die Produktionsdatenbank noch nicht migriert. Issue `#90` und PR `#93` ergänzten deshalb ein idempotentes EF-Migrationsbundle, das vor dem API-Start läuft und den Start bei einem Migrationsfehler verhindert. Dieser Korrekturdurchlauf zeigt, dass die ursprüngliche Feature-Matrix den Deployment-Übergang nicht ausreichend erfasste.

### Review und Korrekturen

`code-review` prüfte den Stand ab dem unveränderten Vergleichspunkt `e7d8b5e` getrennt gegen Repository-Standards und gegen die Spezifikation aus Issue `#77`.

Der Standards-Review fand zwei klare Begriffsabweichungen:

- Ergebnis- und API-Typen verwendeten `Course Scan` als Synonym für den kanonischen Fachbegriff `Scan Run`.
- Das rohe, noch nicht validierte Adapterergebnis hieß `Course Source Snapshot`, obwohl `Course Snapshot` im Domainmodell den validierten und persistierten Kurszustand bezeichnet.

Beide Abweichungen wurden korrigiert. Der Vertrag verwendet jetzt `ScanRun...` für persistierte Läufe und `ExternalCourseSourcePayload` für rohe Adapterdaten. Eine doppelte Hilfsimplementierung zum Kopieren von Inhaltssignaturen wurde ebenfalls durch `ContentSignature.Copy()` vereinheitlicht.

Der finale Standards-Re-Review fand noch lokale `snapshot`-Bezeichner für rohe Adapterdaten und einen fehlenden Glossarbegriff für bereinigte Metadaten. Die lokalen Bezeichner wurden auf `sourcePayload` vereinheitlicht; `Metadata-Purged External Learning Content` wurde in `CONTEXT.md` ergänzt.

Als begründete spätere Refactoring-Möglichkeiten blieben zwei große Infrastruktur-Handler und ein wiederkehrendes Metadaten-Datenbündel bestehen. Ihre Aufteilung hätte den aktuellen Korrekturumfang deutlich erweitert, ohne ein fehlendes Akzeptanzkriterium zu schließen.

Der Spezifikations-Review fand einen schwerwiegenden Abschlussfehler: Obwohl Haupt-Issue `#78` geschlossen war, fehlten der produktive 30-Tage-Cleanup und der geforderte Nachweis für das Rennen zwischen Cleanup und Reaktivierung. Damit waren zwei Matrixzeilen beim ersten Review noch nicht erfüllt. Issue `#78` wurde deshalb transparent wieder geöffnet.

Die Korrektur ergänzt:

- einen konfigurierbaren periodischen Cleanup mit einer anfänglichen Schonfrist von 30 Tagen,
- Transaktionen und PostgreSQL-Zeilensperren für die atomare Prüfung pro External Course,
- vollständige Löschung nur ohne persönliche Referenzen,
- Reduktion referenzierter External Learning Contents auf stabile Identität statt Verlust persönlicher Historie,
- idempotente Wiederholung ohne Adapteraufruf,
- sowie einen deterministischen PostgreSQL-Test, in dem eine gleichzeitig bestätigte Reaktivierung die Löschung verhindert.

Der neue Cleanup wurde mit vier gezielten Integrationstests testgetrieben entwickelt. Der vollständige Backend-Abschlusslauf enthält 225 erfolgreiche Tests: 111 Domain-, 63 API- und 51 Infrastructure-Tests.

Im finalen Standards-Re-Review wurden zwei weitere P2-Abweichungen gefunden und behoben: lokale Rohdatenbezeichner wurden vollständig auf `sourcePayload` vereinheitlicht und der neue Zustand `Metadata-Purged External Learning Content` im Glossar ergänzt. Der parallel gestartete finale Spezifikationsagent konnte wegen eines Nutzungslimits nicht antworten. Der Agent führte den erneuten Abgleich deshalb selbst Zeile für Zeile gegen die bestätigte Matrix durch; dabei blieb keine weitere statische Spezifikationsabweichung offen. Diese fehlende unabhängige zweite Re-Review-Antwort ist eine Einschränkung des Abschlusses und wird nicht verschwiegen.

Die Frontend-Typprüfung lief ohne Fehler. Ein erster Vitest-Lauf in der Agentenumgebung scheiterte nicht an einem Test, sondern bereits beim Lesen von Dateien mit `ETIMEDOUT`. Ursache war der macOS File Provider, der den unter `Documents` liegenden Projektordner zeitweise nicht vollständig bereitstellte. Nachdem die Dateien wieder lokal verfügbar waren, bestätigte der Benutzer am 27.08.2026 erfolgreiche Abschlussläufe von `pnpm exec vitest run`, `pnpm lint` und `pnpm build`. Damit blieb kein ungeklärter Frontend-Test-, Lint- oder Buildfehler offen.

## Matt-Bewertung

### Agentenseitige Auswertung

Matt Pococks Suite war besonders stark bei früher Anforderungsklärung, nachvollziehbaren Entscheidungen und der Trennung von Standards- und Spezifikationsreview. Wayfinder, Grilling, Prototype und Domain Modeling machten versteckte Fragen zu Identität, gemeinsamem Scan, persönlicher Historie, Datenschutz und Parallelität sichtbar, bevor sie zu schwer änderbarem Produktionscode wurden. Die Akzeptanzmatrix ermöglichte außerdem, eine trotz grüner Teilprüfungen übersehene Cleanup-Anforderung im Abschlussreview konkret nachzuweisen.

Die größten Schwächen waren Prozessumfang und Steuerungsaufwand. Zahlreiche Fragerunden, Issues, Auflösungskommentare und ADR-Aktualisierungen erzeugten für einen Mock-Vertikalschnitt viel Verwaltung. Technisch überzeugende Empfehlungen des Agents konnten die Auswahl stark in Richtung eines robusten, aber großen Designs lenken. Außerdem verhinderte der Workflow nicht automatisch, dass ein Haupt-Issue zu früh geschlossen wurde; erst der getrennte Abschlussreview fand die Lücke.

### Bewertung durch den Benutzer

Der Benutzer vergab am 27.08.2026 folgende Bewertungen auf der Skala von 1 bis 5:

| Kriterium | Wert | Konkretes Beispiel aus dem Versuch |
| --- | ---: | --- |
| Verständlichkeit | 4 | Wayfinder, ADRs und Akzeptanzmatrix machten die schrittweise entstandenen Entscheidungen nachlesbar. |
| Kontrolle | 4 | Der Benutzer traf die Produktentscheidungen in den Grilling-Runden selbst und korrigierte unter anderem eine versehentlich gewählte Prototyp-Antwort. |
| Lerngewinn | 5 | Der Versuch führte von der dynamischen Moodle-Struktur über Adapter und stabile Identitäten bis zu gemeinsamem Scannen und persönlicher Projektion. |
| Angemessener Aufwand | 3 | Neun Entscheidungs-Grillings, zahlreiche Issues und mehrere Review-Runden erzeugten für den Mock-Vertikalschnitt spürbaren Prozessaufwand. |
| Vertrauen | 4 | 225 Backend-Tests sowie erfolgreiche Frontend-Tests, Lint- und Buildläufe lieferten technische Nachweise; die unabhängige finale Spezifikationsantwort fiel wegen eines Nutzungslimits aus. |
| Wiederaufnahme | 4 | Issues, ADRs, `CONTEXT.md` und Beobachtungsprotokoll ermöglichten die Fortsetzung über viele getrennte Agentensitzungen hinweg. |
| Anpassbarkeit | 4 | Die Skills ließen sich mit GitHub Issues, den vorhandenen Repository-Regeln, PostgreSQL-Tests und dem Mock-Adapter verbinden. |

Die Beispiele dokumentieren beobachtete Vorgänge des Versuchs; die Zahlen stammen ausschließlich vom Benutzer.
