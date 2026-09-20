# Superpowers-Produktwechsel: Daten und Freigabe

Issue #99 ersetzt die Matt-Produktoberfläche durch Superpowers. Die beiden
Kursimport-Modelle sind nicht kompatibel; deshalb gibt es keine automatische
Umdeutung alter Kurse oder Scan-Ergebnisse.

## Was die Migration macht

- Bestehende Konten, Profile, Lernmodule und Aufgaben bleiben in `public`.
  Aufgaben ohne Frist behalten `NULL`; es wird kein künstliches Datum gesetzt.
- Alle acht Matt-Kursimport-Tabellen werden innerhalb einer Transaktion nach
  `legacy_matt` verschoben. Die alten Datensätze bleiben damit in einem
  vollständigen Datenbank-Backup und für eine gezielte spätere Auswertung
  verfügbar, aber nicht in der neuen Oberfläche.
- Superpowers erhält leere eigene Kurs-, Abonnement-, Inhalts-, Scan- und
  Aufgabenlink-Tabellen. Nutzer müssen unterstützte Mock-Kurslinks erneut
  registrieren. Ein bestehendes Matt-Modul wird dabei nicht automatisch
  zugeordnet; Superpowers legt ein neues persönliches Modul an.
- Falls die erwartete Matt-Struktur fehlt oder das Archivschema bereits
  existiert, bricht die Migration ab. Sie darf nicht auf eine Datenbank mit
  der separaten `experiment/superpowers`-Migrationshistorie angewendet werden.

## Freigabe vor dem Live-Wechsel

1. Aktuellen Produktionsstand und die Tabelle `__EFMigrationsHistory` prüfen.
   Ein vollständiges, rückspielbares PostgreSQL-Backup erstellen und den
   Wiederherstellungsweg auf einer Kopie testen.
2. Auf einer Kopie der echten Daten das neue Image starten. Vorher/nachher
   Konten, Module, Aufgaben und alle acht archivierten Tabellen zählen.
   Aufgaben ohne Frist, Referenzen und Stichproben aus Kursen prüfen.
3. Backend-, Frontend-, Layout-, Golden-Path- und
   `scripts/test-production-migrations.sh`-Prüfungen grün bestätigen.
4. Erst dann den Produktwechsel separat freigeben. Nach dem Start Anmeldung,
   persönliche Aufgaben, Kurs-Neuregistrierung und Scan prüfen.

Ein Rollback ist **kein** EF-`Down`: Wegen neuer Nutzerdaten nach dem Wechsel
ist nur ein koordinierter Restore des Backups zusammen mit dem alten Image
sicher. Die Migration verweigert deshalb ein automatisches `Down`.

Das hier getestete Fixture ist keine echte Moodle-Anbindung. Der unterstützte
Superpowers-Link lautet
`https://mock-moodle.local/course/view.php?id=se-2026`.
