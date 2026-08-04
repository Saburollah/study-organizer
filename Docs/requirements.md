# Anforderungen – Study Organizer

## 1. Zweck

Dieses Dokument beschreibt die Anforderungen an die erste Version des
Study Organizers.

## 2. Zielgruppe

Die Anwendung richtet sich an Studierende, die ihre Module und Aufgaben
zentral organisieren möchten.

## 3. Funktionale Anforderungen

### Benutzerverwaltung

- **FA-01:** Das System muss die Registrierung eines Benutzers ermöglichen.
- **FA-02:** Das System muss die Anmeldung eines registrierten Benutzers ermöglichen.
- **FA-03:** Das System muss die Abmeldung eines angemeldeten Benutzers ermöglichen.
- **FA-04:** Ein Benutzer darf ausschließlich seine eigenen Daten sehen und bearbeiten.

### Modulverwaltung

- **FA-05:** Ein Benutzer muss ein Modul erstellen können.
- **FA-06:** Ein Benutzer muss seine Module anzeigen können.
- **FA-07:** Ein Benutzer muss ein eigenes Modul bearbeiten können.
- **FA-08:** Ein Benutzer muss ein eigenes Modul löschen können.

### Aufgabenverwaltung

- **FA-09:** Ein Benutzer muss eine Aufgabe für ein Modul erstellen können.
- **FA-10:** Eine Aufgabe muss einen Titel, einen Status und eine Frist besitzen.
- **FA-11:** Eine Aufgabe kann eine optionale Beschreibung besitzen.
- **FA-12:** Ein Benutzer muss eine eigene Aufgabe bearbeiten können.
- **FA-13:** Ein Benutzer muss eine eigene Aufgabe löschen können.
- **FA-14:** Ein Benutzer muss eine Aufgabe als erledigt markieren können.
- **FA-15:** Das System muss offene und erledigte Aufgaben getrennt anzeigen können.

## 4. Nichtfunktionale Anforderungen

### Sicherheit

- **NFA-01:** Passwörter dürfen nicht im Klartext gespeichert werden.
- **NFA-02:** Persönliche Daten dürfen nur nach erfolgreicher Anmeldung verfügbar sein.
- **NFA-03:** Eingaben müssen im Frontend und Backend validiert werden.
- **NFA-04:** Die Kommunikation mit der API muss in der Produktionsumgebung über HTTPS erfolgen.

### Bedienbarkeit

- **NFA-05:** Die Benutzeroberfläche soll ohne besondere technische Kenntnisse bedienbar sein.
- **NFA-06:** Fehlermeldungen sollen verständlich formuliert sein.

### Wartbarkeit und Qualität

- **NFA-07:** Backend, Web-Frontend und iOS-App müssen getrennte Komponenten bilden.
- **NFA-08:** Die API muss mit OpenAPI beziehungsweise Swagger dokumentiert werden.
- **NFA-09:** Zentrale Backend-Funktionen müssen durch automatisierte Tests geprüft werden.

### Kompatibilität

- **NFA-10:** Das Backend muss von der Webanwendung und der iOS-App verwendet werden können.
- **NFA-11:** Die Daten müssen in einer relationalen SQL-Datenbank gespeichert werden.

## 5. Abgrenzung der ersten Version

Folgende Funktionen sind nicht Bestandteil der ersten Version:

- Push-Benachrichtigungen
- Lernstatistiken
- Stundenplan
- Datei-Uploads
- Zusammenarbeit zwischen mehreren Studierenden