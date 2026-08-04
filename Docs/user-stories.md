# User Stories – Study Organizer

## US-01: Registrierung

Als Student möchte ich ein Benutzerkonto erstellen, damit meine Daten
persönlich gespeichert werden können.

### Akzeptanzkriterien

- Der Benutzer gibt eine E-Mail-Adresse und ein Passwort ein.
- Die E-Mail-Adresse muss gültig sein.
- Die E-Mail-Adresse darf nur einmal registriert werden.
- Das Passwort muss die festgelegten Sicherheitsanforderungen erfüllen.
- Das Passwort wird niemals im Klartext gespeichert.
- Bei ungültigen Eingaben wird eine verständliche Fehlermeldung angezeigt.

---

## US-02: Anmeldung

Als registrierter Student möchte ich mich anmelden, damit ich auf meine
persönlichen Module und Aufgaben zugreifen kann.

### Akzeptanzkriterien

- Der Benutzer kann sich mit E-Mail-Adresse und Passwort anmelden.
- Falsche Anmeldedaten führen nicht zur Anmeldung.
- Nach erfolgreicher Anmeldung erhält der Benutzer Zugriff auf seine Daten.
- Der Benutzer kann sich wieder abmelden.

---

## US-03: Modul erstellen

Als Student möchte ich ein Modul erstellen, damit ich meine Aufgaben einem
Studienmodul zuordnen kann.

### Akzeptanzkriterien

- Ein angemeldeter Benutzer kann ein Modul erstellen.
- Ein Modul benötigt einen Namen.
- Modulcode, Beschreibung und Farbe sind optional.
- Das Modul wird dem angemeldeten Benutzer zugeordnet.
- Andere Benutzer können das Modul nicht sehen.

---

## US-04: Module verwalten

Als Student möchte ich meine Module anzeigen, bearbeiten und löschen, damit
meine Modulliste aktuell bleibt.

### Akzeptanzkriterien

- Der Benutzer sieht ausschließlich seine eigenen Module.
- Der Name und die optionalen Angaben können bearbeitet werden.
- Vor dem Löschen wird eine Bestätigung verlangt.
- Der Benutzer kann keine Module anderer Benutzer verändern.

---

## US-05: Aufgabe erstellen

Als Student möchte ich einem Modul eine Aufgabe zuordnen, damit ich meine
Arbeit organisieren kann.

### Akzeptanzkriterien

- Ein angemeldeter Benutzer kann eine Aufgabe erstellen.
- Die Aufgabe benötigt einen Titel und eine Frist.
- Eine Beschreibung ist optional.
- Die Aufgabe wird einem eigenen Modul zugeordnet.
- Der anfängliche Status ist `Offen`.

---

## US-06: Aufgaben verwalten

Als Student möchte ich meine Aufgaben anzeigen, bearbeiten und löschen, damit
meine Aufgabenliste aktuell bleibt.

### Akzeptanzkriterien

- Der Benutzer sieht ausschließlich seine eigenen Aufgaben.
- Aufgaben können nach Status angezeigt werden.
- Titel, Beschreibung und Frist können bearbeitet werden.
- Vor dem Löschen wird eine Bestätigung verlangt.
- Aufgaben anderer Benutzer können nicht verändert werden.

---

## US-07: Aufgabe abschließen

Als Student möchte ich eine erledigte Aufgabe als abgeschlossen markieren,
damit ich meinen aktuellen Fortschritt erkennen kann.

### Akzeptanzkriterien

- Eine offene Aufgabe kann als erledigt markiert werden.
- Eine erledigte Aufgabe kann wieder geöffnet werden.
- Der neue Status wird dauerhaft gespeichert.
- Die Änderung wird in der Benutzeroberfläche angezeigt.