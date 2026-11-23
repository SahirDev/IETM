"""Engine IPC PDF (72-00-00 Fig 1), laid out like the landing gear IPC: title page, five illustration sheets
(assembly, exploded view, details A-C; rendered from engine.glb by engine_figs.py) and the parts list.
Run: python Tools/ipc_pdf/engine/build_engine_pdf.py   ->  Assets/StreamingAssets/IETM/72-00-00_Fig1_IPC.pdf"""
import json, os, sys, subprocess, tempfile
from reportlab.lib.pagesizes import letter, landscape
from reportlab.pdfgen import canvas
from reportlab.lib.colors import black, HexColor
from reportlab.lib.utils import ImageReader
from PIL import Image
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
D = json.load(open(os.path.join(REPO, 'Assets/IETM/Resources/IETM/engine_ietm.json'), encoding='utf-8'))
pub, fig, items = D['publication'], D['figure'], D['items']
OUT = os.path.join(REPO, 'Assets/StreamingAssets/IETM/72-00-00_Fig1_IPC.pdf')
WORK = os.path.join(tempfile.gettempdir(), 'engine_ipc')
subprocess.check_call([sys.executable, os.path.join(HERE, 'engine_figs.py'), WORK])
FIGS = json.load(open(os.path.join(WORK, 'figs.json')))

CH, FIGNO, DATE, SHORT = '72-00-00', '1', 'Nov 23/26', 'F-16 ENGINE'
M = 36
c = canvas.Canvas(OUT, pagesize=letter)
c.setTitle('F-16 Engine - Turbine Module - Illustrated Parts Catalog (72-00-00 Fig 1)')
c.setAuthor('IETM POC'); c.setSubject(pub['pmc'])
page = [0]

def frame(title_right='ILLUSTRATED PARTS CATALOG'):
    W, H = c._pagesize
    page[0] += 1
    c.setFont('Helvetica-Bold', 9); c.drawString(M, H - M + 4, SHORT)
    c.drawRightString(W - M, H - M + 4, title_right)
    c.setLineWidth(0.8); c.line(M, H - M, W - M, H - M)
    y = M + 6; c.line(M, y + 26, W - M, y + 26)
    c.setFont('Helvetica', 7); c.drawString(M, y + 16, 'EFFECTIVITY')
    c.setFont('Helvetica-Bold', 8); c.drawString(M, y + 6, 'ALL')
    c.setFont('Helvetica-Bold', 11); c.drawRightString(W - M, y + 14, CH)
    c.setFont('Helvetica', 8); c.drawRightString(W - M, y + 3, f'Page {page[0]}')
    c.drawRightString(W - M, y - 7, DATE)
    c.setFont('Helvetica', 6.5); c.setFillColor(HexColor('#555555'))
    c.drawCentredString(W / 2, y + 8, pub['pmc'] + '   ·   ' + fig['icn'])
    c.drawCentredString(W / 2, y - 2, 'SAMPLE DATA – POC ONLY. Part numbers and CAGE codes are placeholders.')
    c.setFillColor(black)
    return W, H

def sheet_image(f):
    """Crops a sheet to its drawing + labels; returns (image path, labels in the cropped pixels, size)."""
    im = Image.open(f['image']).convert('L'); a = np.array(im) < 250
    ys, xs = np.nonzero(a)
    lx = [L['lx'] for L in f['labels']]; ly = [L['ly'] for L in f['labels']]
    x0 = int(max(0, min([xs.min()] + lx) - 40)); x1 = int(min(im.width, max([xs.max()] + lx) + 40))
    y0 = int(max(0, min([ys.min()] + ly) - 25)); y1 = int(min(im.height, max([ys.max()] + ly) + 25))
    crop = f['image'].replace('.png', '_c.png'); im.crop((x0, y0, x1, y1)).save(crop)
    labels = [dict(L, ax=L['ax'] - x0, lx=L['lx'] - x0, ay=L['ay'] - y0, ly=L['ly'] - y0) for L in f['labels']]
    return crop, labels, (x1 - x0, y1 - y0)

def draw_sheet(f, x, y, w, h, labels=True):
    img, L_, (iw, ih) = sheet_image(f)
    s = min(w / iw, h / ih); ox = x + (w - iw * s) / 2; oy = y + (h - ih * s) / 2
    c.drawImage(ImageReader(img), ox, oy, iw * s, ih * s)
    if not labels: return
    P = lambda px, py: (ox + px * s, oy + (ih - py) * s)
    c.setLineWidth(0.5)
    for L in L_:
        ax, ay = P(L['ax'], L['ay']); lx, ly = P(L['lx'], L['ly'])
        c.setFont('Helvetica-Bold', 9); tw = c.stringWidth(L['item'], 'Helvetica-Bold', 9)
        vx, vy = ax - lx, ay - ly; dl = max(1e-6, (vx * vx + vy * vy) ** 0.5)
        ex = lx + vx / dl * (tw / 2 + 2); ey = ly + vy / dl * 6
        c.line(ax, ay, ex, ey); c.circle(ax, ay, 1.2, fill=1)
        c.drawCentredString(lx, ly - 3, L['item'])

# ---- title page
W, H = frame('TITLE PAGE')
c.setFont('Helvetica-Bold', 24); c.drawCentredString(W / 2, H - 190, 'F-16 ENGINE - TURBINE MODULE')
c.setFont('Helvetica', 16); c.drawCentredString(W / 2, H - 218, 'ILLUSTRATED PARTS CATALOG')
c.setFont('Helvetica', 11); c.drawCentredString(W / 2, H - 246, f'Chapter {CH}  ·  Figure {FIGNO}')
draw_sheet(FIGS[0][1], M + 10, H - 560, W - 2 * M - 20, 290, labels=False)
rows = [('Publication code', pub['pmc']), ('Issue', f"{pub['issue']}  ({pub['issueDate']})"),
        ('Applicability', pub['applicability']), ('Figure ICN', fig['icn'])]
y = 200
for k, v in rows:
    c.setFont('Helvetica-Bold', 9); c.drawString(110, y, k); c.setFont('Helvetica', 9); c.drawString(230, y, v); y -= 15
c.setFillColor(HexColor('#B45309')); c.setFont('Helvetica-Bold', 8.5)
c.drawCentredString(W / 2, 110, pub['dataStatus'][:110]); c.setFillColor(black)
c.showPage()

# ---- illustration sheets (landscape: the engine is long)
c.setPageSize(landscape(letter))
NS = len(FIGS)
for i, (name, f) in enumerate(FIGS):
    W, H = frame()
    draw_sheet(f, M, M + 70, W - 2 * M, H - 2 * M - 84)
    c.setFont('Helvetica-Bold', 10); c.drawCentredString(W / 2, M + 52, fig['title'].upper())
    c.setFont('Helvetica', 9); c.drawCentredString(W / 2, M + 40, f'Figure {FIGNO} (Sheet {i + 1} of {NS})  -  {name.upper()}')
    c.showPage()

# ---- parts list
c.setPageSize(letter)
cols = [('FIG.\nITEM', M, 52), ('PART NUMBER', M + 52, 104), ('1234567\nNOMENCLATURE', M + 156, 236), ('CAGE', M + 392, 46),
        ('SMR', M + 438, 42), ('EFFECT\nCODE', M + 480, 32), ('UNITS\nPER ASSY', M + 512, 28)]
def header(y):
    c.setLineWidth(0.8); c.line(M, y + 2, W - M, y + 2)
    for t, x, w in cols:
        c.setFont('Helvetica-Bold', 7)
        for k, l in enumerate(t.split('\n')): c.drawString(x + 2, y - 8 - k * 8, l)
    c.line(M, y - 22, W - M, y - 22); return y - 34
def wrap(text, font, size, width):
    out, cur = [], ''
    for w_ in text.split():
        t = (cur + ' ' + w_).strip()
        if c.stringWidth(t, font, size) <= width: cur = t
        else: out.append(cur); cur = w_
    out.append(cur); return out
W, H = frame(); y = header(H - M - 14)
for it in items:
    dots = '.' * (it['indent'] - 1)
    lines = wrap(dots + it['description'], 'Courier', 8, cols[2][2] - 4)
    info = it.get('info', '')
    extra = wrap('(' + info + ')', 'Helvetica-Oblique', 6.5, cols[2][2] - 4 - 6 * len(dots)) if info else []
    need = 11 * len(lines) + 8 * len(extra) + 4
    if y - need < M + 50:
        c.showPage(); W, H = frame(); y = header(H - M - 14)
    illustrated = bool(it['nodes'])
    no = it['itemNo']
    c.setFont('Courier', 8)
    c.drawString(cols[0][1] + 2, y, ('' if illustrated or no == '001' else '-') + f'{FIGNO}-{no}')
    c.drawString(cols[1][1] + 2, y, it['partNumber'])
    for k, l in enumerate(lines): c.drawString(cols[2][1] + 2, y - k * 11, l)
    c.drawString(cols[3][1] + 2, y, it['cage']); c.drawString(cols[4][1] + 2, y, it['smr'])
    c.drawRightString(cols[6][1] + cols[6][2], y, str(it['upa']))
    yy = y - 11 * len(lines) + 3
    c.setFont('Helvetica-Oblique', 6.5); c.setFillColor(HexColor('#444444'))
    for l in extra: c.drawString(cols[2][1] + 2 + c.stringWidth(dots, 'Courier', 8), yy, l); yy -= 8
    if it.get('remarks'):
        c.setFillColor(HexColor('#7A4B00'))
        for l in wrap('NOTE: ' + it['remarks'], 'Helvetica-Oblique', 6.5, cols[2][2] - 4 - 6 * len(dots)):
            if yy < M + 50: break
            c.drawString(cols[2][1] + 2 + c.stringWidth(dots, 'Courier', 8), yy, l); yy -= 8
    c.setFillColor(black)
    y = yy - 6
c.setFont('Helvetica', 7)
c.drawString(M, y - 6, '- ITEM NOT ILLUSTRATED   ·   Indenture: 1 = assembly, each leading dot = one level below its next higher assembly (ATA iSpec 2200).')
c.showPage(); c.save()
print('wrote', OUT, 'pages', page[0])
