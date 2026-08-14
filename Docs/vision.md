# Produktvision: Study Organizer

## Vision

Study Organizer bietet Studierenden einen ruhigen, übersichtlichen Ort für Lernmodule, Aufgaben und Fristen. Die Anwendung reduziert organisatorischen Aufwand und hilft dabei, offene, überfällige und erledigte Aufgaben jederzeit im Blick zu behalten.

## Zielgruppe

- Studierende, die ihre Module und Aufgaben persönlich organisieren möchten
- Studierende mit mehreren parallelen Lehrveranstaltungen und Fristen
- Nutzer, die dieselben Daten künftig im Web und auf iOS verwenden möchten

## Nutzenversprechen

Study Organizer verbindet eine einfache Bedienung mit einer klar getrennten, erweiterbaren Architektur. Persönliche Daten werden einem Benutzerkonto zugeordnet und sind nur nach Anmeldung erreichbar. Eine zweisprachige Oberfläche macht die Anwendung für deutsch- und englischsprachige Nutzer zugänglich.

## Aktuell umgesetzter Produktumfang

- sichere Registrierung und Anmeldung mit ASP.NET Core Identity und JWT
- Wiederherstellung der lokalen Anmeldung nach einem Seiten-Neuladen
- persönliches Profil mit unveränderbarer E-Mail-Adresse sowie änderbaren Profildaten
- Änderung des Passworts nach Prüfung des aktuellen Passworts
- vollständige Verwaltung persönlicher Lernmodule
- vollständige Verwaltung der Aufgaben eines Lernmoduls
- Statuswechsel zwischen offen und erledigt
- Fälligkeitsdatum und Kennzeichnung überfälliger Aufgaben
- Dashboard mit Zusammenfassung und nächsten Aufgaben
- deutsche und englische Weboberfläche
- PostgreSQL-Persistenz über Entity Framework Core

## Produktgrenze der aktuellen Version

Die aktuelle Version besteht aus einem Vue-Webfrontend, einer ASP.NET-Core-API und einer PostgreSQL-Datenbank. Eine native iOS-App ist geplant, aber noch nicht implementiert.

## Qualitätsziele

- verständliche Bedienung auf Desktop und mobilen Bildschirmgrößen
- sichere Authentifizierung und konsequente Trennung der Benutzerdaten
- wartbare Schichten für API, Anwendung, Domäne und Infrastruktur
- reproduzierbare lokale Entwicklung mit Docker Compose
- automatisierte Tests für zentrale Backend- und Frontend-Funktionen
- Erweiterbarkeit für weitere Clients und Funktionen

## Nächste Ausbaustufen

1. native iOS-App mit SwiftUI und derselben REST-API
2. Push-Benachrichtigungen für Fristen
3. Notizen, Anhänge, Kalender und Stundenplan
4. erweiterte Auswertungen des Lernfortschritts
5. optionale Zusammenarbeit in Lerngruppen
