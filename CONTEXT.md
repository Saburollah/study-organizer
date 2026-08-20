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

**Course Subscription**:
Die eindeutige Verbindung eines persönlichen Study Module mit einem External Course. Ein Study Module besitzt höchstens ein Abonnement, und ein Benutzer kann denselben External Course nur einmal abonnieren.
_Avoid_: Course Copy

**External Learning Content**:
Ein normalisierter Inhalt eines External Course mit einer stabilen gemeinsamen Identität. Er kann beispielsweise eine PDF-Datei, ein Link oder eine andere Aufgabe sein.
_Avoid_: Study Task, File

**Imported Study Task**:
Eine persönliche Study Task, die für einen Abonnenten aus einem External Learning Content entstanden ist.
_Avoid_: External Learning Content