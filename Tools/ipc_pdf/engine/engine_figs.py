"""Renders the five engine IPC figure sheets (same line-art renderer and callout placement as the landing gear IPC).
Sheet 1 assembly, sheet 2 exploded view (the IETM explode / disassembly layout), sheets 3-5 details: one section
pulled out of the ghosted engine. Output: <work>/figs.json + sheet PNGs."""
import sys, os, json, numpy as np, cv2
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.dirname(HERE))
from engine_model import load, disp, doff, DATA
from render import render_aa, look

D = json.load(open(DATA, encoding='utf-8'))
items = D['items']
stack, children = [], {}
for it in items:
    while stack and stack[-1]['indent'] >= it['indent']: stack.pop()
    if stack: children.setdefault(stack[-1]['itemNo'], []).append(it['itemNo'])
    stack.append(it)
byno = {it['itemNo']: it for it in items}
def nodes_of(no):
    it = byno[no]
    if it['nodes']: return list(it['nodes'])
    out = []
    for c in children.get(no, []): out += nodes_of(c)
    return out
def callno(no): return no.lstrip('0') or no
leaves = [it['itemNo'] for it in items if not children.get(it['itemNo']) and it['nodes']]
parts = disp(load())
allnames = list(parts)
EXPL = doff({e['node']: e['offset'] for e in D.get('explode', [])})

def _seg_cross(p1, p2, p3, p4):
    def o(a, b, c): return np.sign((b[0]-a[0])*(c[1]-a[1]) - (b[1]-a[1])*(c[0]-a[0]))
    return o(p1, p2, p3) != o(p1, p2, p4) and o(p3, p4, p1) != o(p3, p4, p2)

def place_labels(anchors, drawn, W, H, lab=26):
    lab = max(lab, int(0.019 * max(W, H)))          # clearance scales with the sheet (wide sheets shrink on the page)
    free = cv2.distanceTransform((~drawn).astype(np.uint8), cv2.DIST_L2, 5)
    placed, out = [], []
    ys, xs = np.nonzero(drawn); cx, cy = xs.mean(), ys.mean()
    for no, ax, ay in sorted(anchors, key=lambda a: -np.hypot(a[1]-cx, a[2]-cy)):
        best = None
        for r in range(int(2.4 * lab), int(2.4 * lab) + 900, 20):
            for ang in np.radians(np.arange(0, 360, 10)):
                lx = ax + r*np.cos(ang); ly = ay + r*np.sin(ang)
                if not (lab < lx < W-lab and lab < ly < H-lab): continue
                if free[int(ly), int(lx)] < lab: continue
                cost = r
                for (px, py, qx, qy) in placed:
                    if np.hypot(lx-qx, ly-qy) < 2.2*lab: cost += 5000
                    if _seg_cross((ax, ay), (lx, ly), (px, py), (qx, qy)): cost += 900
                    t = np.clip(((qx-ax)*(lx-ax) + (qy-ay)*(ly-ay)) / max(1, (lx-ax)**2 + (ly-ay)**2), 0, 1)
                    if np.hypot(ax + t*(lx-ax) - qx, ay + t*(ly-ay) - qy) < lab*0.9: cost += 1500
                if np.hypot(lx-cx, ly-cy) < np.hypot(ax-cx, ay-cy): cost += 60
                if best is None or cost < best[0]: best = (cost, lx, ly)
            if best is not None and best[0] < r + 200: break
        if best is None: continue
        _, lx, ly = best; placed.append((ax, ay, lx, ly))
        out.append(dict(item=callno(no), ax=float(ax), ay=float(ay), lx=float(lx), ly=float(ly)))
    return out

def figure(callouts, offsets, view, focus=None, size=1800, out='fig.png', margin=0.03):
    ghost = [] if focus is None else [n for n in allnames if n not in focus]
    img, idimg, names, proj = render_aa(parts, offsets, look(*view), size=(size, size), ghost=ghost, margin=margin)
    H, W = idimg.shape
    ys, xs = np.nonzero(idimg >= 0); pad = int(0.02 * size)                 # crop to the drawing
    x0, x1 = max(0, xs.min()-pad), min(W, xs.max()+pad); y0, y1 = max(0, ys.min()-pad), min(H, ys.max()+pad)
    img = img[y0:y1, x0:x1]; idimg = idimg[y0:y1, x0:x1]; H, W = idimg.shape
    anchors = []
    for no in callouts:
        ids = [names.index(n) for n in nodes_of(no) if n in names and n not in ghost]
        m = np.isin(idimg, ids)
        if m.sum() < 60: continue                                            # hidden inside the engine
        dist = cv2.distanceTransform(m.astype(np.uint8), cv2.DIST_L2, 3)
        yy, xx = np.nonzero(m); c = np.array([xx.mean(), yy.mean()])
        k = np.argmax(dist[yy, xx] - 0.15*np.hypot(xx-c[0], yy-c[1]))
        anchors.append((no, float(xx[k]), float(yy[k])))
    P = int(max(170, 0.09 * max(W, H)))
    canvas = np.full((H + 2*P, W + 2*P), 255, np.uint8); canvas[P:P+H, P:P+W] = img
    anchors = [(n, x+P, y+P) for n, x, y in anchors]
    labels = place_labels(anchors, canvas < 245, W + 2*P, H + 2*P)
    cv2.imwrite(out, canvas)
    return dict(image=os.path.abspath(out), size=(W + 2*P, H + 2*P), labels=labels)

# detail layouts (raw glTF offsets of turbine_01_obj): each section pulled clear of the engine, compact
def raw(d): return doff({nodes_of(k)[0]: v for k, v in d.items()})
DET_A = raw({'110': [-2.6116, 0, 0], '120': [-1.4065, 0, 0], '130': [-2.2, 0, 0]})                     # compressor
DET_B = raw({'020': [3.87658, 0, 0], '230': [3.14095, 0, 0], '210': [0, 0, 3.38082], '220': [0, 0, -2.8388]})  # hot section
DET_C = raw({'050': [0, 0, -2.0], '040': [0, 0, 2.0], '030': [4.4, 0, 0]})                            # case, mounts, lines

ASM_VIEW, EXP_VIEW, DET_VIEW = (-32, 20), (-14, 14), (-28, 18)
if __name__ == '__main__':
    work = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(work, exist_ok=True); os.chdir(work)
    figs = [('Assembly', figure(['010', '020', '030', '040', '050'], {}, ASM_VIEW, out='sheet1.png')),
            ('Exploded view', figure(leaves, EXPL, EXP_VIEW, size=2200, out='sheet2.png')),
            ('Detail A - compressor section', figure(['110', '120', '130'], DET_A, DET_VIEW,
                                                     focus=[nodes_of(k)[0] for k in ('110', '120', '130')], out='sheet3.png')),
            ('Detail B - combustion and turbine', figure(['020', '210', '220', '230'], DET_B, DET_VIEW,
                                                         focus=[nodes_of(k)[0] for k in ('020', '210', '220', '230')], out='sheet4.png')),
            ('Detail C - case, mounts and external lines', figure(['010', '030', '040', '050'], DET_C, DET_VIEW,
                                                                  focus=[nodes_of(k)[0] for k in ('010', '030', '040', '050')], out='sheet5.png'))]
    json.dump([[n, f] for n, f in figs], open('figs.json', 'w'), indent=1, default=float)
    print([(n, f['size'], [l['item'] for l in f['labels']]) for n, f in figs])
