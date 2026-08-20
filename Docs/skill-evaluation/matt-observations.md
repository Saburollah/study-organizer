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

#### Domain Modeling

Domain Modeling trennte das persönliche Study Module klar vom gemeinsam gespeicherten External Course. Geklärte Begriffe und Beziehungen wurden sofort in `CONTEXT.md` festgehalten.

Für die schwer rückgängig zu machende Trennung interner und externer Identitäten wurde zusätzlich der ADR `Docs/adr/0001-interne-und-externe-identitaeten-trennen.md` erstellt.

Stärke:

Die Sprache bleibt zwischen Dokumentation, Issues und späterem Code konsistent.

Schwäche:

Das sofortige Aktualisieren des Glossars unterbricht den Gesprächsfluss und erzeugt zusätzlichen Dokumentationsaufwand.

### Implementierung

Noch nicht begonnen.

## Vorläufige Bewertung

Wayfinder und Grilling haben den Umfang und die offenen Entscheidungen gut sichtbar gemacht. Eine endgültige Bewertung ist erst nach Spezifikation und Implementierung möglich.