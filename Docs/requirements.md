# Anforderungen an Study Organizer

## 1. Funktionale Anforderungen

### Benutzerkonto und Sicherheit

- **FA-01 Registrierung:** Ein Gast kann ein Konto mit einer eindeutigen E-Mail-Adresse und einem sicheren Passwort erstellen.
- **FA-02 Anmeldung:** Ein registrierter Benutzer kann sich mit E-Mail-Adresse und Passwort anmelden und erhält ein zeitlich begrenztes JWT.
- **FA-03 Abmeldung:** Ein angemeldeter Benutzer kann seine lokale Sitzung beenden.
- **FA-04 Sitzungswiederherstellung:** Eine noch gültige Anmeldung bleibt nach dem Neuladen des Webbrowsers erhalten.
- **FA-05 Passwortregeln:** Passwörter müssen mindestens 15 Zeichen, Groß- und Kleinbuchstaben, eine Ziffer und ein Sonderzeichen enthalten.
- **FA-06 Passwort ändern:** Ein angemeldeter Benutzer kann sein Passwort nach Eingabe des aktuellen Passworts ändern.
- **FA-07 Datenschutz:** Ein Benutzer darf ausschließlich eigene Lernmodule, Aufgaben und Profildaten lesen oder verändern.

### Persönliches Profil

- **FA-08 Profil anzeigen:** Ein angemeldeter Benutzer kann E-Mail-Adresse, Vorname, Nachname, Geburtsdatum und Geschlecht anzeigen.
- **FA-09 Profil bearbeiten:** Vorname, Nachname, Geburtsdatum und Geschlecht können geändert werden.
- **FA-10 E-Mail schützen:** Die E-Mail-Adresse wird im Profil angezeigt, ist in der aktuellen Version aber nicht änderbar.

### Lernmodule

- **FA-11 Lernmodul erstellen:** Ein Benutzer kann ein Lernmodul mit Name sowie optional Kürzel, Beschreibung und Farbe erstellen.
- **FA-12 Lernmodule anzeigen:** Ein Benutzer kann alle eigenen Lernmodule anzeigen.
- **FA-13 Lernmodul bearbeiten:** Ein Benutzer kann die Daten eines eigenen Lernmoduls ändern.
- **FA-14 Lernmodul löschen:** Ein Benutzer kann ein eigenes Lernmodul nach Bestätigung löschen. Zugehörige Aufgaben werden ebenfalls gelöscht.

### Aufgaben

- **FA-15 Aufgabe erstellen:** Ein Benutzer kann in einem eigenen Lernmodul eine Aufgabe mit Titel, Fälligkeit und optionaler Beschreibung erstellen.
- **FA-16 Aufgaben anzeigen:** Ein Benutzer kann die Aufgaben eines eigenen Lernmoduls anzeigen.
- **FA-17 Aufgabe bearbeiten:** Ein Benutzer kann Titel, Beschreibung und Fälligkeit einer eigenen Aufgabe ändern.
- **FA-18 Aufgabenstatus ändern:** Ein Benutzer kann eine Aufgabe zwischen `Open` und `Completed` umschalten.
- **FA-19 Aufgabe löschen:** Ein Benutzer kann eine eigene Aufgabe nach Bestätigung löschen.
- **FA-20 Überfälligkeit:** Eine offene Aufgabe mit vergangener Fälligkeit wird als überfällig gekennzeichnet.

### Dashboard und Sprache

- **FA-21 Dashboard:** Ein angemeldeter Benutzer sieht Anzahlen der Lernmodule sowie offenen, überfälligen und erledigten Aufgaben.
- **FA-22 Nächste Aufgaben:** Das Dashboard zeigt die zeitlich nächsten offenen Aufgaben des Benutzers.
- **FA-23 Sprachauswahl:** Die Weboberfläche kann zwischen Deutsch und Englisch umgeschaltet werden.
- **FA-24 Sprachspeicherung:** Die gewählte Sprache bleibt lokal für den nächsten Besuch erhalten.

## 2. Nichtfunktionale Anforderungen

- **NFA-01 Sicherheit:** Passwörter werden ausschließlich durch ASP.NET Core Identity gehasht gespeichert.
- **NFA-02 Authentifizierung:** Geschützte API-Endpunkte verlangen ein gültiges, signiertes JWT mit geprüftem Aussteller, Empfänger und Ablaufzeitpunkt.
- **NFA-03 Geheimnisse:** Datenbankpasswort und JWT-Schlüssel dürfen nicht in Git gespeichert werden.
- **NFA-04 Zugriffsschutz:** Datenbankabfragen berücksichtigen immer die Benutzer-ID des angemeldeten Benutzers.
- **NFA-05 Validierung:** Ungültige Eingaben liefern verständliche Validierungsfehler und verändern keine Daten.
- **NFA-06 Persistenz:** Daten werden dauerhaft in PostgreSQL gespeichert; lokale Containerdaten liegen in einem Docker-Volume.
- **NFA-07 Wartbarkeit:** Backend-Code ist in API-, Application-, Domain- und Infrastructure-Schicht getrennt.
- **NFA-08 Erweiterbarkeit:** Weitere Clients wie eine iOS-App können dieselbe HTTP-API verwenden.
- **NFA-09 Testbarkeit:** Zentrale Domänenregeln, Endpunkte, Services, Stores und Views besitzen automatisierte Tests.
- **NFA-10 Kompatibilität:** Das Webfrontend funktioniert in aktuellen Versionen verbreiteter Browser.
- **NFA-11 Responsive Design:** Die Benutzeroberfläche passt sich Desktop-, Tablet- und mobilen Bildschirmgrößen an.
- **NFA-12 Internationalisierung:** Sichtbare Standardtexte sind über Vue I18n auf Deutsch und Englisch verfügbar.
- **NFA-13 Reproduzierbarkeit:** Die lokale PostgreSQL-Umgebung kann mit Docker Compose gestartet werden.
- **NFA-14 Beobachtbarkeit:** Die API stellt einen Health-Check unter `/health` und im Entwicklungsmodus eine Swagger-Oberfläche bereit.

## 3. Systemgrenzen

### Bestandteil der aktuellen Version

- Vue-Webfrontend
- ASP.NET-Core-REST-API
- ASP.NET Core Identity und JWT
- PostgreSQL mit Entity Framework Core
- deutsche und englische Benutzeroberfläche

### Geplant, aber noch nicht implementiert

- native iOS-App
- Push-Benachrichtigungen
- Notizen und Dateianhänge
- Stundenplan und Kalenderintegration
- Zusammenarbeit in Lerngruppen
