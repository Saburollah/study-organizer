# Architekturproblem der Moodle-Integration

## Status und Ziel dieser Notiz

Diese Notiz beschreibt die Architekturfragen einer möglichen Moodle-Integration für den Study Organizer. Sie ist noch keine fertige Spezifikation und enthält keine endgültige Implementierungsentscheidung. Ihr Zweck ist, das Problem vor dem Einsatz von Wayfinder fachlich zu verstehen, realistische Lösungsvarianten zu vergleichen und offene Entscheidungen sichtbar zu machen.

## Gewünschte Funktion

Studierende sollen einen externen Moodle-Kurs im Study Organizer registrieren können. Dafür hinterlegen sie zunächst den Link zum Kurs. Der Study Organizer untersucht anschließend, über welchen technischen Zugang die Kursinhalte gelesen werden können und welche Struktur der Kurs besitzt.

Nach erfolgreicher Einrichtung soll der Kurs regelmäßig, zunächst beispielsweise einmal pro Tag, auf neue oder geänderte Inhalte geprüft werden. Neue Übungsblätter, Aufgaben, Dateien oder relevante Abgabefristen sollen als Lerninhalte erkannt werden. Aus ihnen können Lernaufgaben im Study Organizer entstehen und betroffene Benutzer können benachrichtigt werden.

Mehrere Benutzer können denselben externen Kurs abonnieren. In diesem Fall soll der Study Organizer den Kurs nicht für jeden Benutzer separat abrufen. Ein gemeinsamer Scan soll neue Inhalte einmal erkennen und danach alle registrierten Benutzer informieren. Damit werden unnötige Anfragen an Moodle vermieden und dieselben externen Inhalte nicht mehrfach gespeichert.

## Warum die Moodle-Struktur dynamisch ist

Moodle ist kein einzelnes, weltweit identisch konfiguriertes System. Universitäten betreiben eigene Moodle-Installationen mit unterschiedlichen Versionen, Erweiterungen, Themes, Berechtigungen und Authentifizierungsverfahren. Zusätzlich können Lehrende Kurse unterschiedlich strukturieren.

Moodle unterstützt verschiedene Kursformate. Ein Kurs kann beispielsweise nach Themen, Wochen oder einer durch ein Plugin bereitgestellten Struktur organisiert sein. Kursformate bestimmen unter anderem, wie Abschnitte, Navigation und Lerninhalte dargestellt werden. Moodle-Aktivitätsmodule können Dateien, Ordner, Aufgaben, Tests, Foren, Links oder weitere durch Plugins bereitgestellte Inhaltstypen repräsentieren.

Aus Sicht des Study Organizers sind deshalb mehrere Dinge veränderlich:

- Die sichtbare HTML-Struktur kann sich durch Moodle-Version, Theme oder Kursformat ändern.
- Derselbe fachliche Inhalt kann als Datei, Ordner, Aufgabe, Link oder Plugin-Inhalt erscheinen.
- Titel und Position eines Inhalts können geändert werden, ohne dass ein neuer Inhalt entstanden ist.
- Inhalte können abhängig von Rolle, Einschreibung, Gruppe, Freigabedatum oder Abschlussbedingungen sichtbar sein.
- Eine Moodle-Installation kann offizielle Web Services anbieten, eine andere jedoch nicht oder nur mit eingeschränkten Berechtigungen.
- Authentifizierung kann über Moodle selbst oder über ein universitäres Single-Sign-on-System erfolgen.

Die offiziellen Moodle-Unterlagen bestätigen diese Erweiterbarkeit: Kursformate bestimmen den Aufbau von Kursressourcen, Aktivitätsmodule stellen unterschiedliche Lerninhalte bereit, und das External-Service-System bietet konfigurierbare Endpunkte für externe Anwendungen.

Quellen:

- [Moodle: Course formats](https://moodledev.io/docs/4.5/apis/plugintypes/format)
- [Moodle: Activity modules](https://moodledev.io/docs/5.0/apis/plugintypes/mod)
- [Moodle: External Services](https://moodledev.io/docs/5.0/apis/subsystems/external)

## Warum wenige fest programmierte REST-Methoden nicht ausreichen

Drei fest programmierte REST-Methoden würden voraussetzen, dass jede unterstützte Moodle-Installation dieselben Endpunkte, Datenstrukturen, Berechtigungen und Bedeutungen bereitstellt. Diese Annahme ist ohne eine verbindliche Integrationsvereinbarung mit den Universitäten nicht belastbar.

Das Problem ist nicht REST als Technologie. Wenn eine Universität die benötigten offiziellen Moodle Web Services aktiviert, dokumentiert und für Studierende freigibt, kann ein REST-basierter Adapter eine gute Lösung sein. Ein normaler Moodle-Kurslink garantiert diesen Zugang jedoch nicht. Die tatsächlich verfügbaren Funktionen werden von der jeweiligen Installation und ihrer Administration bestimmt.

Ein direkter HTML-Scraper löst dieses Problem ebenfalls nicht vollständig. Er tauscht die Abhängigkeit von einer Web-Service-Schnittstelle gegen eine Abhängigkeit von HTML, CSS, Sprache, Theme und Kursformat. Änderungen an der Darstellung könnten den Scraper beschädigen, obwohl sich der fachliche Kursinhalt nicht verändert hat.

Die Anwendung benötigt daher eine stabile eigene Sicht auf Kursinhalte und austauschbare Zugangswege zu externen Systemen.

## Stabile Domänensicht des Study Organizers

Die interne Domänensicht sollte nicht die Moodle-HTML-Struktur kopieren. Sie sollte nur die Informationen abbilden, die der Study Organizer für Erkennung, Aufgaben und Benachrichtigungen benötigt.

Eine vorläufige normalisierte Sicht könnte folgende Konzepte enthalten:

- **Externer Kurs:** Der einmalig identifizierte Kurs einer externen Lernplattform.
- **Kursabonnement:** Die Zuordnung eines Benutzers zu einem externen Kurs.
- **Externer Lerninhalt:** Eine Datei, Aufgabe, ein Link oder ein anderer relevanter Inhalt.
- **Kurs-Snapshot:** Der bei einem Scan beobachtete Zustand eines Kurses.
- **Scanlauf:** Zeitpunkt, Ergebnis und Fehler eines Abrufs.
- **Inhaltsänderung:** Ein neuer, geänderter oder nicht mehr sichtbarer Lerninhalt.
- **Lernaufgabe:** Eine aus einem externen Inhalt abgeleitete Aufgabe im Study Organizer.

Diese Begriffe sind Arbeitshypothesen. Sie müssen später mit dem vorhandenen Domain-Modell abgeglichen und in `CONTEXT.md` präzisiert werden.

## Architekturprinzipien

### Ports and Adapters

Der Kern des Study Organizers soll über ein kleines Interface mit einer externen Lernplattform kommunizieren. Konkrete Zugänge werden als Adapter implementiert. Denkbare Adapter sind ein Moodle-Web-Service-Adapter, ein HTML-Adapter und ein deterministischer Mock-Adapter.

Das Interface ist gleichzeitig die Testoberfläche. Fachliche Tests sollen Kurs-Snapshots und Änderungen prüfen können, ohne eine echte Moodle-Installation aufzurufen.

### Discovery und Fähigkeiten

Beim Registrieren eines Kurses soll eine Discovery feststellen, welche Integrationsmöglichkeit verfügbar ist. Sie könnte beispielsweise prüfen, ob ein unterstützter Web Service erreichbar ist oder ob für die Moodle-Installation ein bekannter HTML-Adapter existiert.

Discovery bedeutet nicht, bei jedem Scan erneut eine vollständige Analyse durchzuführen. Das Ergebnis kann als Integrationsprofil gespeichert und später validiert werden. Wenn das Profil nicht mehr funktioniert, wechselt die Registrierung in einen Zustand wie `discovery-required`, statt falsche Lernaufgaben anzulegen.

### Snapshot und Diff

Ein Scan erzeugt einen normalisierten Kurs-Snapshot. Dieser wird mit dem zuletzt erfolgreichen Snapshot verglichen. Erst der Vergleich entscheidet, ob ein Inhalt neu, geändert oder unverändert ist.

Dabei wird eine stabile externe Identität benötigt. Nur Titel oder Position reichen nicht aus, weil ein umbenannter oder verschobener Inhalt sonst fälschlich als neu erkannt werden könnte. Falls Moodle keine stabile ID liefert, muss eine Ersatzidentität aus belastbaren Merkmalen abgeleitet werden. Diese Ableitung ist eine wichtige offene Entscheidung.

### Idempotenz

Das wiederholte Verarbeiten desselben Snapshots muss dasselbe Ergebnis erzeugen. Ein Inhalt darf nicht bei jedem täglichen Scan erneut als Lernaufgabe angelegt werden. Idempotenz gilt auch bei Wiederholungen nach Timeouts oder teilweise fehlgeschlagenen Scanläufen.

### Multi-Tenancy und Deduplizierung

Ein externer Kurs wird unabhängig von seinen Benutzern gespeichert. Benutzer abonnieren diesen Kurs über separate Kursabonnements. Der Scheduler plant einen Scan pro externem Kurs und nicht pro Benutzer. Nach der Änderungserkennung werden die betroffenen Abonnenten bestimmt und benachrichtigt.

Die kanonische Identität eines Kurses darf nicht allein aus dem eingegebenen Link abgeleitet werden, weil verschiedene Links auf denselben Kurs zeigen können. Auch diese Normalisierung ist eine offene Entscheidung.

## Lösungsvarianten

### 1. Offizielle Moodle Web Services

**Vorteile**

- Strukturierte Daten statt Interpretation von HTML.
- Stabilere technische Identitäten und Datentypen.
- Bessere Trennung zwischen Inhalt und Darstellung.
- Dateizugriffe können über vorgesehene Moodle-Funktionen erfolgen.

**Nachteile**

- Web Services müssen von der Universität aktiviert und freigegeben werden.
- Benutzer benötigen ein geeignetes Authentifizierungsverfahren oder Token.
- Verfügbare Funktionen und Plugins können sich zwischen Installationen unterscheiden.
- Der Study Organizer kann die Administration einer fremden Moodle-Instanz nicht kontrollieren.

### 2. HTML-Scraping mit bekannten Adaptern

**Vorteile**

- Kann möglicherweise mit dem normalen Kurszugang arbeiten.
- Benötigt kein eigenes Moodle-Plugin.
- Für eine begrenzte Zahl bekannter Installationen relativ schnell prototypisierbar.

**Nachteile**

- Abhängigkeit von HTML, Theme, Sprache und Kursformat.
- Single Sign-on und Sitzungsverwaltung können komplex sein.
- Änderungen an der Darstellung können den Adapter unbemerkt beschädigen.
- Fehlerhafte Interpretation kann falsche Aufgaben erzeugen.
- Nutzungsbedingungen, Datenschutz, Zugriffslast und sichere Behandlung von Zugangsdaten müssen geprüft werden.

### 3. Eigenes Moodle-Plugin

**Vorteile**

- Direkter Zugriff auf strukturierte Moodle-Daten und Ereignisse.
- Änderungen könnten aktiv gemeldet werden, sodass weniger Polling nötig ist.
- Eine klar definierte, projektspezifische Schnittstelle wäre möglich.

**Nachteile**

- Jede Universität müsste Installation und Betrieb erlauben.
- Rollout und Wartung liegen teilweise außerhalb der Kontrolle des Study Organizers.
- Für ein studentisches Projekt ist diese organisatorische Abhängigkeit wahrscheinlich zu groß.

### 4. Discovery mit gespeichertem Integrationsprofil

**Vorteile**

- Unterschiede zwischen Moodle-Installationen werden explizit behandelt.
- Eine einmal erkannte Struktur kann bei späteren Scans wiederverwendet werden.
- Fehlerzustände und erneute Discovery können als kontrollierter Lebenszyklus modelliert werden.

**Nachteile**

- Discovery selbst ist anspruchsvoll und kann falsche Profile erzeugen.
- Strukturänderungen müssen erkannt werden.
- Profile benötigen Versionierung, Validierung und Diagnoseinformationen.

### 5. LLM-gestützte Erkennung

**Vorteile**

- Ein LLM kann semantische Hinweise wie „Übungsblatt“, „Abgabe“ oder „Deadline“ trotz unterschiedlicher Bezeichnungen erkennen.
- Es kann bei der erstmaligen Zuordnung unbekannter Strukturen helfen.
- Neue Varianten könnten teilweise ohne sofortige Codeänderung analysiert werden.

**Nachteile**

- Ergebnisse sind nicht vollständig deterministisch.
- Laufende Kosten und Latenz entstehen.
- Kursinhalte können personenbezogene oder urheberrechtlich geschützte Daten enthalten.
- Fremdes HTML ist nicht vertrauenswürdig und kann Prompt-Injection enthalten.
- Ein LLM sollte keine unkontrollierten Tool-Aufrufe mit Benutzerzugang ausführen.
- Jede LLM-Entscheidung benötigt strukturierte Validierung und sichere Begrenzungen.

### 6. Hybrider Ansatz

Ein hybrider Ansatz versucht zunächst den zuverlässigsten verfügbaren Zugang zu verwenden:

1. Offizielle Web Services, wenn sie verfügbar und autorisiert sind.
2. Einen bekannten deterministischen Adapter für unterstützte Moodle-Installationen.
3. Discovery zur Erstellung oder Reparatur eines Integrationsprofils.
4. LLM-Unterstützung nur für eng begrenzte semantische Zuordnungen, nicht als alleinige Entscheidungsinstanz.

Dieser Ansatz bietet Flexibilität, erhöht jedoch die Anzahl der Zustände, Adapter und Fehlerfälle. Er ist deshalb zunächst nur eine Arbeitshypothese.

## Vorläufige Arbeitshypothese

Für einen ersten vertikalen Schnitt erscheint eine kleine, deterministische Lösung sinnvoll:

- Ein lokaler Mock-Moodle-Adapter stellt mehrere Kursstrukturen bereit.
- Ein gemeinsames Interface liefert normalisierte Kurs-Snapshots.
- Ein Diff erkennt neue Lerninhalte idempotent.
- Mehrere Benutzer können denselben externen Kurs abonnieren.
- Ein manueller Scanlauf beweist zunächst die fachliche Logik; zeitgesteuertes Polling folgt später.

Dieser Schnitt testet die risikoreichen fachlichen Annahmen, ohne bereits Authentifizierung, fremde Moodle-Installationen oder LLM-Aufrufe zu benötigen. Erst nach diesem Beweis soll entschieden werden, welcher reale Moodle-Zugang als erster Adapter implementiert wird.

## Teststrategie mit einem fiktiven Kurs

Da während der Semesterferien möglicherweise keine echten neuen Inhalte veröffentlicht werden, soll eine kontrollierte Mock-Quelle verschiedene zeitliche Zustände simulieren:

1. Der Kurs enthält ein erstes Übungsblatt.
2. Ein zweites Übungsblatt kommt hinzu.
3. Das zweite Übungsblatt wird umbenannt oder verschoben.
4. Eine Abgabefrist wird ergänzt oder verändert.
5. Die HTML-Struktur oder das simulierte Kursformat ändert sich.
6. Der Kurs liefert vorübergehend einen Fehler oder eine Login-Seite.

Mindestens folgende Verhaltensweisen sollen überprüft werden:

- Ein neuer Inhalt wird genau einmal erkannt.
- Ein unveränderter Inhalt erzeugt keine weitere Lernaufgabe.
- Eine Umbenennung erzeugt kein Duplikat, sofern die Identität erhalten bleibt.
- Drei Abonnenten führen zu einem Scan und drei gezielten Benachrichtigungen.
- Ein fehlgeschlagener Scan überschreibt den letzten erfolgreichen Snapshot nicht.
- Eine Login- oder Fehlerseite wird nicht als Kursinhalt interpretiert.
- Ein ungültiges Integrationsprofil führt kontrolliert zu erneuter Discovery.
- Tests können Scheduler und Uhr deterministisch steuern, ohne einen echten Tag zu warten.

## Sicherheits- und Betriebsfragen

Vor einer realen Moodle-Anbindung müssen mindestens folgende Themen geklärt werden:

- Zugangsdaten und Passwörter werden nicht im Klartext gespeichert.
- Es ist zu klären, ob OAuth, Web-Service-Token oder eine andere delegierte Autorisierung möglich ist.
- Kursinhalte und Metadaten dürfen nur für berechtigte Benutzer verarbeitet werden.
- Polling benötigt Rate Limits, Timeouts, Wiederholungsregeln und Backoff.
- Protokolle dürfen keine Tokens, Sitzungsdaten oder vertraulichen Kursinhalte enthalten.
- Der Umgang mit urheberrechtlich geschützten Dateien und personenbezogenen Daten muss geklärt werden.
- Externe Inhalte werden als nicht vertrauenswürdig behandelt.

## Offene Architekturfragen

1. Welche konkreten Moodle-Installationen sollen zuerst unterstützt werden?
2. Stehen dort offizielle Web Services für Studierende zur Verfügung?
3. Wie wird ein externer Kurs kanonisch identifiziert?
4. Wie wird ein externer Lerninhalt über Umbenennungen und Verschiebungen hinweg identifiziert?
5. Welche Inhaltstypen gehören zur ersten Version?
6. Entsteht aus jedem neuen Inhalt automatisch eine Lernaufgabe oder muss der Benutzer bestätigen?
7. Wie werden geänderte und entfernte Inhalte behandelt?
8. Wie authentifiziert sich der Study Organizer, ohne Moodle-Passwörter zu speichern?
9. Wie oft darf eine Moodle-Installation abgefragt werden?
10. Wann gilt ein Integrationsprofil als ungültig und benötigt neue Discovery?
11. Welche LLM-Daten dürften an welchen Modellanbieter übertragen werden?
12. Wie werden falsche positive und falsche negative Erkennungen sichtbar gemacht?

Diese Fragen sollen vor oder innerhalb des Wayfinder-Prozesses als Entscheidungstickets bearbeitet werden.

## Persönliche Reflexion

Dieser Abschnitt wird nach der eigenen Durcharbeitung ergänzt:

- Welche Ursache der Dynamik war für mich zunächst nicht offensichtlich?
- Welche Lösungsvariante hätte ich spontan gewählt und warum?
- Welche Nachteile dieser Variante habe ich erst bei der Untersuchung erkannt?
- Warum ist die Trennung zwischen stabilem Domänenmodell und veränderlichem Moodle-Adapter wichtig?
- Welche Entscheidung würde ich vor einem ersten Prototyp treffen?
- Welche Frage kann nur durch einen Prototyp oder Test beantwortet werden?

## Nächster Schritt

Vor Wayfinder werden die offiziellen Moodle-Funktionen und mindestens eine reale Moodle-Installation auf verfügbare Integrationsmöglichkeiten untersucht. Danach werden die offenen Architekturfragen priorisiert. Wayfinder soll daraus eine Entscheidungslandkarte erstellen; er soll noch keine vollständige Implementierung planen.
