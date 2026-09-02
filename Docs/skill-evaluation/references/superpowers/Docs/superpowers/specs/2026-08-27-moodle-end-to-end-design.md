# Entwurf: Lokaler Moodle-End-to-End-Schnitt

## Status

Dieser Entwurf beschreibt den vom Benutzer bestätigten ersten vertikalen Schnitt der Moodle-Integration. Er ist eine ausführbare fachliche und technische Spezifikation, aber noch kein Implementierungsplan. Produktcode, reale Moodle-Zugänge, Scheduler und Benachrichtigungen sind nicht Bestandteil dieses Entwurfs.

## Ziel

Ein angemeldeter Benutzer registriert in der Weboberfläche einen bekannten Mock-Moodle-Kurs, startet einen manuellen Scan und sieht die normalisierten Kursinhalte. Ein als Aufgabe erkannter Inhalt mit strukturierter Abgabefrist erzeugt pro Abonnent genau eine persönliche Aufgabe im automatisch angelegten Lernmodul. Wiederholte Scans, mehrere Links zum selben Kurs und mehrere Abonnenten erzeugen weder doppelte externe Kurse noch doppelte Aufgaben.

Der Schnitt beweist den vollständigen Ablauf von der Registrierung bis zur sichtbaren Aufgabe mit reproduzierbaren Daten. Er testet die risikoreichen Domänenregeln, ohne von Zugangsdaten oder der Verfügbarkeit einer echten Moodle-Installation abhängig zu sein.

## Bestätigter Umfang

Der erste Schnitt umfasst:

- Registrierung eines bekannten Mock-Kurslinks in der Vue-Oberfläche;
- Discovery einer stabilen externen Kursidentität;
- einen gemeinsam gespeicherten externen Kurs mit persönlichen Abonnements;
- ein automatisch angelegtes persönliches Lernmodul je Abonnement;
- einen manuell ausgelösten gemeinsamen Scan;
- normalisierte Kursinhalte mit stabilen externen IDs;
- Snapshot-Vergleich, Änderungserkennung und Idempotenz;
- automatische persönliche Aufgaben bei strukturierter Aufgabenfrist;
- den sichtbaren Zustand `Prüfung erforderlich` für unsichere Inhalte;
- Synchronisierung offener Moodle-gesteuerter Aufgaben;
- sichere Behandlung ungültiger oder fehlgeschlagener Scans;
- deutsche und englische UI-Texte;
- automatisierte Tests und eine manuelle sichtbare Abnahme.

## Ausgeschlossener Umfang

Nicht Bestandteil dieses Schnitts sind:

- echte Moodle Web Services, HTML-Scraping oder Moodle-Plugins;
- Speicherung oder Verarbeitung realer Moodle-Zugangsdaten;
- Single Sign-on, OAuth oder Web-Service-Token;
- Scheduler, Hintergrundjobs und tägliches Polling;
- E-Mail-, Push- oder In-App-Benachrichtigungen;
- LLM-gestützte Erkennung, Freitext-Datumsanalyse oder Confidence-Schwellen;
- eine Benutzeraktion zum Bestätigen ungeklärter Inhalte;
- Abonnementlöschung;
- automatisches Löschen persönlicher Aufgaben bei verschwundenen Inhalten;
- ein benutzerseitiger Schalter zum Manipulieren der Mock-Zustände;
- die native iOS-Anwendung.

## Architektur

Der Schnitt verwendet ein explizites relationales Domänenmodell. Die gemeinsamen externen Daten werden von den persönlichen Lernmodulen und Aufgaben getrennt.

```text
ExternalCourse
├── ExternalContent
├── ScanRun
└── CourseSubscription
    ├── persönliches StudyModule
    └── ExternalTaskLink ──> persönliche StudyTask
```

### Schichten und Verantwortlichkeiten

- **Domain:** Entitäten, Identitäts- und Statusregeln sowie ein deterministischer Snapshot-Vergleich.
- **Application:** Adapter-Port, Registrierungs- und Scan-Orchestrierung sowie Anwendungsresultate.
- **Infrastructure:** EF-Core-Persistenz, eindeutige Datenbank-Constraints und der deterministische Mock-Adapter.
- **API:** Authentifizierte Minimal-API-Endpunkte, Autorisierung über das persönliche Abonnement und sichere Fehlerabbildung.
- **Frontend:** Kursregistrierung, Scanaktion, Inhaltszustände und Verlinkung zum persönlichen Modul.

Der bestehende Schichtenaufbau des Study Organizers bleibt erhalten. Reale Moodle-Adapter können später hinter denselben Application-Port treten, gehören aber nicht zu diesem Schnitt.

## Domänenmodell

### ExternalCourse

`ExternalCourse` repräsentiert einen externen Kurs genau einmal. Seine fachliche Eindeutigkeit ist die Kombination aus `ProviderKey` und `ExternalCourseId`. Der vom Benutzer eingegebene Link ist nur ein Discovery-Eingang und nicht der Kursschlüssel.

Gespeichert werden mindestens interne ID, Provider-Schlüssel, externe Kurs-ID, erkannter Kursname, Scanstatus und Zeitstempel des letzten erfolgreichen Scans. Für den Mock werden keine Zugangsdaten oder Sitzungsinformationen benötigt.

Die Datenbank erzwingt einen eindeutigen Index auf `ProviderKey + ExternalCourseId`.

### CourseSubscription

`CourseSubscription` verbindet genau einen Benutzer mit einem `ExternalCourse` und einem persönlichen `StudyModule`. Beim ersten Abonnement wird dieses Modul mit dem erkannten Kursnamen angelegt.

Die Datenbank erzwingt:

- höchstens ein Abonnement je `OwnerId + ExternalCourseId`;
- genau ein persönliches Modul je Abonnement;
- Autorisierung aller Lese- und Scanoperationen über den Besitzer des Abonnements.

Mehrere Benutzer teilen den externen Kurs und seine Scanergebnisse, besitzen aber getrennte Module und Aufgaben.

Das automatisch angelegte Modul bleibt persönlich: Name, Code, Beschreibung und Farbe dürfen mit den bestehenden Modulaktionen angepasst werden. Solange das Modul mit einem aktiven `CourseSubscription` verbunden ist, darf es jedoch nicht gelöscht werden. API und UI weisen den Löschversuch als Konflikt zurück. Eine spätere Abonnementlöschung benötigt eine eigene Entscheidung und ist in diesem Schnitt ausgeschlossen.

### ExternalContent

`ExternalContent` repräsentiert den zuletzt erfolgreich erkannten Zustand eines Kursinhalts. Seine fachliche Eindeutigkeit ist `ExternalCourseId + ProviderContentId`.

Gespeichert werden mindestens:

- normalisierter Inhaltstyp;
- Titel und optionale Beschreibung;
- sicherer externer Quelllink;
- optionale strukturierte Frist;
- Verarbeitungsstatus und gegebenenfalls Prüfgrund;
- Sichtbarkeitsstatus;
- Zeitpunkt der letzten erfolgreichen Beobachtung.

Titel, Position und Link dürfen sich ändern, ohne eine neue Inhaltsidentität zu erzeugen. Die aktuellen `ExternalContent`-Datensätze bilden den letzten erfolgreichen Snapshot ab.

Strukturierte Fristen werden als UTC-Zeitpunkt normalisiert. Externe Quelllinks müssen ein zulässiges HTTP- oder HTTPS-Schema besitzen; andere Schemata machen den Snapshot ungültig.

### ExternalTaskLink

`ExternalTaskLink` verbindet ein `ExternalContent`, ein `CourseSubscription` und die daraus erzeugte persönliche `StudyTask`. Ein eindeutiger Index auf `CourseSubscriptionId + ExternalContentId` verhindert eine zweite Aufgabe für dieselbe Kombination.

Die Verknüpfung bleibt auch erhalten, wenn eine Aufgabe erledigt ist, der Inhalt später verschwindet oder seine Frist unsicher wird.

### ScanRun

`ScanRun` dokumentiert Beginn, Ende, Ergebnis und einen sicheren Fehlercode eines manuellen Abrufs. Protokolliert werden keine Tokens, Sitzungsdaten oder vollständigen vertraulichen Kursinhalte.

Ein fehlgeschlagener Lauf darf als Diagnosemetadatum gespeichert werden, verändert aber weder den letzten erfolgreichen Inhaltszustand noch persönliche Aufgaben.

## Adapter-Port und Mock

Der Application-Port bietet zwei Operationen:

1. Discovery eines eingegebenen Kurslinks;
2. Abruf eines vollständigen normalisierten Snapshots für eine entdeckte Kursidentität.

Discovery liefert mindestens `ProviderKey`, `ExternalCourseId` und Kursname. Der Snapshot enthält dieselbe Kursidentität sowie eine Liste normalisierter Inhalte mit stabiler `ProviderContentId`.

Der Mock-Adapter:

- akzeptiert nur bekannte Fixture-Links;
- führt keine Netzwerkzugriffe aus;
- liefert für verschiedene Alias-Links dieselbe stabile Kursidentität;
- stellt in Tests kontrollierbare Ausgangs-, Änderungs- und Fehlerzustände bereit;
- verwendet in der normalen Entwicklungsoberfläche einen festen Ausgangszustand;
- gibt Login-, Timeout- und ungültige Antwortzustände als typisierte Adapterfehler zurück.

Freitext wird nicht nach Fristen durchsucht. Es gibt weder LLM-Aufrufe noch probabilistische Entscheidungen.

## Vertrauens- und Aufgabenregeln

Ein Inhalt ist im ersten Schnitt nur dann automatisch aufgabenfähig, wenn:

1. der Adapter ihn als Aufgabe normalisiert hat und
2. ein eigenes strukturiertes Fristfeld vorhanden ist.

Ein Datum in Titel oder Beschreibung genügt nicht. Hinweise, Ressourcen und Aufgaben ohne strukturierte Frist erhalten `Prüfung erforderlich` und erzeugen keine `StudyTask`.

Wenn ein zuvor ungeklärter Inhalt später eine strukturierte Aufgabenfrist erhält, wird er beim nächsten erfolgreichen Scan aufgabenfähig und pro Abonnent genau einmal materialisiert.

### Moodle-gesteuerte Aufgaben

Eine durch `ExternalTaskLink` verknüpfte Aufgabe ist hinsichtlich Titel, Beschreibung, Frist und Löschen Moodle-gesteuert:

- Der Benutzer darf nur zwischen `Open` und `Completed` wechseln.
- Bearbeiten und Löschen werden in UI und API abgelehnt.
- Die Quellenkennzeichnung und der externe Link sind sichtbar.
- Manuell angelegte Aufgaben behalten ihr bestehendes Verhalten.

Bei einem geänderten Inhalt werden Titel, Beschreibung und Frist einer offenen Moodle-gesteuerten Aufgabe synchronisiert. ID und Status bleiben erhalten. Erledigte Aufgaben werden nicht nachträglich verändert.

Verliert ein Inhalt seine strukturierte Frist, erhält er `Prüfung erforderlich`. Eine bereits verknüpfte Aufgabe bleibt unverändert und Moodle-gesteuert; sie wird weder gelöscht noch mit einer erfundenen Frist versehen.

## Registrierungsablauf

1. Ein angemeldeter Benutzer sendet einen Kurslink.
2. Der Link wird formal validiert und dem Mock-Provider zugeordnet.
3. Discovery liefert die stabile Kursidentität und den Kursnamen.
4. Der Service findet oder erstellt `ExternalCourse` anhand `ProviderKey + ExternalCourseId`.
5. Der Service findet oder erstellt idempotent das persönliche `StudyModule` und `CourseSubscription`.
6. Existiert ein letzter erfolgreicher Snapshot, werden daraus ohne Adapteraufruf die für den neuen Abonnenten relevanten Aufgaben materialisiert.
7. Die API gibt das vorhandene oder neu erzeugte Abonnement mit Kurs- und Modulreferenz zurück.

Alle Datenbankänderungen der Registrierung einschließlich Modul, Abonnement und Aufgabenmaterialisierung sind atomar. Schlägt ein Teil fehl, bleiben keine teilweise angelegten persönlichen Daten zurück.

Für einen späten Abonnenten gilt ein Inhalt als relevant, wenn er im aktuellen erfolgreichen Snapshot sichtbar ist, aufgabenfähig ist und seine Frist nach der durch `TimeProvider` gelieferten aktuellen Zeit liegt. Bereits abgelaufene bekannte Inhalte erzeugen beim späten Abonnement keine neue persönliche Aufgabe.

Wiederholte oder konkurrierende Registrierungen werden durch Domänenprüfung und Datenbank-Constraints idempotent aufgelöst.

## Manueller Scanablauf

1. Ein angemeldeter Abonnent startet den Scan über sein Abonnement.
2. Der Service prüft die Besitzberechtigung und reserviert genau einen aktiven Lauf für den gemeinsamen Kurs.
3. Existiert bereits ein aktiver Lauf, startet kein zweiter Adapteraufruf; die API antwortet mit `scan_in_progress`.
4. Der Adapter wird genau einmal aufgerufen und liefert einen vollständigen Snapshot oder einen typisierten Fehler.
5. Vor jeder Mutation wird der gesamte Snapshot validiert:
   - Kursidentität stimmt mit dem registrierten Kurs überein;
   - externe Inhalts-IDs sind innerhalb des Snapshots eindeutig;
   - Pflichtfelder und Links sind formal gültig;
   - der Snapshot ist als vollständiger Kurszustand gekennzeichnet.
6. Ein deterministischer Diff vergleicht eingehende Inhalte über stabile IDs mit `ExternalContent`.
7. Eine Datenbanktransaktion verarbeitet neue, geänderte, unveränderte und verschwundene Inhalte sowie alle persönlichen Aufgabenverknüpfungen.
8. Erst nach erfolgreicher Gesamtverarbeitung werden die Inhaltsdaten und der Scanlauf als neuer erfolgreicher Zustand festgeschrieben.
9. Die API liefert eine Zusammenfassung mit neuen, geänderten, ungeklärten und nicht mehr sichtbaren Inhalten.

Ein wiederholter identischer Snapshot erzeugt keine neue Aufgabe und verändert keine bestehende Aufgabe.

### Verschwundene Inhalte

Fehlt ein zuvor bekannter Inhalt in einem vollständigen, anderweitig gültigen Snapshot, wird er als `Nicht mehr sichtbar` markiert. Verknüpfte persönliche Aufgaben bleiben bestehen und werden nicht automatisch gelöscht.

## Transaktionen und Parallelität

Die Mutation eines erfolgreichen Scans ist atomar. Entweder werden alle Inhalte, Verknüpfungen, Aufgabenänderungen und Erfolgsmetadaten gespeichert oder keine davon.

Pro `ExternalCourse` darf höchstens ein Scan gleichzeitig aktiv sein. Ein zweiter manueller Auslöser erhält HTTP `409 Conflict` mit dem sicheren Code `scan_in_progress`. Er startet keinen zweiten Adapteraufruf.

Datenbank-Constraints bilden die letzte Schutzschicht gegen doppelte Kurse, Abonnements, Inhalte und Aufgabenverknüpfungen. Die Anwendungslogik behandelt daraus entstehende konkurrierende Konflikte idempotent, sofern das fachlich vorhandene Objekt bereits dem angefragten Benutzer und Kurs entspricht.

## Fehlerverhalten

| Situation | Verhalten |
| --- | --- |
| Ungültiger oder unbekannter Fixture-Link | HTTP 400; keine Daten werden angelegt |
| Benutzer besitzt das Abonnement nicht | HTTP 404, um fremde Kursdaten nicht offenzulegen |
| Scan läuft bereits | HTTP 409 mit `scan_in_progress`; kein zweiter Abruf |
| Adapter-Timeout oder Login erforderlich | fehlgeschlagener `ScanRun`; HTTP 502; letzter erfolgreicher Zustand bleibt erhalten |
| Falsche Kursidentität oder ungültiger Snapshot | fehlgeschlagener `ScanRun`; HTTP 502 mit sicherem Fehlercode; keine Inhaltsmutation |
| Fehler in der atomaren Verarbeitung | Rollback aller Inhalts- und Aufgabenmutationen; fehlgeschlagener Lauf wird ohne vertrauliche Daten dokumentiert |

Eine Login-Seite, Fehlerseite oder unvollständige Antwort wird niemals als Kursinhalt interpretiert.

## API

Alle Endpunkte erfordern Authentifizierung.

### `POST /api/course-subscriptions`

Registriert einen Fixture-Kurslink für den aktuellen Benutzer. Die Antwort enthält Abonnement-ID, Kursname, Kursidentität, persönliche Modul-ID und letzten Scanstatus. Eine neue Registrierung antwortet mit 201, eine idempotent wiederholte Registrierung mit 200.

### `GET /api/course-subscriptions`

Listet nur die Abonnements des aktuellen Benutzers mit Kursname, Modulreferenz, letztem Scanstatus und Zeit des letzten erfolgreichen Scans.

### `GET /api/course-subscriptions/{id}/contents`

Liefert nur bei Besitz des Abonnements die normalisierten Inhalte des gemeinsamen Kurses. Der benutzerbezogene Status `Aufgabe erstellt` wird aus `ExternalTaskLink` abgeleitet; `Prüfung erforderlich` und `Nicht mehr sichtbar` stammen aus dem externen Inhaltszustand.

### `POST /api/course-subscriptions/{id}/scan`

Startet synchron einen gemeinsamen manuellen Scan, sofern der aktuelle Benutzer das Abonnement besitzt. Die Antwort enthält Scanstatus und Zähler für neue, geänderte, ungeklärte und nicht mehr sichtbare Inhalte.

### Bestehende Aufgabenendpunkte

Aufgabenresultate erhalten optionale externe Quellenmetadaten. Update- und Delete-Endpunkte lehnen Moodle-gesteuerte Aufgaben mit HTTP `409 Conflict` und einem sicheren Fehlercode ab. Der bestehende Status-Endpunkt bleibt für diese Aufgaben erlaubt.

Der bestehende Modul-Delete-Endpunkt lehnt das Löschen eines mit einem aktiven Moodle-Abonnement verbundenen Moduls ebenfalls mit HTTP `409 Conflict` ab. Andere Modulaktionen behalten ihr bisheriges Verhalten.

## Benutzeroberfläche

Die geschützte Navigation erhält den Eintrag `Moodle-Kurse` mit der Route `/moodle-courses`.

Die Ansicht bietet:

- ein Kurslink-Formular;
- eine Kurskarte je persönlichem Abonnement;
- Kursname, letzter Scanstatus und Link zum persönlichen Modul;
- die Aktion `Jetzt scannen`;
- eine Liste erkannter Inhalte mit Titel, Quelllink, strukturierter Frist und Verarbeitungsstatus;
- eine Scan-Zusammenfassung;
- Lade-, Leer-, Erfolgs- und Fehlerzustände.

In der bestehenden Aufgabenansicht:

- erscheinen automatisch erzeugte Aufgaben im persönlichen Kursmodul;
- sind Moodle-Quelle und externer Link sichtbar;
- bleiben Statusaktionen verfügbar;
- sind Bearbeiten und Löschen für Moodle-gesteuerte Aufgaben nicht verfügbar.

In der bestehenden Modulansicht bleibt das verknüpfte Modul bearbeitbar. Die Löschaktion ist gesperrt und erklärt, dass zunächst eine künftige Abonnementlöschung erforderlich wäre.

Alle neuen sichtbaren Texte werden in Deutsch und Englisch bereitgestellt.

## Sicherheit und Datenschutz

- Der Mock führt keine Netzwerkzugriffe aus.
- Nur bekannte Fixture-Links werden akzeptiert.
- Fremde Abonnement-IDs werden als nicht gefunden behandelt.
- Quelllinks werden als nicht vertrauenswürdige externe Daten validiert und sicher dargestellt.
- Logs und `ScanRun` enthalten keine Tokens, Zugangsdaten oder vollständigen vertraulichen Kursinhalte.
- Fixtures enthalten ausschließlich fiktive Daten.
- Ein später realer Adapter benötigt eine eigene Sicherheits- und Datenschutzentscheidung; diese Spezifikation autorisiert keine Passwortspeicherung.

## Deterministische Testdaten

Die Tests steuern die Mock-Quelle direkt, ohne eine produktive oder benutzerseitige Steueroberfläche.

### Ausgangszustand

- ein fiktiver Kurs mit stabiler externer Kurs-ID;
- `exercise-1` als Aufgabe mit strukturierter zukünftiger Frist;
- `announcement-1` als Hinweis ohne strukturierte Frist.

### Geänderter Zustand

- `exercise-1` behält seine externe ID, erhält aber neuen Titel, Quelllink und neue strukturierte Frist;
- `exercise-2` kommt als neue Aufgabe mit strukturierter Frist hinzu;
- der Hinweis bleibt ohne aufgabenfähige Frist.

### Fehlerzustände

- Adapter-Timeout;
- Login erforderlich;
- falsche externe Kurs-ID;
- doppelte externe Inhalts-ID;
- unvollständiger oder formal ungültiger Snapshot.

`TimeProvider` steuert die aktuelle Zeit in Tests, damit Fristrelevanz und späte Abonnements ohne echte Wartezeit reproduzierbar sind.

## Teststrategie

### Domain und Application

- Entitätsinvarianten und Statusübergänge;
- Snapshot-Diff für neu, geändert, unverändert und nicht mehr sichtbar;
- Erzeugungsregel für strukturierte Aufgabenfristen;
- Idempotenz bei wiederholtem Snapshot;
- Synchronisierung offener und Schutz erledigter Aufgaben;
- Materialisierung für drei Abonnenten und für einen späten Abonnenten;
- Erhalt des letzten erfolgreichen Zustands bei Fehlern.

### Persistenz und API

- eindeutige Kurs-, Abonnement-, Inhalts- und Aufgabenverknüpfungen;
- atomare Verarbeitung ohne Teilaufgaben;
- genau ein Adapteraufruf je gemeinsamem Scan;
- Autorisierung über das persönliche Abonnement;
- Statuscodes und sichere Fehlercodes;
- Schreibschutz Moodle-gesteuerter Aufgaben.
- Löschschutz eines mit einem aktiven Abonnement verbundenen Moduls.

### Frontend

- Serviceverträge für Registrierung, Liste, Inhalte und Scan;
- Formularvalidierung und Fehlerdarstellung;
- Scanstatus und Zusammenfassung;
- Inhaltsstatus `Aufgabe erstellt`, `Prüfung erforderlich` und `Nicht mehr sichtbar`;
- Link zum persönlichen Modul und Moodle-Quellenanzeige;
- ausgeblendete Bearbeiten-/Löschen-Aktionen für Moodle-gesteuerte Aufgaben;
- deutsche und englische Texte.

Der erste Schnitt führt kein neues Browser-End-to-End-Framework ein. Der sichtbare Gesamtweg wird zusätzlich manuell abgenommen; Kernregeln, API und Vue-Komponenten werden automatisiert getestet.

## Akzeptanzkriterien

1. Ein bekannter Fixture-Link erzeugt genau einen gemeinsamen Kurs, ein persönliches Abonnement und ein persönliches Modul.
2. Eine wiederholte Registrierung desselben Kurses erzeugt keine Duplikate.
3. Drei Abonnenten führen bei einem Scan zu genau einem Adapteraufruf und drei persönlichen Aufgaben für einen aufgabenfähigen Inhalt.
4. Ein Inhalt ohne strukturierte Aufgabenfrist erscheint als `Prüfung erforderlich` und erzeugt keine Aufgabe.
5. Derselbe Snapshot erzeugt bei Wiederholung keine weitere Aufgabe.
6. Eine Umbenennung, Verschiebung oder Friständerung erhält Inhalts- und Aufgaben-ID und aktualisiert nur offene Moodle-gesteuerte Aufgaben.
7. Eine erledigte Aufgabe bleibt bei externen Änderungen unverändert.
8. Ein später Abonnent erhält relevante Aufgaben aus dem bestehenden Snapshot, ohne erneuten Adapteraufruf.
9. Fehlerhafte Scans und Login-Antworten überschreiben den letzten erfolgreichen Zustand nicht.
10. Verschwundene Inhalte löschen keine persönlichen Aufgaben.
11. Nichtabonnenten können Kursinhalte weder lesen noch einen Scan starten.
12. Moodle-gesteuerte Aufgaben erlauben Statusänderungen, aber kein Bearbeiten oder Löschen.
13. Ein mit einem aktiven Abonnement verbundenes Modul bleibt bearbeitbar, kann aber nicht gelöscht werden.
14. Code, Logs und Fixtures enthalten keine Zugangsdaten oder vertraulichen Echtdaten.
15. Der sichtbare Ablauf `registrieren → scannen → Inhalt sehen → erzeugte Aufgabe im Modul öffnen` funktioniert auf Deutsch und Englisch.

## Abschlussprüfungen

Nach der späteren Implementierung werden mindestens ausgeführt:

```bash
dotnet build backend/StudyOrganizer.sln
dotnet test backend/StudyOrganizer.sln

cd frontend
pnpm type-check
pnpm lint
pnpm exec vitest run
pnpm build
```

Der bereits in S0 dokumentierte JWT-Konfigurationsfehler der 43 API-Baseline-Tests wird weiterhin getrennt von Moodle-Regressionen ausgewiesen. Neue Moodle-Tests und alle zuvor grünen Prüfungen dürfen keine ungeklärten neuen Fehler enthalten.

## Nächster Prozessschritt

Nach der schriftlichen Freigabe dieses Dokuments erstellt `writing-plans` einen ausführbaren Implementierungsplan. Bis zu dieser Freigabe und Planung wird kein Produktcode geändert.
