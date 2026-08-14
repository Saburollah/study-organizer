# User Stories

## US-01 – Konto registrieren

**Als** Gast \
**möchte ich** mich mit E-Mail-Adresse und sicherem Passwort registrieren, \
**damit** ich meine persönlichen Studiendaten speichern kann.

**Akzeptanzkriterien**

- Die E-Mail-Adresse muss gültig und eindeutig sein.
- Das Passwort erfüllt alle angezeigten Passwortregeln.
- Bei Erfolg wird das Konto erstellt.
- Fehler werden verständlich angezeigt.

## US-02 – Anmelden und Sitzung behalten

**Als** registrierter Benutzer \
**möchte ich** mich anmelden und nach einem Neuladen angemeldet bleiben, \
**damit** ich ohne wiederholte Anmeldung weiterarbeiten kann.

**Akzeptanzkriterien**

- Gültige Zugangsdaten liefern ein JWT.
- Ungültige Zugangsdaten liefern keine vertraulichen Details.
- Geschützte Seiten sind ohne gültige Sitzung nicht erreichbar.
- Eine noch gültige lokale Sitzung wird beim App-Start wiederhergestellt.

## US-03 – Abmelden

**Als** angemeldeter Benutzer \
**möchte ich** mich abmelden, \
**damit** meine Sitzung auf dem Gerät beendet wird.

**Akzeptanzkriterien**

- Token und lokale Sitzungsdaten werden entfernt.
- Geschützte Seiten sind danach nicht mehr erreichbar.

## US-04 – Profil verwalten

**Als** angemeldeter Benutzer \
**möchte ich** meine persönlichen Profildaten anzeigen und bearbeiten, \
**damit** mein Konto meine aktuellen Angaben enthält.

**Akzeptanzkriterien**

- E-Mail-Adresse, Vorname, Nachname, Geburtsdatum und Geschlecht werden angezeigt.
- Die E-Mail-Adresse ist nicht änderbar.
- Ein zukünftiges Geburtsdatum wird abgelehnt.
- Gespeicherte Änderungen sind nach einem Neuladen weiterhin vorhanden.

## US-05 – Passwort ändern

**Als** angemeldeter Benutzer \
**möchte ich** mein Passwort ändern, \
**damit** ich die Sicherheit meines Kontos kontrollieren kann.

**Akzeptanzkriterien**

- Das aktuelle Passwort muss korrekt sein.
- Das neue Passwort erfüllt dieselben Sicherheitsregeln wie bei der Registrierung.
- Das neue Passwort wird bestätigt.
- Nach Erfolg ist eine Anmeldung mit dem neuen Passwort möglich.

## US-06 – Lernmodul erstellen

**Als** angemeldeter Benutzer \
**möchte ich** ein Lernmodul anlegen, \
**damit** ich Aufgaben einem Fach oder einer Vorlesung zuordnen kann.

**Akzeptanzkriterien**

- Ein Name ist erforderlich.
- Kürzel, Beschreibung und Farbe sind optional.
- Das neue Modul erscheint in meiner Modulliste.
- Andere Benutzer können das Modul nicht sehen.

## US-07 – Lernmodule verwalten

**Als** angemeldeter Benutzer \
**möchte ich** eigene Lernmodule anzeigen, bearbeiten und löschen, \
**damit** meine Studienstruktur aktuell bleibt.

**Akzeptanzkriterien**

- Es werden nur eigene Module angezeigt.
- Änderungen werden dauerhaft gespeichert.
- Vor dem Löschen wird eine Bestätigung angezeigt.
- Beim Löschen werden zugehörige Aufgaben ebenfalls entfernt.

## US-08 – Aufgabe erstellen

**Als** angemeldeter Benutzer \
**möchte ich** einem Lernmodul eine Aufgabe mit Fälligkeit hinzufügen, \
**damit** ich meine Arbeit planen kann.

**Akzeptanzkriterien**

- Titel und Fälligkeitszeitpunkt sind erforderlich.
- Eine Beschreibung ist optional.
- Die Aufgabe gehört genau zu einem eigenen Lernmodul.
- Eine neue Aufgabe hat den Status `Open`.

## US-09 – Aufgaben verwalten

**Als** angemeldeter Benutzer \
**möchte ich** eigene Aufgaben anzeigen, bearbeiten und löschen, \
**damit** meine Planung aktuell bleibt.

**Akzeptanzkriterien**

- Aufgaben sind über das zugehörige Lernmodul erreichbar.
- Änderungen an Titel, Beschreibung und Fälligkeit werden gespeichert.
- Vor dem Löschen wird eine Bestätigung angezeigt.
- Fremde Aufgaben sind nicht erreichbar.

## US-10 – Aufgabenstatus pflegen

**Als** angemeldeter Benutzer \
**möchte ich** Aufgaben als erledigt oder wieder offen markieren, \
**damit** ich meinen tatsächlichen Arbeitsstand sehe.

**Akzeptanzkriterien**

- Der Status kann zwischen `Open` und `Completed` wechseln.
- Eine offene Aufgabe mit vergangener Fälligkeit wird als überfällig markiert.
- Der geänderte Status wird im Dashboard berücksichtigt.

## US-11 – Dashboard verwenden

**Als** angemeldeter Benutzer \
**möchte ich** eine Zusammenfassung und meine nächsten Aufgaben sehen, \
**damit** ich schnell erkenne, was als Nächstes wichtig ist.

**Akzeptanzkriterien**

- Das Dashboard zeigt die Anzahl der Module.
- Offene, überfällige und erledigte Aufgaben werden zusammengefasst.
- Die nächsten offenen Aufgaben werden nach Fälligkeit sortiert.
- Es werden ausschließlich eigene Daten verwendet.

## US-12 – Sprache wechseln

**Als** Benutzer \
**möchte ich** die Oberfläche auf Deutsch oder Englisch verwenden, \
**damit** ich die Anwendung in meiner bevorzugten Sprache bedienen kann.

**Akzeptanzkriterien**

- Die Sprache kann über die Flaggenauswahl gewechselt werden.
- Navigation, Formulare, Meldungen und Seiteninhalte werden übersetzt.
- Die Auswahl bleibt beim nächsten Besuch erhalten.
