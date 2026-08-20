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

Noch nicht durchgeführt.

### Implementierung

Noch nicht begonnen.

## Vorläufige Bewertung

Wayfinder und Grilling haben den Umfang und die offenen Entscheidungen gut sichtbar gemacht. Eine endgültige Bewertung ist erst nach Spezifikation und Implementierung möglich.