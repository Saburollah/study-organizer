# Study Organizer

Der Study Organizer organisiert persönliche Lernmodule und Aufgaben. Externe Kursinhalte können gemeinsam erkannt und anschließend als persönliche Aufgaben für abonnierte Benutzer bereitgestellt werden.

## Language

**Study Module**:
Ein persönlicher, genau einem Benutzer gehörender Organisationsbereich für Lernaufgaben.
_Avoid_: External Course, Moodle Course

**Study Task**:
Eine persönliche Lernaufgabe innerhalb eines Study Module. Sie kann ein optionales Fälligkeitsdatum besitzen.
_Avoid_: External Learning Content

**External Course**:
Die gemeinsame Darstellung eines Kurses aus einer externen Lernplattform, den mehrere Benutzer abonnieren können.
_Avoid_: Study Module, User Course

**Inactive External Course**:
Der Lebenszykluszustand eines External Course ohne aktive Course Subscription, der keinen allgemeinen Kurszugriff gewährt; nur eine Course Subscription im Zustand `Pending` darf einen kontrollierten Einrichtungsscan auslösen. Benötigte Identitäts- und Referenzhistorie bleibt erhalten.
_Avoid_: Archived Course, Deleted Course

**External Course Identity**:
Die kanonische Kombination aus Quellentyp, Quelleninstanz und stabilem externen Kursschlüssel, durch die derselbe External Course wiedererkannt wird. Kursname und Zugriffslink gehören nicht zu dieser Identität.
_Avoid_: Course URL, Course Name

**External Content Key**:
Ein innerhalb eines External Course stabiler Schlüssel für einen External Learning Content. Umbenennung, Verschiebung oder ein neuer Zugriffslink verändern diesen Schlüssel nicht.
_Avoid_: Content URL, Content Title, Position

**Course Snapshot**:
Der vollständige validierte Zustand eines External Course zu einem bestimmten Beobachtungszeitpunkt. Nur ein erfolgreicher Scan Run darf den zuletzt gültigen Course Snapshot ersetzen.
_Avoid_: Partial Response, Scan Run

**Scan Run**:
Ein einzelner Versuch, den vollständigen Zustand eines External Course abzurufen, zu validieren und zu übernehmen. Sein Ergebnis beschreibt Erfolg, Fehler oder Abbruch, ohne selbst der Kurszustand zu sein.
_Avoid_: Course Snapshot, Polling Schedule

**Course Subscription**:
Die eindeutige Verbindung eines persönlichen Study Module mit einem External Course; sie besitzt den Zustand `Pending`, `Active` oder `Ended`, ein Study Module höchstens eine und ein Benutzer pro External Course höchstens eine. `Active` gewährt Kurszugriff, `Pending` erlaubt nur den Einrichtungsscan und `Ended` kann persönliche Importhistorie ohne Kurszugriff bewahren.
_Avoid_: Course Copy, Course Ownership

**External Learning Content**:
Ein normalisierter Inhalt eines External Course mit einer stabilen gemeinsamen Identität. Er kann eine PDF-Datei, ein Link oder eine andere Aufgabe sein und bleibt auch bekannt, wenn er extern nicht mehr verfügbar ist.
_Avoid_: Study Task, File

**Metadata-Purged External Learning Content**:
Ein External Learning Content, dessen Aufbewahrungsfrist abgelaufen ist und dessen externe Metadaten entfernt wurden, während seine stabile Identität wegen persönlicher Importhistorie erhalten bleibt. Imported Study Tasks bleiben bestehen; Titel, Fälligkeit, Medientyp und Quellreferenz des externen Inhalts dürfen nicht mehr offengelegt werden.
_Avoid_: Deleted External Content, Unavailable External Learning Content

**Imported Study Task**:
Eine persönliche Study Task, die aus einem External Learning Content entstanden und dauerhaft mit ihm verbunden ist. Ihre persönlichen Planungsdaten und ihr Status bleiben von den gemeinsamen externen Metadaten getrennt.
_Avoid_: External Learning Content

**Dismissed Import**:
Die persönliche Entscheidung eines Abonnenten, für einen bestimmten External Learning Content keine Imported Study Task mehr zu führen. Sie verhindert eine automatische Neuerstellung, ohne den gemeinsamen externen Inhalt zu verändern.
_Avoid_: Deleted External Content, Shared Deletion

**Source Update**:
Ein persönlicher, noch nicht bestätigter Hinweis darauf, dass sich die externen Metadaten einer Imported Study Task geändert haben. Er überschreibt weder persönliche Planungsdaten noch den Aufgabenstatus.
_Avoid_: Task Reopening, Automatic Overwrite
