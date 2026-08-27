# Beobachtungslog: Superpowers-Versuch

## Versuchsmetadaten

| Merkmal | Wert |
| --- | --- |
| Versuch | Obras Superpowers |
| Branch | `experiment/superpowers` |
| Separater Worktree | Ja: `.worktrees/superpowers` |
| Gemeinsamer Feature-Ausgangscommit | `e7d8b5ecc3fe75a38655f16c4636328bb1598d57` |
| Experiment-Setup-Commit | `1c4b35aef923328a9ff0e3afba501dcf923e0c1a` |
| Superpowers-Version | `superpowers-v6.3.0` |
| Superpowers-Skill-Commit | `a41901651ce44a66c2fea78dda4f81e8a3815fbe` |
| Beginn | 27. August 2026, ca. 21:00 CEST; nachträglich näherungsweise erfasst |
| Aktuelle Agentensitzung | 1 |

Der Setup-Commit liegt direkt auf dem gemeinsamen Ausgangscommit. Sein Diff ändert nur den Submodule-Pointer von `.agents/skills` und neutrale Metadaten in `experiment-protocol.md`. Vor der Baseline war der Arbeitsbaum sauber. `AGENTS.md` war nicht vorhanden.

## Laufende objektive Messwerte

| Messwert | Aktueller Wert | Erläuterung |
| --- | ---: | --- |
| Agentensitzungen | 1 | Beginn von S0 und S1 in derselben Sitzung |
| Rückfragen an den Benutzer | 19 | Eine S0-Reflexionsfrage, siebzehn S1-Entscheidungs-/Freigabefragen und eine schriftliche Spec-Review-Frage |
| Davon Produkt-/Architekturfragen | 17 | Zehn Klärungsfragen, eine Ansatzwahl, fünf Designfreigaben und eine Konsistenzfrage |
| Umgebungsfreigaben | 3 | Sandbox-Ausnahme für Build und Tests; Lockfile-Installation |
| Vom Benutzer korrigierte Annahmen | 0 |  |
| Planungs- und Entscheidungsdokumente | 1 | Bestätigte Moodle-Designspezifikation; das Beobachtungslog wird nicht mitgezählt |
| Implementierungsplanaufgaben | 0 | Noch keine Planung oder Implementierung |
| Fehlgeschlagene oder wiederholte Implementierungsversuche | 0 | Drei Setup-/Baseline-Wiederholungen werden unten separat erfasst |
| Neu angelegte automatisierte Tests | 0 |  |
| Geänderte Produktdateien | 0 | Nur dieses Versuchslog wurde angelegt |

## Baseline-Ergebnisse

Die Produkt-Baseline wurde am 27. August 2026 abgeschlossen. Backend-Befehle mussten wegen der von MSBuild benötigten lokalen Named Pipes außerhalb der eingeschränkten Sandbox wiederholt werden. Frontend-Abhängigkeiten wurden vor der aussagekräftigen Prüfung mit `pnpm install --frozen-lockfile` aus dem vorhandenen Lockfile und vollständig aus dem lokalen Paketcache installiert. Das Lockfile blieb unverändert.

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| `dotnet build backend/StudyOrganizer.sln` | Bestanden | Exit 0; 0 Warnungen; 0 Fehler; gemeldete Buildzeit 1,48 s |
| `dotnet test backend/StudyOrganizer.sln` | Baseline-Fehler | Domain: 17/17 bestanden. API: 0/43 bestanden; alle 43 Tests scheitern beim Hoststart mit `InvalidOperationException: JWT configuration is incomplete or invalid.`; Exit 1 |
| `cd frontend && pnpm type-check` | Bestanden | Exit 0 nach Installation der Lockfile-Abhängigkeiten |
| `cd frontend && pnpm lint` | Bestanden | Exit 0; keine versionierten Änderungen durch die `--fix`-Skripte |
| `cd frontend && pnpm exec vitest run` | Bestanden | 17/17 Testdateien, 77/77 Tests; Exit 0; gemeldete Dauer 2,47 s |
| `cd frontend && pnpm build` | Bestanden | Exit 0; 86 Module transformiert; Vite-Build in 472 ms |

### Abgrenzung des Backend-Baseline-Fehlers

Der API-Testfehler bestand vor jeder Moodle-Implementierung im isolierten Superpowers-Branch. Er wird deshalb gemäß Versuchsprotokoll weder der Skill-Suite noch dem Moodle-Feature zugerechnet. Vor der Implementierung muss entschieden werden, wie er für Abschlussprüfungen reproduzierbar konfiguriert wird; in S0 wurde er bewusst nicht repariert.

### Setup- und Umgebungsereignisse

1. Der erste Sandbox-Lauf von `dotnet build` blieb mehr als zwei Minuten ohne Ausgabe hängen und wurde abgebrochen. Derselbe Befehl bestand außerhalb der Sandbox.
2. Der erste Sandbox-Lauf von `dotnet test` scheiterte mit `System.Net.Sockets.SocketException (13): Permission denied`, weil MSBuild keine lokale Named Pipe binden durfte. Derselbe Befehl lief außerhalb der Sandbox bis zu den beschriebenen Produkttests.
3. Der erste Aufruf von `pnpm type-check` konnte `vue-tsc` nicht finden, weil `frontend/node_modules` fehlte. Nach `pnpm install --frozen-lockfile` bestand die Typprüfung.
4. Die Baseline erzeugte nur ignorierte Arbeitsartefakte: `frontend/node_modules/`, `frontend/dist/` und `frontend/.eslintcache`. Der Git-Diff blieb bis zur Anlage dieses Logs leer.

## Beobachtungslog pro Phase

### Superpowers S0 — Isolation und Baseline

- **Beginn/Ende:** 27. August 2026, ca. 21:00–21:13 CEST; Beginn nachträglich näherungsweise erfasst
- **Verwendete Skills:** `using-superpowers`, danach `brainstorming` zur Prozessklassifikation; noch keine Implementierungs-Skills
- **Erzeugte Artefakte:** `Docs/skill-evaluation/superpowers-observations.md`
- **Gestellte Rückfragen:** Keine Produktfrage; drei technische Freigaben für reproduzierbare Baseline-Ausführung
- **Wichtige Entscheidungen:** Aufgabe als architektonischen Brainstorming-Pfad klassifiziert; Baseline- und Umgebungsfehler getrennt; keine Reparatur des vorbestehenden JWT-Problems in S0
- **Fehlversuche oder Blocker:** Sandbox-Hänger beim Build; Named-Pipe-Verbot bei Tests; fehlende Frontend-Abhängigkeiten; vorbestehender JWT-Konfigurationsfehler in allen 43 API-Tests
- **Korrekturen durch den Benutzer:** Keine
- **Besonders hilfreich:** Das Protokoll gab identische, konkrete Prüfbefehle und die Regel zur Behandlung vorbestehender Fehler vor
- **Unnötig oder zu aufwendig:** **Agentenentwurf:** Die Sandbox-bedingten Wiederholungen erhöhten den Zeit- und Freigabeaufwand, ohne Produktwissen zu erzeugen
- **Vom Skill übersehen:** **Agentenentwurf:** `brainstorming` selbst definiert keine Experiment-Baseline; die nötige Trennung kam aus dem projektspezifischen Versuchsprotokoll
- **Was ich selbst gelernt habe:** **Vom Benutzer bestätigt:** „Ich habe gelernt, dass ein eigener Worktree den Superpowers-Versuch vom Matt-Versuch und von main trennt. Die Baseline-Prüfung zeigt, dass das Projekt vor der neuen Implementierung funktioniert, sodass spätere Fehler eindeutig unseren Änderungen zugeordnet werden können.“
- **Nachweis:** Branch `experiment/superpowers`; Ausgangscommit `e7d8b5e`; Setup-Commit `1c4b35a`; Skill-Commit `a419016`; obige Testausgaben; dieses Dokument

## Noch ausstehende Benutzerangaben für S0

- Tatsächliche manuelle Arbeitszeit des Benutzers:
- Was war an Isolation und Baseline verständlich oder unverständlich?
- War der Freigabeaufwand angemessen?

### Superpowers S1 — Brainstorming (laufend)

- **Beginn/Ende:** 27. August 2026, nach Abschluss von S0 / laufend
- **Verwendete Skills:** `brainstorming`
- **Erzeugte Artefakte:** `Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md`
- **Gestellte Rückfragen:** 1. Welchen Umfang soll der erste vertikale Moodle-Schnitt haben? 2. Was ist der Hauptgrund für diese Wahl? 3. Wie sollen erkannte Moodle-Inhalte zu persönlichen Aufgaben werden? 4. Was ist der Hauptgrund für diese Aufgabenregel? 5. Wie weit soll der Zustand `Prüfung erforderlich` im ersten Schnitt bedienbar sein? 6. Welchem persönlichen Lernmodul sollen automatisch erzeugte Aufgaben zugeordnet werden? 7. Wie wird derselbe externe Kurs trotz unterschiedlicher Links kanonisch erkannt? 8. Wie wirken Änderungen externer Inhalte auf bereits erzeugte Aufgaben? 9. Wann gilt eine externe Frist im ersten Schnitt als verlässlich? 10. Wie erhält ein später Abonnent bereits bekannte Kursinhalte? 11. Welcher Architekturansatz soll den bestätigten Schnitt tragen? 12. Ist der Designabschnitt zu Systemgrenzen und Datenmodell korrekt? 13. Ist der Designabschnitt zu Registrierung und Scanablauf korrekt? 14. Ist der Designabschnitt zu Fehlern und sicheren Zustandsänderungen korrekt? 15. Ist der Designabschnitt zu API und Benutzeroberfläche korrekt? 16. Ist der Designabschnitt zu Tests, Abnahme und ausgeschlossenem Umfang korrekt? 17. Dürfen automatisch erzeugte Aufgaben lokal bearbeitet oder gelöscht werden? 18. Schriftliche Prüfung der Designspezifikation. Antwort auf Frage 18 ausstehend.
- **Wichtige Entscheidungen:** **Vom Benutzer bestätigt:** Variante A, ein lokaler End-to-End-Schnitt. Er umfasst Kursregistrierung über die UI, manuellen Scan einer deterministischen Mock-Quelle, sichtbare erkannte Inhalte sowie automatisierte Nachweise für Deduplizierung und mehrere Abonnenten. Reale Moodle-Anbindung, Scheduler und reale Benachrichtigungen sind zunächst ausgeschlossen.

  **Begründung des Benutzers:** „Ich bevorzuge den lokalen End-to-End-Schnitt, weil ich damit den vollständigen Ablauf vom Registrieren eines Kurses bis zur erzeugten Aufgabe testen kann. Der Mock macht die Tests reproduzierbar und unabhängig von Zugangsdaten oder der Verfügbarkeit eines echten Moodle-Systems, während trotzdem Architektur, Benutzeroberfläche und Deduplizierung gemeinsam geprüft werden.“

  **Vom Benutzer bestätigte Aufgabenregel:** Neue externe Inhalte erzeugen pro Abonnent genau dann automatisch eine persönliche Aufgabe, wenn die Quelle eine verlässliche Abgabefrist liefert. Inhalte ohne oder mit unsicherer Frist erzeugen keine Aufgabe und werden als `Prüfung erforderlich` markiert.

  **Begründung des Benutzers:** „Eine automatisch erzeugte Aufgabe soll zuverlässig sein, damit normale Hinweise oder andere Inhalte nicht fälschlich als Aufgaben erscheinen. Wenn die Moodle-Frist unsicher ist, soll das Programm den Inhalt lieber zur Prüfung markieren, statt eine möglicherweise falsche Aufgabe mit falschem Termin anzulegen.“

  **Vom Benutzer bestätigter Umfang des Prüfzustands:** Ein als `Prüfung erforderlich` markierter Inhalt zeigt im ersten Schnitt Titel, Quelle und den Grund der Unsicherheit. Er kann dort noch nicht bestätigt oder in eine Aufgabe umgewandelt werden.

  **Vom Benutzer bestätigte Modulzuordnung:** Beim ersten Abonnement wird für den Benutzer automatisch ein persönliches `StudyModule` mit dem erkannten Kursnamen angelegt. Das `CourseSubscription` verbindet dieses Modul mit dem gemeinsam gespeicherten `ExternalCourse`. Mehrere Abonnenten teilen dadurch den externen Kurs und seine Scans, besitzen aber getrennte Module und Aufgaben.

  **Vom Benutzer bestätigte Kursidentität:** Der Mock-Adapter liefert eine stabile Identität aus `provider + externalCourseId`. Der eingegebene Link ist nicht der eindeutige Kursschlüssel; unterschiedliche Links dürfen auf denselben gemeinsam gespeicherten Kurs und denselben Scanlauf zeigen.

  **Vom Benutzer bestätigtes Änderungsverhalten:** Bei unveränderter externer Inhalts-ID werden Titel, Beschreibung und Frist einer offenen automatisch erzeugten Aufgabe mit der Quelle synchronisiert. Aufgaben-ID und Bearbeitungsstatus bleiben erhalten. Erledigte Aufgaben werden nicht nachträglich verändert.

  **Vom Benutzer bestätigte Vertrauensgrenze:** Eine Frist gilt im ersten Schnitt nur dann als verlässlich, wenn der Adapter für einen als Aufgabe normalisierten Inhalt ein strukturiertes Fristfeld liefert. Datumsangaben in Titel oder Beschreibung werden nicht interpretiert; Confidence-Schwellen und LLM-Erkennung sind ausgeschlossen.

  **Vom Benutzer bestätigtes Verhalten für späte Abonnenten:** Ein neuer Abonnent erhält ohne erneuten externen Abruf persönliche Aufgaben aus den noch relevanten Inhalten des letzten erfolgreichen gemeinsamen Snapshots. Die Zuordnung bleibt pro Abonnement und externem Inhalt idempotent.

  **Vom Benutzer bestätigter Architekturansatz:** Explizites gemeinsames Domänenmodell mit relational gespeicherten Kursen, Abonnements, normalisierten Inhalten, Scanläufen und idempotenten Zuordnungen zu persönlichen Aufgaben. Snapshot-JSON und Ereignisprotokoll sind für den ersten Schnitt verworfen.

  **Vom Benutzer bestätigter Designabschnitt 1 — Systemgrenzen und Datenmodell:** `ExternalCourse` ist gemeinsam; `CourseSubscription`, persönliches `StudyModule`, `ExternalTaskLink` und `StudyTask` trennen die Benutzerdaten. `ExternalContent` repräsentiert den letzten erfolgreichen normalisierten Zustand; `ScanRun` dokumentiert Abrufe. Der Adapter-Port liefert Discovery und Snapshot, im ersten Schnitt ausschließlich über einen Mock-Adapter.

  **Vom Benutzer bestätigter Designabschnitt 2 — Registrierung und Scan:** Discovery legt Kurs, Abonnement und persönliches Modul idempotent an. Späte Abonnenten verwenden relevante Inhalte des letzten erfolgreichen Snapshots. Ein manueller gemeinsamer Scan validiert und vergleicht den Snapshot vollständig und schreibt Inhalte, Aufgabenverknüpfungen und erfolgreichen Scanlauf atomar.

  **Vom Benutzer bestätigter Designabschnitt 3 — Fehler und Sicherheit:** Ungültige Registrierung erzeugt keine Daten. Fehlerhafte oder nicht vertrauenswürdige Scans erhalten den letzten erfolgreichen Zustand und protokollieren nur sichere Fehlerdaten. Gleichzeitige Scans werden verhindert; verschwundene Inhalte löschen keine Aufgaben; verlorene Fristverlässlichkeit führt zu `Prüfung erforderlich`.

  **Vom Benutzer bestätigter Designabschnitt 4 — API und UI:** Eine geschützte, zweisprachige Moodle-Kursansicht registriert Fixture-Links, listet eigene Abonnements und Inhalte und startet gemeinsame manuelle Scans. Erzeugte Aufgaben erscheinen mit Moodle-Quellenhinweis im automatisch angelegten persönlichen Modul. Benutzerbezogene Endpunkte autorisieren stets über das Abonnement.

  **Vom Benutzer bestätigter Designabschnitt 5 — Tests und Abnahme:** Kontrollierbare Ausgangs-, Änderungs- und Fehlerzustände des Mock-Adapters belegen Registrierung, gemeinsamen Scan, Deduplizierung, Aufgabenregeln, Synchronisierung, späte Abonnenten, Fehlererhalt und Autorisierung. Reale Moodle-Zugänge, Scheduler, Benachrichtigungen, LLM, manueller Prüfworkflow, Abonnementlöschung und iOS sind ausgeschlossen.

  **Vom Benutzer bestätigte Konsistenzregel:** Moodle-gesteuerte Aufgaben erlauben nur Statusänderungen. Titel, Beschreibung, Frist, Bearbeiten und Löschen bleiben quellengesteuert. Der Spec-Self-Review ergänzte daraus folgend den Löschschutz für das mit einem aktiven Abonnement verbundene persönliche Modul; andere Modulfelder bleiben persönlich bearbeitbar.
- **Fehlversuche oder Blocker:** Keine
- **Korrekturen durch den Benutzer:** Keine
- **Besonders hilfreich:** Die abschnittsweisen Freigaben hielten Produktentscheidungen sichtbar; der vorgeschriebene Spec-Self-Review fand die zuvor nicht geregelte Löschung des verknüpften persönlichen Moduls.
- **Unnötig oder zu aufwendig:** Noch zu bewerten
- **Vom Skill übersehen:** Noch zu bewerten
- **Was ich selbst gelernt habe:** Nach Abschluss von S1 vom Benutzer zu ergänzen
- **Nachweis:** Dieses Beobachtungslog; späteres Designdokument
