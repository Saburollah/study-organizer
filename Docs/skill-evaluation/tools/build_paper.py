#!/usr/bin/env python3
"""Build the case-study PDF, vector figures and a compact reader bundle.

No network calls, application tests or product changes. Historical inputs are
exported by immutable git revision, or verified from the supplied manifest.
"""
from __future__ import annotations

import hashlib
import html
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import zipfile

from PIL import Image, ImageOps, ImageDraw
import pypdfium2 as pdfium
from pypdf import PdfReader, PdfWriter
from pypdf.generic import NameObject, TextStringObject
from reportlab import rl_config
from reportlab.graphics import renderPDF, renderSVG
from reportlab.graphics.shapes import Drawing, Rect, Line, Polygon, String, Circle
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether,
)

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'Docs/skill-evaluation'
OUT = ROOT / 'output/pdf'
QA = ROOT / 'tmp/pdfs/skill-paper'
PAPER = BASE / 'bewerbungs-paper-agentische-skill-suites.md'
PDF_NAME = 'agent-skills-fallstudie.pdf'
PAGE_COUNT = 6
INK = colors.HexColor('#162C40')
TEAL = colors.HexColor('#087F82')
MATT = colors.HexColor('#6257A5')
MUTED = colors.HexColor('#526579')
PALE = colors.HexColor('#F1F5F7')
LINE = colors.HexColor('#D5DFE5')
CRITERIA = ['Verständlichkeit', 'Kontrolle', 'Lerngewinn', 'Angemessener Aufwand',
            'Vertrauen', 'Wiederaufnahme', 'Anpassbarkeit']


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def confirmed_ratings():
    """User-confirmed reflection, separate from immutable experiment evidence."""
    document = (BASE / 'PAPER-BEWERTUNG.md').read_text()
    result = [[], []]
    for criterion in CRITERIA:
        row = next(line for line in document.splitlines() if line.startswith('| '+criterion+' |'))
        points = [int(n) for n in re.findall(r'\| ([1-5]) ', row)]
        assert len(points) == 2, criterion
        for i, point in enumerate(points):
            result[i].append(point)
    return result


def fonts():
    task_home = Path.home()
    candidates = [Path(os.environ['PAPER_FONT_DIR'])] if os.environ.get('PAPER_FONT_DIR') else []
    candidates += [
        task_home / '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/pdfjs-dist/standard_fonts',
        Path('/usr/share/fonts/truetype/liberation2'),
        Path('/usr/share/fonts/truetype/liberation'),
    ]
    folder = next((d for d in candidates if (d / 'LiberationSans-Regular.ttf').is_file()), None)
    if folder is None:
        raise RuntimeError('Set PAPER_FONT_DIR to a directory containing LiberationSans *.ttf')
    for name, suffix in [('Body', 'Regular'), ('BodyBold', 'Bold'),
                         ('BodyItalic', 'Italic'), ('BodyBoldItalic', 'BoldItalic')]:
        pdfmetrics.registerFont(TTFont(name, str(folder / f'LiberationSans-{suffix}.ttf')))
    pdfmetrics.registerFontFamily('Body', normal='Body', bold='BodyBold',
                                  italic='BodyItalic', boldItalic='BodyBoldItalic')


def source_snapshots():
    """Keep repository-relative layouts so links inside exported MD remain valid."""
    manifest_path = BASE / 'references/source-manifest.json'
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    commits = {'matt': 'ab8249c', 'superpowers': 'a8801ff', 'comparison': '866e724'}
    paths = {
        'matt': ['Docs/skill-evaluation/matt-observations.md',
                 'Docs/skill-evaluation/experiment-protocol.md', 'CONTEXT.md'],
        'superpowers': ['Docs/skill-evaluation/superpowers-observations.md',
                       'Docs/skill-evaluation/agent-activity-log.md',
                       'Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md'],
        'comparison': ['Docs/skill-evaluation/matt-vs-superpowers-comparison.md'],
    }
    try:
        subprocess.check_output(['git', 'cat-file', '-e', 'a8801ff^{commit}'], cwd=ROOT, stderr=subprocess.DEVNULL)
    except (subprocess.CalledProcessError, FileNotFoundError):
        data = json.loads(manifest_path.read_text())
        for entry in data['sources']:
            assert sha((ROOT / entry['export']).read_bytes()) == entry['sha256'], entry['export']
        return data
    entries = []
    visited = set()

    def export(group, revision, path, dest=None):
        destination = dest or BASE / 'references' / group / path
        if str(destination) in visited:
            return
        visited.add(str(destination))
        full = subprocess.check_output(['git', 'rev-parse', revision + '^{commit}'], cwd=ROOT, text=True).strip()
        data = subprocess.check_output(['git', 'show', f'{full}:{path}'], cwd=ROOT)
        destination.parent.mkdir(parents=True, exist_ok=True)
        # Existing non-export sources belong to the user: never overwrite a differing file.
        if dest and destination.exists():
            assert destination.read_bytes() == data, f'Local source differs: {destination}'
        else:
            destination.write_bytes(data)
        entries.append({'group': group, 'commit': full, 'original': path,
                        'export': str(destination.relative_to(ROOT)), 'sha256': sha(data)})
        for target in re.findall(r'(?<!!)\[[^\]]*\]\(([^)]+)\)', data.decode()):
            target = target.split('#')[0]
            if not target or '://' in target or target.startswith('mailto:'):
                continue
            source = os.path.normpath(str(Path(path).parent / target))
            child_dest = destination.parent / target
            export(group, revision, source, child_dest.resolve())

    for group, items in paths.items():
        for path in items:
            export(group, commits[group], path)
    export('protocol', 'a8801ff', 'Docs/skill-evaluation/experiment-protocol.md', BASE / 'experiment-protocol.md')
    result = {'purpose': 'Unmodified historical evidence; integrity hashes are not truth certificates.',
              'sources': entries}
    manifest_path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    return result


def text(d, x, y, value, size=20, color=INK, bold=False, anchor='start'):
    d.add(String(x, y, value, fontName='BodyBold' if bold else 'Body',
                 fontSize=size, fillColor=color, textAnchor=anchor))


def box(d, x, y, w, h, title, lines=(), accent=TEAL):
    d.add(Rect(x, y, w, h, rx=10, ry=10, fillColor=colors.white, strokeColor=LINE, strokeWidth=1.5))
    d.add(Rect(x, y + h - 7, w, 7, fillColor=accent, strokeColor=None))
    text(d, x+18, y+h-36, title, 20, bold=True)
    for i, line in enumerate(lines):
        text(d, x+18, y+h-65-i*25, line, 18, MUTED)


def arrow(d, points, color=TEAL):
    for a, b in zip(points, points[1:]):
        d.add(Line(a[0], a[1], b[0], b[1], strokeColor=color, strokeWidth=2.4))
    a, b = points[-2:]
    if b[0] > a[0]:
        vertices = [b[0], b[1], b[0]-10, b[1]+5, b[0]-10, b[1]-5]
    elif b[0] < a[0]:
        vertices = [b[0], b[1], b[0]+10, b[1]+5, b[0]+10, b[1]-5]
    elif b[1] < a[1]:
        vertices = [b[0], b[1], b[0]-5, b[1]+10, b[0]+5, b[1]+10]
    else:
        vertices = [b[0], b[1], b[0]-5, b[1]-10, b[0]+5, b[1]-10]
    d.add(Polygon(vertices, fillColor=color, strokeColor=None))


def architecture():
    d = Drawing(940, 418)
    pale_teal = colors.HexColor('#EAF5F4')
    text(d, 16, 391, 'DIE QUELLE', 16, MUTED, bold=True)
    text(d, 263, 391, 'EINMAL GEMEINSAM VERARBEITEN', 16, TEAL, bold=True)
    text(d, 691, 391, 'PERSÖNLICH ORGANISIEREN', 16, MUTED, bold=True)

    # Source documents: concrete content, not a generic process box.
    for y, label in [(302, 'PDF-Aufgabe'), (226, 'Link'), (150, 'Aktivität')]:
        d.add(Rect(16, y-28, 184, 62, rx=9, ry=9, fillColor=PALE, strokeColor=None))
        d.add(Polygon([33,y-13, 33,y+20, 53,y+20, 63,y+10, 63,y-13],
                      fillColor=colors.white, strokeColor=MUTED, strokeWidth=1.4))
        d.add(Line(53,y+20,53,y+10,strokeColor=MUTED,strokeWidth=1.4))
        d.add(Line(53,y+10,63,y+10,strokeColor=MUTED,strokeWidth=1.4))
        for yy in [y+4,y-3]:
            d.add(Line(39,yy,56,yy,strokeColor=MUTED,strokeWidth=1))
        text(d, 76, y-3, label, 19)
    text(d, 16, 94, 'Kontrollierter Mock-Kurs', 16, MUTED)

    # The shared state is deliberately visually dominant.
    d.add(Rect(248, 100, 356, 262, rx=15, ry=15, fillColor=INK, strokeColor=None))
    text(d, 270, 329, 'Gemeinsamer Kurszustand', 23, colors.white, bold=True)
    steps = [(282, 'Inhalte vereinheitlichen', 'Adapter für verschiedene Formate'),
             (212, 'Änderungen erkennen', 'Stabile IDs statt Dateinamen'),
             (142, 'Gültigen Stand übernehmen', 'Bei Fehlern bleibt der alte Stand')]
    for n, (y, title, detail) in enumerate(steps, 1):
        d.add(Circle(280, y, 12, fillColor=TEAL, strokeColor=None))
        text(d, 280, y-5, str(n), 14, colors.white, True, 'middle')
        text(d, 304, y-3, title, 20, colors.white, bold=True)
        text(d, 304, y-27, detail, 16, colors.HexColor('#BED2DD'))
    d.add(Line(222,153,222,305,strokeColor=MUTED,strokeWidth=2.4))
    for y in [153,229,305]:
        d.add(Line(200,y,222,y,strokeColor=MUTED,strokeWidth=2.4))
    arrow(d, [(222,229),(248,229)], MUTED)

    # The branch is routed outside cards; no connector crosses a label.
    d.add(Line(604,232,640,232,strokeColor=TEAL,strokeWidth=2.4))
    d.add(Line(640,144,640,316,strokeColor=TEAL,strokeWidth=2.4))
    for y, name in [(316,'A'), (230,'B'), (144,'C')]:
        arrow(d, [(640,y),(683,y)], TEAL)
        d.add(Rect(683,y-33,241,66,rx=10,ry=10,fillColor=pale_teal,strokeColor=None))
        d.add(Circle(711,y+9,8,fillColor=TEAL,strokeColor=None))
        d.add(Rect(699,y-14,24,13,rx=6,ry=6,fillColor=TEAL,strokeColor=None))
        text(d, 741, y+6, f'Person {name}', 20, bold=True)
        text(d, 741, y-17, 'Eigene Aufgabe', 18, MUTED)
    d.add(Line(16,72,924,72,strokeColor=LINE,strokeWidth=1))
    for x, number, label in [(16,'1','gemeinsamer Abruf'),
                              (333,'3','persönliche Aufgaben'),
                              (650,'0','Duplikate bei Wiederholung')]:
        text(d, x, 23, number, 35, TEAL, bold=True)
        text(d, x+42, 30, label, 18, MUTED)
    return d


def experiment():
    """Original setup diagram, with only the duplicated workflow rows removed."""
    setup_matt = colors.HexColor('#B15C39')
    d = Drawing(940, 502)
    d.add(Rect(0, 0, 940, 502, fillColor=PALE, strokeColor=None, rx=14, ry=14))
    text(d, 24, 467, 'GLEICHE CODEBASIS, GETRENNTE VERSUCHE', 23, bold=True)
    box(d, 264, 356, 412, 84, 'Gemeinsamer Start: e7d8b5e', ['Mock-Quelle + Versuchsprotokoll'])
    arrow(d, [(470, 356), (470, 334), (236, 334), (236, 307)], setup_matt)
    arrow(d, [(470, 334), (704, 334), (704, 307)], TEAL)
    box(d, 24, 138, 422, 167, '01  Matt Pocock Skills', [], setup_matt)
    box(d, 494, 138, 422, 167, '02  Superpowers', [], TEAL)
    left = ['matt-v1.0  |  Skill f6de92c', 'Asynchroner Scan, Historie, Cleanup',
            'Nachweisstand: ab8249c']
    right = ['superpowers-v6.3.0  |  Skill a419016', 'Synchroner, enger begrenzter Schnitt',
             'Nachweisstand: a8801ff']
    for i, label in enumerate(left): text(d, 42, 236-i*29, label, 19, MUTED)
    for i, label in enumerate(right): text(d, 512, 236-i*29, label, 19, MUTED)
    arrow(d, [(235, 138), (235, 105), (470, 105), (470, 78)], setup_matt)
    arrow(d, [(705, 138), (705, 105), (470, 105)], TEAL)
    text(d, 470, 51, 'Vergleich von Abläufen und Nachweisen - kein Produktmerge', 21, bold=True, anchor='middle')
    text(d, 470, 20, 'Nacheinander durchgeführt; Umfang, Umgebung und Vorwissen sind nicht identisch.', 17, MUTED, anchor='middle')
    return d


def workflow():
    """Retain the original workflow figure alongside the setup diagram."""
    d = Drawing(940, 278)
    lanes = [(156, 'Matt Pocock', 'Entscheidungstiefe', MATT, '#F0EFF8',
              [('Offene Fragen klären', 'Wayfinder + Grilling'),
               ('Architektur festhalten', 'ADRs + Abnahmekriterien'),
               ('Soll und Code prüfen', 'TDD + Spezifikationsreview')]),
             (40, 'Superpowers', 'Umsetzungsfluss', TEAL, '#EAF5F4',
              [('Ziel konkretisieren', 'Brainstorming'),
               ('Schrittweise umsetzen', 'Plan + TDD'),
               ('Gesamtweg abnehmen', 'Review + Sichttest')])]
    for y, name, focus, color, bg, nodes in lanes:
        d.add(Rect(0,y-10,940,100,rx=12,ry=12,fillColor=colors.HexColor(bg),strokeColor=None))
        d.add(Rect(0,y-10,5,100,fillColor=color,strokeColor=None))
        text(d, 19, y+49, name, 22, color, bold=True)
        text(d, 19, y+20, focus, 16, MUTED)
        for i, (title, desc) in enumerate(nodes):
            x = 209+i*239
            d.add(Rect(x,y,218,78,rx=8,ry=8,fillColor=colors.white,strokeColor=None))
            text(d, x+13, y+49, title, 19, bold=True)
            text(d, x+13, y+22, desc, 16, MUTED)
            if i < 2: arrow(d, [(x+218,y+39),(x+239,y+39)], color)
    text(d, 0, 264, 'ZWEI WEGE VON DER KLÄRUNG ZUR ABNAHME', 18, MUTED, bold=True)
    return d


def rating_comparison():
    """Paired categorical dots, never a trend line across unrelated criteria."""
    d = Drawing(940, 348)
    values = confirmed_ratings()
    text(d,16,328,'PERSÖNLICHES BEWERTUNGSPROFIL',18,MUTED,bold=True)
    d.add(Circle(563,333,6,fillColor=MATT,strokeColor=None))
    text(d,578,327,'Matt',18,MATT,bold=True)
    d.add(Polygon([724,340,731,333,724,326,717,333],fillColor=TEAL,strokeColor=None))
    text(d,740,327,'Superpowers',18,TEAL,bold=True)
    position = lambda value: 300 + (value-1)*121.25
    for i in range(7):
        if i % 2 == 0:
            d.add(Rect(0,251-i*32,940,32,fillColor=PALE,strokeColor=None))
    for value in range(1,6):
        x=position(value)
        d.add(Line(x,57,x,290,strokeColor=LINE,strokeWidth=1))
        text(d,x,34,str(value),17,MUTED,anchor='middle')
    for i,criterion in enumerate(CRITERIA):
        y=267-i*32
        matt,superpowers=values[0][i],values[1][i]
        xm,xs=position(matt),position(superpowers)
        text(d,16,y-5,criterion,17)
        d.add(Line(xm,y+5,xs,y-5,strokeColor=MUTED,strokeWidth=1.5))
        d.add(Circle(xm,y+5,6,fillColor=MATT,strokeColor=colors.white,strokeWidth=1))
        d.add(Polygon([xs,y+2,xs+7,y-5,xs,y-12,xs-7,y-5],
                      fillColor=TEAL,strokeColor=colors.white,strokeWidth=1))
        label='gleich' if matt==superpowers else ('Matt' if matt>superpowers else 'Super')+f' +{abs(matt-superpowers)}'
        text(d,818,y-5,label,16,MUTED if matt==superpowers else (MATT if matt>superpowers else TEAL))
    text(d,16,7,'1 sehr schlecht  /  3 gemischt  /  5 sehr gut',15,MUTED)
    text(d,925,7,'Höher = günstiger eingeschätzt',15,MUTED,anchor='end')
    return d


def save_figures():
    folder = BASE / 'figures'
    folder.mkdir(exist_ok=True)
    figures = {'01-gemeinsamer-scan': architecture(), '02-versuchsaufbau': experiment(),
               '02-workflowvergleich': workflow(),
               '03-bewertungsvergleich': rating_comparison()}
    for stem, drawing in figures.items():
        renderSVG.drawToFile(drawing, str(folder / f'{stem}.svg'))
        pdf = pdfium.PdfDocument(renderPDF.drawToString(drawing))
        pdf[0].render(scale=2).to_pil().save(folder / f'{stem}.png')
        pdf.close()
    return figures


def inline(value):
    value = html.escape(value)
    value = re.sub(r'`([^`]+)`', r'<font color="#355168">\1</font>', value)
    value = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', value)
    value = re.sub(r'\*([^*]+)\*', r'<i>\1</i>', value)
    def link(m):
        target = m.group(2)
        if not re.match(r'\w+://', target):
            target = 'Docs/skill-evaluation/' + target
        return f'<link href="{target}" color="#087F82">{m.group(1)}</link>'
    return re.sub(r'\[([^\]]+)\]\(([^)]+)\)', link, value)


def build_pdf(figures):
    styles = {
        'quote': ParagraphStyle('quote', fontName='BodyBold', fontSize=13, leading=18, textColor=TEAL, spaceBefore=12, spaceAfter=12, borderPadding=12, backColor=PALE),
        'body': ParagraphStyle('body', fontName='Body', fontSize=10.5, leading=14.5,
                               textColor=INK, spaceAfter=8, splitLongWords=True),
        'title': ParagraphStyle('title', fontName='BodyBold', fontSize=27, leading=31,
                                textColor=INK, spaceAfter=13),
        'subtitle': ParagraphStyle('subtitle', fontName='Body', fontSize=13, leading=18,
                                   textColor=TEAL, spaceAfter=14),
        'h2': ParagraphStyle('h2', fontName='BodyBold', fontSize=16, leading=20,
                             textColor=INK, spaceBefore=14, spaceAfter=8, keepWithNext=True),
        'h3': ParagraphStyle('h3', fontName='BodyBold', fontSize=12, leading=15,
                             textColor=TEAL, spaceBefore=10, spaceAfter=6, keepWithNext=True),
        'cell': ParagraphStyle('cell', fontName='Body', fontSize=9.2, leading=12.2, textColor=INK),
        'head': ParagraphStyle('head', fontName='BodyBold', fontSize=9, leading=12, textColor=colors.white),
        'caption': ParagraphStyle('caption', fontName='BodyItalic', fontSize=9, leading=12,
                                  textColor=MUTED, spaceAfter=12),
        'list': ParagraphStyle('list', fontName='Body', fontSize=10.5, leading=15.1,
                               textColor=INK, leftIndent=13, firstLineIndent=-11, spaceAfter=7),
    }
    width = A4[0] - 96
    story = []
    lines = PAPER.read_text().splitlines()
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        if not line:
            i += 1
            continue
        if line == '<!-- pagebreak -->':
            story.append(PageBreak())
        elif line.startswith('> '):
            story.append(Paragraph(inline(line[2:]), styles['quote']))
        elif line.startswith('# '):
            story.append(Paragraph(inline(line[2:]), styles['title']))
        elif line.startswith('## Matt Pocock Skills'):
            story.append(Paragraph(inline(line[3:]), styles['subtitle']))
        elif line.startswith('## '):
            heading = line[3:]
            story.append(Paragraph(inline(heading), styles['h2']))
        elif line.startswith('### '):
            story.append(Paragraph(inline(line[4:]), styles['h3']))
        elif line.startswith('!['):
            match = re.match(r'!\[.*\]\(([^)]+)\)', line)
            stem = Path(match[1]).stem
            drawing = figures[stem]
            # The two workflow/setup diagrams benefit from slightly larger type.
            # They may use part of the normal margin while remaining centered.
            target_width = width * (1.08 if stem in {'02-versuchsaufbau', '02-workflowvergleich'} else 1)
            scale = target_width / drawing.width
            scaled = Drawing(target_width, drawing.height * scale)
            scaled.add(drawing)
            scaled.scale(scale, scale)
            scaled.hAlign = 'CENTER'
            # Avoid orphaning the caption away from its figure.
            j = i + 1
            while j < len(lines) and not lines[j].strip(): j += 1
            caption = lines[j].strip()
            if caption.startswith('*Abbildung'):
                group = [scaled, Spacer(1, 7), Paragraph(inline(caption), styles['caption'])]
                if story and isinstance(story[-1], Paragraph) and story[-1].style.name == 'h3':
                    group.insert(0, story.pop())
                story.append(KeepTogether(group))
                i = j
            else:
                story.append(scaled)
        elif line.startswith('|'):
            raw = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                row = [x.strip() for x in lines[i].strip().strip('|').split('|')]
                if not all(re.fullmatch(r'[-: ]+', c) for c in row):
                    raw.append(row)
                i += 1
            count = len(raw[0])
            if raw[0][0] == 'ID':
                widths = [56, 193, width-249]
            elif count == 3:
                widths = [116, (width-116)/2, (width-116)/2]
            else:
                widths = [103, 36, 76, width-215]
            rows = [[Paragraph(inline(c), styles['head' if r == 0 else 'cell'])
                     for c in row] for r, row in enumerate(raw)]
            table = Table(rows, colWidths=widths, repeatRows=1, hAlign='LEFT')
            commands = [('BACKGROUND', (0,0), (-1,0), INK),
                        ('VALIGN', (0,0), (-1,-1), 'TOP'),
                        ('LEFTPADDING', (0,0), (-1,-1), 8),
                        ('RIGHTPADDING', (0,0), (-1,-1), 8),
                        ('TOPPADDING', (0,0), (-1,-1), 5),
                        ('BOTTOMPADDING', (0,0), (-1,-1), 5),
                        ('LINEBELOW', (0,0), (-1,0), 1, TEAL)]
            for row in range(1, len(rows)):
                commands += [('BACKGROUND', (0,row), (-1,row), PALE if row % 2 else colors.white),
                             ('LINEBELOW', (0,row), (-1,row), .35, LINE)]
            table.setStyle(TableStyle(commands))
            story += [Spacer(1, 5), table, Spacer(1, 12)]
            continue
        else:
            paragraph = [line]
            while i+1 < len(lines) and lines[i+1].strip() and not re.match(r'^(#|\||!\[|- |\d+\. |<!--|> )', lines[i+1]):
                i += 1
                paragraph.append(lines[i].strip())
            para = ' '.join(paragraph)
            is_list = bool(re.match(r'^(- |\d+\. )', para))
            if para.startswith('- '): para = '• ' + para[2:]
            story.append(Paragraph(inline(para), styles['list' if is_list else 'body']))
        i += 1

    def frame(canvas, doc):
        canvas.saveState()
        canvas.setStrokeColor(TEAL)
        canvas.setLineWidth(2)
        canvas.line(48, A4[1]-31, A4[0]-48, A4[1]-31)
        canvas.setFont('BodyBold', 8)
        canvas.setFillColor(MUTED)
        canvas.drawString(48, A4[1]-22, 'SOFTWARE ENGINEERING / PROJEKTSTUDIE')
        canvas.setFont('Body', 8)
        canvas.drawString(48, 27, 'Saburollah Safari  /  Study Organizer')
        canvas.drawRightString(A4[0]-48, 27, f'{doc.page:02} / {PAGE_COUNT:02}')
        canvas.restoreState()

    rl_config.invariant = 1
    doc = SimpleDocTemplate(str(OUT / PDF_NAME), pagesize=A4, leftMargin=48,
                            rightMargin=48, topMargin=48, bottomMargin=48,
                            title='Von der Feature-Idee zur verlässlichen Software',
                            author='Saburollah Safari',
                            subject='Fallstudie: Matt Pocock Skills und Obras Superpowers')
    doc.build(story, onFirstPage=frame, onLaterPages=frame)


def verify_sources_and_paper(manifest):
    content = PAPER.read_text()
    # Validate the article and its detailed appendix, including both figures.
    for document in [PAPER, BASE / 'PAPER-ANHANG.md', BASE / 'PAPER-NACHWEISE.md']:
        for target in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)', document.read_text()):
            if '://' not in target:
                assert (document.parent / target.split('#')[0]).is_file(), target
    for entry in manifest['sources']:
        assert sha((ROOT / entry['export']).read_bytes()) == entry['sha256']
    sources = [BASE / 'references/matt/Docs/skill-evaluation/matt-observations.md',
               BASE / 'references/superpowers/Docs/skill-evaluation/superpowers-observations.md']
    values = []
    for file in sources:
        source = file.read_text()
        points = []
        for criterion in CRITERIA:
            match = re.search(r'^\| '+re.escape(criterion)+r'\s*\|\s*([1-5])\s*\|', source, re.M)
            assert match, criterion
            points.append(int(match[1]))
        values.append(points)
    assert values == [[4,4,5,3,4,4,4], [5,4,4,5,5,4,3]]
    current = confirmed_ratings()
    # Four criteria were explicitly confirmed without change by the user.
    for i in [0, 1, 2, 5]:
        assert [v[i] for v in current] == [v[i] for v in values]
    for i, criterion in enumerate(CRITERIA):
        row = next(line for line in content.splitlines() if line.startswith('| '+criterion+' |'))
        assert [int(n) for n in re.findall(r'\*\*([1-5])\*\*', row)] == [current[0][i], current[1][i]]
    numbered_content = content.split('## Nachweise')[0]
    blocks = re.split(r'^## (\d\. .+)$', numbered_content, flags=re.M)
    counts = {blocks[i]: len(blocks[i+1].split()) for i in range(1, len(blocks), 2)}
    assert max(counts, key=counts.get).startswith('4.'), counts
    assert content.index('## 4. Fazit') < content.index('## 5. Was ich persönlich')
    assert len(re.findall(r'^!\[', content, re.M)) == 4
    assert '## 3. Was die beiden Arbeitsweisen leisten' in content
    assert '### 3.1 Beobachtete Arbeitsweisen' in content
    assert '### 3.2 Persönliche Bewertung mit konkreten Gründen' in content
    assert '(figures/02-workflowvergleich.png)' in content
    appendix = (BASE / 'PAPER-ANHANG.md').read_text()
    assert set(re.findall(r'\| (FR-\d+)', content)) == {f'FR-{i:02}' for i in range(1,8)}
    assert set(re.findall(r'\| (NFR-\d+)', content)) == {f'NFR-{i:02}' for i in range(1,8)}
    assert set(re.findall(r'\| (FR-\d+)', appendix)) == {f'FR-{i:02}' for i in range(1,8)}
    assert set(re.findall(r'\| (NFR-\d+)', appendix)) == {f'NFR-{i:02}' for i in range(1,8)}
    assert not re.search(r'\b\d{1,2}\.\s*(August|September)|\b\d{2}\.\d{2}\.\d{4}', content)
    for suite, points in zip(['Matt', 'Superpowers'], current):
        assert suite + ' ' + '/'.join(map(str, points)) in appendix
    return {'original_ratings': values, 'confirmed_ratings': current,
            'confirmed_means': [round(sum(v)/7,2) for v in current],
            'section_word_counts': counts, 'fr_count': 7, 'nfr_count': 7,
            'figure_count': 4, 'plot_ratings': current,
            'local_paper_links_valid': True, 'source_hashes_valid': True}


def render_qa(report):
    reader = PdfReader(OUT / PDF_NAME)
    report['pdf_pages'] = len(reader.pages)
    assert len(reader.pages) == PAGE_COUNT, f'The article is designed for {PAGE_COUNT} pages'
    link_count = 0
    for page in reader.pages:
        for annotation in page.get('/Annots', []):
            target = annotation.get_object().get('/A', {}).get('/URI')
            if target and '://' not in target:
                assert (ROOT / target).is_file(), target
                link_count += 1
    report['verified_local_pdf_links'] = link_count
    text_pages = [p.extract_text() for p in reader.pages]
    report['pdf_text_characters'] = [len(t) for t in text_pages]
    assert all(len(t) > 250 for t in text_pages), 'Possibly empty/orphan page'
    joined = '\n'.join(text_pages)
    expected_ids = [f'{prefix}-{i:02}' for prefix in ['FR', 'NFR'] for i in range(1,8)]
    for key in expected_ids + ['Abbildung 3.', 'Abbildung 4.', 'Offene Fragen klären',
                               'Gesamtweg abnehmen', '4.5 Reichweite', '5. Was ich persönlich', 'Nachweise']:
        assert key in joined, key
    setup_page = text_pages[2]
    for key in ['Gemeinsamer Start: e7d8b5e', 'Skill f6de92c', 'Skill a419016',
                'Nachweisstand: ab8249c', 'Nachweisstand: a8801ff']:
        assert key in setup_page, key
    evaluation_page = text_pages[3]
    assert evaluation_page.index('3.2 Persönliche Bewertung mit konkreten Gründen') < evaluation_page.index('PERSÖNLICHES BEWERTUNGSPROFIL')
    assert evaluation_page.index('Anpassbarkeit') < evaluation_page.index('PERSÖNLICHES BEWERTUNGSPROFIL')
    assert 'Abbildung 4.' in evaluation_page
    pdf = pdfium.PdfDocument(str(OUT / PDF_NAME))
    thumbs = []
    for n in range(len(pdf)):
        page = pdf[n]
        img = page.render(scale=1.6).to_pil().convert('RGB')
        img.save(QA / f'page-{n+1:02}.png')
        thumb = ImageOps.contain(img, (330, 467))
        tile = Image.new('RGB', (350, 500), '#dbe3e8')
        tile.paste(thumb, ((350-thumb.width)//2, 8))
        ImageDraw.Draw(tile).text((15, 479), f'Page {n+1}', fill='#162c40')
        thumbs.append(tile)
    pdf.close()
    for offset in range(0, len(thumbs), 6):
        group = thumbs[offset:offset+6]
        sheet = Image.new('RGB', (1050, 500*((len(group)+2)//3)), 'white')
        for i, tile in enumerate(group): sheet.paste(tile, ((i%3)*350, (i//3)*500))
        sheet.save(QA / f'contact-{offset//6+1}.png')
    (QA / 'qa-report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')


def package_links(content, document, manifest, overrides=None):
    """Adapt links in delivery copies; keep repository sources untouched."""
    local = {
        (BASE / 'PAPER-ANHANG.md').resolve(): 'Anhang.md',
        (BASE / 'experiment-protocol.md').resolve(): 'experiment-protocol.md',
        (BASE / 'PAPER-NACHWEISE.md').resolve(): 'Nachweise.md',
        (BASE / 'PAPER-README.md').resolve(): 'Nachweise.md',
        PAPER.resolve(): PDF_NAME,
    }
    originals = {
        (ROOT / e['export']).resolve():
        f"https://github.com/Saburollah/study-organizer/blob/{e['commit']}/{e['original']}"
        for e in manifest['sources']
    }
    def replace(match):
        label, target = match.groups()
        if '://' in target or target.startswith('#'):
            return match[0]
        filename, separator, fragment = target.partition('#')
        source = (document.parent / filename).resolve()
        if source in (overrides or {}):
            destination = overrides[source]
        else:
            destination = local.get(source) or originals.get(source)
            assert destination, f'Unmapped package link: {target}'
            if separator:
                destination += '#' + fragment
        return f'[{label}]({destination})'
    return re.sub(r'\[([^\]]+)\]\(([^)]+)\)', replace, content)


def package_pdf():
    """Change only the two local links, with pixel equality as a guard."""
    original = (OUT / PDF_NAME).read_bytes()
    reader = PdfReader(io.BytesIO(original))
    writer = PdfWriter()
    writer.clone_document_from_reader(reader)
    links = {
        'Docs/skill-evaluation/experiment-protocol.md': 'experiment-protocol.md',
        'Docs/skill-evaluation/PAPER-ANHANG.md': 'Anhang.md',
    }
    changed = 0
    for page in writer.pages:
        for annotation in page.get('/Annots', []):
            action = annotation.get_object().get('/A')
            if action and action.get('/URI') in links:
                action[NameObject('/URI')] = TextStringObject(links[str(action['/URI'])])
                changed += 1
    assert changed == 2, f'Unexpected number of PDF links: {changed}'
    stream = io.BytesIO()
    writer.write(stream)
    result = stream.getvalue()
    updated = PdfReader(io.BytesIO(result))
    assert len(updated.pages) == len(reader.pages) == PAGE_COUNT
    assert [p.extract_text() for p in updated.pages] == [p.extract_text() for p in reader.pages]
    before, after = pdfium.PdfDocument(original), pdfium.PdfDocument(result)
    for i in range(len(before)):
        left = before[i].render(scale=1.5).to_pil().convert('RGB')
        right = after[i].render(scale=1.5).to_pil().convert('RGB')
        assert left.size == right.size and left.tobytes() == right.tobytes(), f'PDF layout changed on page {i+1}'
    before.close()
    after.close()
    return result


def bundle(manifest):
    # A deliberate allowlist prevents build files and complete logs leaking back in.
    q_sources = {
        'references/matt/Docs/skill-evaluation/matt-observations.md': 'q2',
        'references/matt/CONTEXT.md': 'q3',
        'references/matt/Docs/skill-evaluation/experiment-protocol.md': 'q3',
        'references/superpowers/Docs/skill-evaluation/superpowers-observations.md': 'q4',
        'references/superpowers/Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md': 'q5',
        'references/superpowers/Docs/skill-evaluation/agent-activity-log.md': 'q6',
        'references/comparison/Docs/skill-evaluation/matt-vs-superpowers-comparison.md': 'q7',
    }
    q_links = {(BASE / p).resolve(): 'Nachweise.md#' + anchor for p, anchor in q_sources.items()}
    files = {PDF_NAME: package_pdf()}
    for source, destination in [('PAPER-ANHANG.md', 'Anhang.md'),
                                 ('experiment-protocol.md', 'experiment-protocol.md'),
                                 ('PAPER-NACHWEISE.md', 'Nachweise.md')]:
        document = BASE / source
        files[destination] = package_links(document.read_text(), document, manifest,
                                           q_links if source == 'PAPER-ANHANG.md' else None).encode()
    assert set(files) == {PDF_NAME, 'Anhang.md', 'experiment-protocol.md', 'Nachweise.md'}
    for name, data in files.items():
        if not name.endswith('.md'):
            continue
        for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)', data.decode()):
            if '://' in target:
                continue
            filename, separator, fragment = target.partition('#')
            assert filename in files, f'Broken package link: {name} -> {target}'
            if separator:
                assert f'## {fragment.upper()}\n' in files[filename].decode(), target
    for page in PdfReader(io.BytesIO(files[PDF_NAME])).pages:
        for annotation in page.get('/Annots', []):
            target = annotation.get_object().get('/A', {}).get('/URI')
            if target and '://' not in target:
                assert target in files, target
    # Check all fourteen current values against the separate confirmation record.
    evidence = files['Nachweise.md'].decode()
    current = confirmed_ratings()
    for i, criterion in enumerate(CRITERIA):
        row = next(line for line in evidence.splitlines() if line.startswith('| '+criterion+' |'))
        assert [int(n) for n in re.findall(r'\| ([1-5]) ', row)] == [current[0][i], current[1][i]]
    target = OUT / 'agent-skills-fallstudie-quellenpaket.zip'
    with zipfile.ZipFile(target, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(files.items()):
            info = zipfile.ZipInfo(name, date_time=(2026,8,31,12,0,0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    with zipfile.ZipFile(target) as archive:
        assert archive.testzip() is None
        assert set(archive.namelist()) == set(files)
        for name, data in files.items(): assert archive.read(name) == data
    return len(files)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    if sys.argv[1:] == ['--package-only']:
        manifest = json.loads((BASE / 'references/source-manifest.json').read_text())
        for entry in manifest['sources']:
            assert sha((ROOT / entry['export']).read_bytes()) == entry['sha256']
        print(json.dumps({'bundle_file_count': bundle(manifest), 'pdf_layout_unchanged': True,
                          'local_links_valid': True, 'confirmed_ratings_valid': True}, indent=2))
        return
    QA.mkdir(parents=True, exist_ok=True)
    fonts()
    manifest = source_snapshots()
    figures = save_figures()
    report = verify_sources_and_paper(manifest)
    build_pdf(figures)
    render_qa(report)
    report['bundle_file_count'] = bundle(manifest)
    (QA / 'qa-report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
    print(json.dumps(report, ensure_ascii=False, indent=2))
    print(OUT / PDF_NAME)
    print(OUT / 'agent-skills-fallstudie-quellenpaket.zip')


if __name__ == '__main__':
    main()
