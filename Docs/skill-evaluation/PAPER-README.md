# Paper und Quellenpaket

## Lesen und weitergeben

1. Für die Lektüre: `output/pdf/agent-skills-fallstudie.pdf` vom Repository-Root.
2. Für die Überprüfung: `output/pdf/agent-skills-fallstudie-quellenpaket.zip`.
3. Das ZIP vollständig entpacken. Es enthält das PDF, die überarbeitete
   Markdown-Datei, beide Diagramme und sämtliche im Paper als Q1 bis Q7
   referenzierten Nachweise. Die Verzeichnisstruktur darf für relative Links
   nicht verändert werden.

Im ZIP liegt das PDF direkt im Hauptverzeichnis. Die Markdown-Quelle befindet
sich unter `Docs/skill-evaluation/bewerbungs-paper-agentische-skill-suites.md`.
Das bisher bei der Einzelweitergabe fehlende `experiment-protocol.md` liegt
direkt daneben. Auch die darin verlinkte Architektur-Arbeitsnotiz ist enthalten.
Ein GitHub-Zugang ist zum Lesen der mitgelieferten Nachweise nicht erforderlich.

## Quellenstatus

Die historischen Logs wurden für diese Überarbeitung nicht umgeschrieben.
`references/` enthält Exporte aus festgeschriebenen Git-Commits. Die vollständigen
Commit-IDs, Originalpfade, Exportpfade und SHA-256-Prüfsummen stehen in
`references/source-manifest.json`. `package-manifest.json` im ZIP prüft zusätzlich
die mitgelieferten Dateien. Diese Prüfsummen sichern Dateiintegrität, nicht die
inhaltliche Wahrheit einer Beobachtung.

Die FR/NFR-Matrix und die Erklärung der Bewertung sind nachträgliche
Zusammenstellungen aus den historischen Quellen. Die alten Punktewerte und
Versuchslogs bleiben unverändert. Die Skala war subjektiv; nur Übernahme,
Gewichtung und Berechnung der Punkte sind reproduzierbar.

## Umsetzung des Feedbacks

| Rückmeldung | Änderung |
| --- | --- |
| Mindestens zwei Diagramme | Gemeinsamer Scan mit persönlichen Aufgaben; isolierter Versuchsaufbau mit getrennten Feature-Umfängen. PNG für Markdown, SVG als Vektorquelle, Vektoren im PDF. |
| Saubere FR und NFR | Abschnitt 2 enthält identifizierbare Anforderungen, Abnahmeszenarien und Quellen; Unterschiede der Varianten bleiben sichtbar. |
| Bewertung erklären | Abschnitt 4 nennt Herkunft, Zeitpunkt, Skala, Begründung aller 14 Werte und die Grenzen des Determinismus. |
| Fehlendes Protokoll | Das vollständige ZIP enthält `experiment-protocol.md` sowie die weiteren zitierten Markdown-Nachweise. |
| Fazit vor Lernerfahrungen; Tokenlimit | Abschnitt 5 vor Abschnitt 6; begrenztes Kontingent und fehlende vollständige Tokenbilanz sind ausdrücklich benannt. |
| Fazit länger und strukturiert | Sechs Fazit-Unterabschnitte; längster nummerierter Hauptabschnitt des Papers. |
| Fließtext strukturieren | Kurze Absätze, präzise Zwischenüberschriften, Anforderungs- und Begründungstabellen, nummerierte Handlungsempfehlung. |

## Reproduktion

Voraussetzungen: Python 3, `reportlab`, `pypdf`, `pypdfium2` und `Pillow` sowie
lokale Liberation-Sans-Schriftdateien (Regular, Bold, Italic und BoldItalic).
`PAPER_FONT_DIR` kann auf deren Verzeichnis zeigen. Der Builder sucht sonst in
der lokalen Codex-Laufzeit und gängigen Systemverzeichnissen. Es erfolgen keine
Netzwerkaufrufe und keine Produktcodeänderungen.

Vom Repository-Root:

```bash
python3 Docs/skill-evaluation/tools/build_paper.py
```

Im ursprünglichen Repository werden historische Quellen aus den dokumentierten
Commits exportiert. Im entpackten ZIP werden vorhandene Exporte gegen ihre
SHA-256-Prüfsummen geprüft; eine Git-Historie ist dort nicht erforderlich.

Der Builder prüft Referenzen, unveränderte Ratings, Kapitelreihenfolge und
Fazitlänge. Er erstellt Diagramme, PDF, Seitenrenderings, das Quellen-ZIP und
einen maschinenlesbaren Prüfbericht. Die Seitenrenderings unter `tmp/pdfs/`
dienen der visuellen Kontrolle und werden nicht mitgeliefert.

Die PDF-Layoutprüfung ersetzt keine erneute Produktprüfung. Die Testzahlen im
Paper stammen ausdrücklich aus den historischen Abschlusslogs.
