# Paper und kompaktes Quellenpaket

## Weitergeben

`output/pdf/agent-skills-fallstudie-quellenpaket.zip` enthält ausschließlich:

1. `agent-skills-fallstudie.pdf` - sechsseitige Projektstudie mit allen 14 Anforderungen und vier Abbildungen.
2. `Anhang.md` - vollständige Anforderungen und Bewertungsmethode.
3. `experiment-protocol.md` - das vollständige archivierte Versuchsprotokoll.
4. `Nachweise.md` - relevante Abschlussbefunde, bestätigte Bewertungen und Quellenübersicht.

Alle vier Dateien liegen direkt im ZIP, ohne verschachtelte Ordner. Zum Lesen
das ZIP vollständig entpacken. Die beiden PDF-Links zeigen auf den Anhang und
das Protokoll daneben. Der sichtbare PDF-Inhalt bleibt gegenüber der separaten
Ausgabe unverändert. Je nach PDF-Programm müssen Markdown-Dateien manuell
geöffnet werden.

## Bewusst nicht enthalten

Erzeugungsskripte, Prüfsummen-Manifeste, die Markdown-Doppelung des Papers,
separate PNG-/SVG-Diagramme, vollständige Arbeitslogs und ältere Protokollstände
werden nicht mitgeliefert. Die Diagramme sind bereits im PDF enthalten.

Diese Originalunterlagen werden nicht gelöscht: Sie bleiben im Repository.
Die kompakte Nachweisdatei fasst die relevanten Befunde zusammen und verlinkt
festgeschriebene GitHub-Stände. Für die vollständigen Originale können Internet
und Repository-Berechtigung erforderlich sein. Das Paket ist eine Leseausgabe,
kein vollständiges Offline-Archiv aller Entwicklungsschritte.

## Paket und Quellen pflegen

Die Paketkopien bekommen passende relative Links. Verweise auf ausgelassene
Originaldokumente führen auf deren festgeschriebene GitHub-Stände. Das gilt auch
für die Architektur-Arbeitsnotiz im Versuchsprotokoll; dessen übriger Inhalt
bleibt unverändert. Historische Nachweise werden nicht verändert. Die aktuelle
Benutzerbewertung ist von den ursprünglichen Urteilen getrennt dokumentiert;
ihre Bestätigung steht im Repository unter `PAPER-BEWERTUNG.md`.

Im Repository erzeugt dieser Befehl nur das kompakte ZIP neu:

```bash
python3 Docs/skill-evaluation/tools/build_paper.py --package-only
```

Ohne `--package-only` werden zusätzlich PDF und Diagramme neu erzeugt. Der
Builder ist absichtlich nicht Teil des Bewerbungspakets. Er benötigt die
historischen Quellen im Repository und Python mit ReportLab, pypdf, pypdfium2
und Pillow; der vollständige PDF-Neuaufbau außerdem Liberation-Sans-Schriften.

Die Paketprüfung kontrolliert die vier erlaubten Dateien, lokale Links,
bestätigte Bewertungen und pixelgleichen PDF-Inhalt nach Anpassung der Links.
Der vollständige Builder prüft außerdem alle sieben FR und NFR im Hauptpapier
und verwendet für den Punktvergleich dieselben Werte wie für die Tabellen.
Es werden keine Produkttests oder Netzwerkabfragen ausgeführt.
