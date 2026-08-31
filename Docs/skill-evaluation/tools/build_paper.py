#!/usr/bin/env python3
"""Build the case-study PDF, vector figures and a self-contained evidence bundle.

No network calls, application tests or product changes. Historical inputs are
exported by immutable git revision, or verified from the supplied manifest.
"""
from __future__ import annotations

import hashlib
import html
import json
import os
from pathlib import Path
import re
import subprocess
import zipfile

from PIL import Image, ImageOps, ImageDraw
import pypdfium2 as pdfium
from pypdf import PdfReader
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
INK = colors.HexColor('#162C40')
TEAL = colors.HexColor('#087F82')
MATT = colors.HexColor('#B15C39')
MUTED = colors.HexColor('#526579')
PALE = colors.HexColor('#F1F5F7')
LINE = colors.HexColor('#D5DFE5')
CRITERIA = ['Verständlichkeit', 'Kontrolle', 'Lerngewinn', 'Angemessener Aufwand',
            'Vertrauen', 'Wiederaufnahme', 'Anpassbarkeit']


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


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
    d = Drawing(940, 455)
    d.add(Rect(0, 0, 940, 455, fillColor=PALE, strokeColor=None, rx=14, ry=14))
    text(d, 24, 416, 'EIN KURS. EIN GEMEINSAMER ABRUF. DREI PERSÖNLICHE AUFGABEN.', 21, bold=True)
    box(d, 24, 249, 220, 133, 'Mock-Quelle', ['PDF / Link / Aktivität', 'kontrollierte Zustände'])
    box(d, 284, 249, 278, 133, 'Adapter + Validierung', ['stabile Kurs- und Inhalts-ID', 'vollständiger Zustand'])
    box(d, 602, 249, 312, 133, 'Gemeinsamer Scan', ['mit altem Zustand vergleichen', 'atomar übernehmen'])
    arrow(d, [(244, 313), (284, 313)])
    arrow(d, [(562, 313), (602, 313)])
    d.add(Line(758, 249, 758, 208, strokeColor=TEAL, strokeWidth=2.4))
    d.add(Line(171, 208, 770, 208, strokeColor=TEAL, strokeWidth=2.4))
    arrow(d, [(171, 208), (171, 167)])
    arrow(d, [(470, 208), (470, 167)])
    arrow(d, [(770, 208), (770, 167)])
    text(d, 24, 225, 'NUR FÜR BERECHTIGTE ABONNEMENTS', 16, color=TEAL, bold=True)
    for x, label in [(24, 'A'), (324, 'B'), (624, 'C')]:
        box(d, x, 40, 290, 125, f'Benutzer {label}', ['eigenes Modul', 'genau eine persönliche Aufgabe'])
    text(d, 24, 16, 'Bei einem aufgabenfähigen Inhalt; erneuter identischer Scan erzeugt keine Duplikate.', 16, MUTED)
    return d


def experiment():
    d = Drawing(940, 560)
    d.add(Rect(0, 0, 940, 560, fillColor=PALE, strokeColor=None, rx=14, ry=14))
    text(d, 24, 525, 'GLEICHE CODEBASIS, GETRENNTE VERSUCHE', 23, bold=True)
    box(d, 264, 414, 412, 84, 'Gemeinsamer Start: e7d8b5e', ['Mock-Quelle + Versuchsprotokoll'])
    arrow(d, [(470, 414), (470, 392), (236, 392), (236, 365)], MATT)
    arrow(d, [(470, 392), (704, 392), (704, 365)], TEAL)
    box(d, 24, 138, 422, 225, '01  Matt Pocock Skills', [], MATT)
    box(d, 494, 138, 422, 225, '02  Superpowers', [], TEAL)
    left = ['matt-v1.0  |  Skill f6de92c', 'Wayfinder > Grilling > ADRs', 'TDD + Standards-/Spec-Review',
            'Asynchroner Scan, Historie, Cleanup', 'Nachweisstand: ab8249c']
    right = ['superpowers-v6.3.0  |  Skill a419016', 'Brainstorming > Plan > TDD', 'Task-Reviews + lokaler Sichttest',
             'Synchroner, enger begrenzter Schnitt', 'Nachweisstand: a8801ff']
    for i, label in enumerate(left): text(d, 42, 294-i*29, label, 19, MUTED)
    for i, label in enumerate(right): text(d, 512, 294-i*29, label, 19, MUTED)
    arrow(d, [(235, 138), (235, 105), (470, 105), (470, 78)], MATT)
    arrow(d, [(705, 138), (705, 105), (470, 105)], TEAL)
    text(d, 470, 51, 'Vergleich von Abläufen und Nachweisen - kein Produktmerge', 21, bold=True, anchor='middle')
    text(d, 470, 20, 'Nacheinander durchgeführt; Umfang, Umgebung und Vorwissen sind nicht identisch.', 17, MUTED, anchor='middle')
    return d


def save_figures():
    folder = BASE / 'figures'
    folder.mkdir(exist_ok=True)
    figures = {'01-gemeinsamer-scan': architecture(), '02-versuchsaufbau': experiment()}
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
        'body': ParagraphStyle('body', fontName='Body', fontSize=10.5, leading=15.1,
                               textColor=INK, spaceAfter=8, splitLongWords=True),
        'title': ParagraphStyle('title', fontName='BodyBold', fontSize=25, leading=30,
                                textColor=INK, spaceAfter=13),
        'subtitle': ParagraphStyle('subtitle', fontName='Body', fontSize=13, leading=18,
                                   textColor=TEAL, spaceAfter=14),
        'h2': ParagraphStyle('h2', fontName='BodyBold', fontSize=17, leading=22,
                             textColor=INK, spaceBefore=17, spaceAfter=10, keepWithNext=True),
        'h3': ParagraphStyle('h3', fontName='BodyBold', fontSize=12, leading=16,
                             textColor=TEAL, spaceBefore=11, spaceAfter=7, keepWithNext=True),
        'cell': ParagraphStyle('cell', fontName='Body', fontSize=9, leading=12.2, textColor=INK),
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
        if line.startswith('# '):
            story.append(Paragraph(inline(line[2:]), styles['title']))
        elif line.startswith('## Eine Fallstudie'):
            story.append(Paragraph(inline(line[3:]), styles['subtitle']))
        elif line.startswith('## '):
            heading = line[3:]
            if heading.startswith('5.'):
                story.append(PageBreak())
            story.append(Paragraph(inline(heading), styles['h2']))
        elif line.startswith('### '):
            story.append(Paragraph(inline(line[4:]), styles['h3']))
        elif line.startswith('!['):
            match = re.match(r'!\[.*\]\(([^)]+)\)', line)
            drawing = figures[Path(match[1]).stem]
            scale = width / drawing.width
            scaled = Drawing(width, drawing.height * scale)
            scaled.add(drawing)
            scaled.scale(scale, scale)
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
                widths = [42, 208, width-250]
            elif count == 3:
                widths = [116, (width-116)/2, (width-116)/2]
            else:
                widths = [92, 138, 138, width-368]
            rows = [[Paragraph(inline(c), styles['head' if r == 0 else 'cell'])
                     for c in row] for r, row in enumerate(raw)]
            table = Table(rows, colWidths=widths, repeatRows=1, hAlign='LEFT')
            commands = [('BACKGROUND', (0,0), (-1,0), INK),
                        ('VALIGN', (0,0), (-1,-1), 'TOP'),
                        ('LEFTPADDING', (0,0), (-1,-1), 8),
                        ('RIGHTPADDING', (0,0), (-1,-1), 8),
                        ('TOPPADDING', (0,0), (-1,-1), 8),
                        ('BOTTOMPADDING', (0,0), (-1,-1), 8),
                        ('LINEBELOW', (0,0), (-1,0), 1, TEAL)]
            for row in range(1, len(rows)):
                commands += [('BACKGROUND', (0,row), (-1,row), PALE if row % 2 else colors.white),
                             ('LINEBELOW', (0,row), (-1,row), .35, LINE)]
            table.setStyle(TableStyle(commands))
            story += [Spacer(1, 5), table, Spacer(1, 12)]
            continue
        else:
            paragraph = [line]
            while i+1 < len(lines) and lines[i+1].strip() and not re.match(r'^(#|\||!\[|- |\d+\. )', lines[i+1]):
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
        canvas.drawString(48, A4[1]-22, 'STUDY ORGANIZER / AGENTISCHE SOFTWAREENTWICKLUNG')
        canvas.setFont('Body', 8)
        canvas.drawString(48, 27, 'Saburollah Safari  |  Fallstudie  |  31.08.2026')
        canvas.drawRightString(A4[0]-48, 27, str(doc.page))
        canvas.restoreState()

    rl_config.invariant = 1
    doc = SimpleDocTemplate(str(OUT / PDF_NAME), pagesize=A4, leftMargin=48,
                            rightMargin=48, topMargin=48, bottomMargin=48,
                            title='Agentische Skill-Suites in der Softwareentwicklung',
                            author='Saburollah Safari',
                            subject='Fallstudie: Matt Pocock Skills und Obras Superpowers')
    doc.build(story, onFirstPage=frame, onLaterPages=frame)


def verify_sources_and_paper(manifest):
    content = PAPER.read_text()
    # Validate local Markdown links including both figures.
    for target in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)', content):
        if '://' not in target:
            assert (BASE / target.split('#')[0]).is_file(), target
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
    for i, criterion in enumerate(CRITERIA):
        row = next(line for line in content.splitlines() if line.startswith('| '+criterion+' |'))
        assert [int(n) for n in re.findall(r'\*\*([1-5])\*\*', row)] == [values[0][i], values[1][i]]
    numbered_content = content.split('## Quellen und mitgelieferte Nachweise')[0]
    blocks = re.split(r'^## (\d\. .+)$', numbered_content, flags=re.M)
    counts = {blocks[i]: len(blocks[i+1].split()) for i in range(1, len(blocks), 2)}
    assert max(counts, key=counts.get).startswith('5.'), counts
    assert content.index('## 5. Fazit') < content.index('## 6. Persönliche')
    assert len(re.findall(r'^!\[', content, re.M)) == 2
    assert set(re.findall(r'\| (FR-\d+)', content)) == {f'FR-{i:02}' for i in range(1,8)}
    assert set(re.findall(r'\| (NFR-\d+)', content)) == {f'NFR-{i:02}' for i in range(1,8)}
    return {'original_ratings': values, 'means': [round(sum(v)/7,2) for v in values],
            'section_word_counts': counts, 'fr_count': 7, 'nfr_count': 7,
            'figure_count': 2, 'local_paper_links_valid': True, 'source_hashes_valid': True}


def render_qa(report):
    reader = PdfReader(OUT / PDF_NAME)
    report['pdf_pages'] = len(reader.pages)
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
    for key in ['FR-07', 'NFR-07', '4,29', '5.6 Grenzen', '6. Persönliche', 'Q7:']:
        assert key in joined, key
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


def bundle(manifest):
    paths = [PAPER, BASE / 'PAPER-README.md', BASE / 'references/source-manifest.json', Path(__file__)]
    paths += [ROOT / e['export'] for e in manifest['sources']]
    paths += sorted((BASE / 'figures').glob('*.svg')) + sorted((BASE / 'figures').glob('*.png'))
    files = {str(p.relative_to(ROOT)): p.read_bytes() for p in paths}
    files[PDF_NAME] = (OUT / PDF_NAME).read_bytes()
    files['README.md'] = (BASE / 'PAPER-README.md').read_bytes()
    index = {name: sha(data) for name, data in sorted(files.items())}
    files['package-manifest.json'] = (json.dumps(index, ensure_ascii=False, indent=2)+'\n').encode()
    target = OUT / 'agent-skills-fallstudie-quellenpaket.zip'
    with zipfile.ZipFile(target, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(files.items()):
            info = zipfile.ZipInfo(name, date_time=(2026,8,31,12,0,0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    with zipfile.ZipFile(target) as archive:
        assert archive.testzip() is None
        for name, digest in index.items(): assert sha(archive.read(name)) == digest
        assert 'Docs/skill-evaluation/experiment-protocol.md' in archive.namelist()
    return len(files)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
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
