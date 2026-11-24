"""Sensor & Health: the Sensors and Health pages of HELIOS UI.prefab become one page (UI only, Figma 1021:12072).
Menu: 3D View, IETM, Sensor & Health, Asset Integrity, Maintenance, Settings (the Health item is switched off; its
page stays in the prefab, unused). The Sensors page (page id "Sensors", so buttons, sensors and wireframe keep working)
gets, on the right: Sensor Readings (a card: header + plain rows with icons), System Health (the Health page's
4 rings + Overall Health) and Sensor Trends. The Live bar and the Sensor Health card are switched off.
Run once from the repo root; it refuses to run twice."""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc, col, f

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
d = Doc(P)
if 'Sensor & Health' in d.t:
    sys.exit('already done')

# ---------------------------------------------------------------- index helpers (re-scanned after every raw edit)
def scan():
    global D, name, rt, father, kids, go_of
    hs = list(re.finditer(r'^--- !u!(\d+) &(-?\d+)', d.t, re.M))
    D = {m.group(2): (m.start(), hs[i + 1].start() if i + 1 < len(hs) else len(d.t)) for i, m in enumerate(hs)}
    name, rt, father, kids = {}, {}, {}, {}
    for k_, (s, e) in D.items():
        b = d.t[s:e]
        if b.startswith('--- !u!1 ') and 'm_Name' in b: name[k_] = re.search(r'm_Name: (.*)', b).group(1)
        if b.startswith('--- !u!224') or b.startswith('--- !u!4 '):
            g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b).group(1); rt[g] = k_
            father[k_] = re.search(r'm_Father: \{fileID: (-?\d+)', b).group(1)
            kids[k_] = re.findall(r'  - \{fileID: (-?\d+)\}', b.split('m_Children:')[1].split('m_Father')[0])
    go_of = {v: k_ for k_, v in rt.items()}
def B(k_): s, e = D[k_]; return d.t[s:e]
def put(k_, new):
    s, e = D[k_]; d.t = d.t[:s] + new + d.t[e:]; scan()
def find(path):
    p = path.split('/')
    c = [r for r, fa in father.items() if fa == '0' and name.get(go_of[r]) == p[0]]
    for nm in p[1:]: c = [x for r in c for x in kids.get(r, []) if name.get(go_of[x]) == nm]
    assert len(c) == 1, (path, len(c))
    return c[0]
def comps(go):
    return re.findall(r'component: \{fileID: (-?\d+)\}', B(go))
def comp_of(r, cls):
    for c in comps(go_of[r]):
        if cls in B(c): return c
    return None
def set_children(parent, order):
    b = B(parent)
    head, rest = b.split('  m_Children:\n', 1)
    tail = rest[rest.index('  m_Father:'):]
    lst = '  m_Children: []\n' if not order else '  m_Children:\n' + ''.join('  - {fileID: %s}\n' % c for c in order)
    put(parent, head + lst + tail)
def move(child, new_parent, index=None):
    old = father[child]
    set_children(old, [c for c in kids[old] if c != child])
    order = list(kids[new_parent]); order.insert(len(order) if index is None else index, child)
    set_children(new_parent, order)
    put(child, re.sub(r'm_Father: \{fileID: -?\d+\}', 'm_Father: {fileID: %s}' % new_parent, B(child), count=1))
def active(r, on):
    g = go_of[r]; put(g, re.sub(r'm_IsActive: \d', 'm_IsActive: %d' % (1 if on else 0), B(g), count=1))
def rename(r, nm):
    g = go_of[r]; put(g, re.sub(r'm_Name: .*', 'm_Name: ' + nm, B(g), count=1))
def text(r, s):
    c = comp_of(r, 'TextMeshProUGUI'); assert c, r
    put(c, re.sub(r'\n  m_text: .*', lambda m: '\n  m_text: ' + s, B(c), count=1))
def rect(r, pos=None, size=None):
    b = B(r)
    if pos is not None: b = re.sub(r'm_AnchoredPosition: \{[^}]*\}', 'm_AnchoredPosition: {x: %s, y: %s}' % (f(pos[0]), f(pos[1])), b, count=1)
    if size is not None: b = re.sub(r'm_SizeDelta: \{[^}]*\}', 'm_SizeDelta: {x: %s, y: %s}' % (f(size[0]), f(size[1])), b, count=1)
    put(r, b)
def le_height(r, h):
    c = comp_of(r, 'LayoutElement')
    if c is None: return
    b = B(c)
    b = re.sub(r'm_PreferredHeight: [-\d.]+', 'm_PreferredHeight: %s' % f(h), b, count=1)
    b = re.sub(r'm_MinHeight: [-\d.]+', lambda m: m.group(0) if m.group(0).endswith('-1') else 'm_MinHeight: %s' % f(h), b, count=1)
    put(c, b)
def vlg(r, left=None, right=None, top=None, bottom=None, spacing=None):
    c = comp_of(r, 'VerticalLayoutGroup'); b = B(c)
    for key, v in (('m_Left', left), ('m_Right', right), ('m_Top', top), ('m_Bottom', bottom)):
        if v is not None: b = re.sub(r'(\n    %s: )[-\d.]+' % key, lambda m: m.group(1) + f(v), b, count=1)
    if spacing is not None: b = re.sub(r'(\n  m_Spacing: )[-\d.]+', lambda m: m.group(1) + f(spacing), b, count=1)
    put(c, b)

scan()
UI = 'HELIOS UI/UI'
S = UI + '/Pages/Sensors'
H = UI + '/Pages/Health'

# ---------------------------------------------------------------- phase 1: new objects (Doc writer)
ROUNDED, ROUNDED_LINE = 'bd47ad8e7de76dd458fc070441db5007', '5e10806c54123544cb7cf367a35f1b02'
THEMED = '346a0a1447b7ab8b6ace9c7385c9dfe7'
LEG = '306cc8c2b49d7114eaa3623786fc2126'
C = {3: (0.043137256, 0.07450981, 0.15686275, 0.9), 4: (0.16470589, 0.21960784, 0.3764706, 1), 10: (0.9254902, 0.94509804, 0.98039216, 1),
     20: (0.4392157, 0.47058824, 1, 1)}
ICON = {'pulse': '0b313f14d41c54c48987e059db088d7e', 'vibration': '9c2ba4d9429e3a74fbca7e0af01c08c9', 'layers': '2a41c357c28d77e4b827259b33f70e35',
        'cloud': '9bc52bdf2520f634e9602fcce97013bf', 'fan': '4435564b9cc69f444bba9c676d2f2aa5', 'drop': 'fe1a4d2f8d23b4a4399279f9979d08cb',
        'gauge': 'ad83f900e57104a44877f096d7d8a212', 'hydraulic': 'c72df1fdd7e5c2f4da2003f38db74c6e'}
TMP_TEMPLATE = B(comp_of(find(H + '/Cards/System Health Overview/Header/Title'), 'TextMeshProUGUI'))

def image(o, guid, color, sliced=False, ppu=1.0, preserve=False):
    d.canvas_renderer(o)
    sp = '{fileID: 21300000, guid: %s, type: 3}' % guid if guid else '{fileID: 0}'
    return d.mono(o, 'fe87c0e1cc204ed48ad3b37840f39efc', 'UnityEngine.UI::UnityEngine.UI.Image',
        '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: 0\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: %s\n  m_Type: %d\n'
        '  m_PreserveAspect: %s\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n'
        '  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: %s\n' % (col(color), sp, 1 if sliced else 0, f(preserve), f(float(ppu))))
def themed(o, role): d.mono(o, THEMED, 'Assembly-CSharp::Helios.HeliosThemed', '  role: %d\n  opacity: 1\n  useThemeFont: 1\n' % role)
def ignore_layout(o):
    d.mono(o, LEG, 'UnityEngine.UI::UnityEngine.UI.LayoutElement', '  m_IgnoreLayout: 1\n  m_MinWidth: -1\n  m_MinHeight: -1\n'
           '  m_PreferredWidth: -1\n  m_PreferredHeight: -1\n  m_FlexibleWidth: -1\n  m_FlexibleHeight: -1\n  m_LayoutPriority: 1\n')
def tmp(o, s, size, role, bold=False):
    d.canvas_renderer(o)
    cid = d.nid(); o['comps'].append(cid)
    b = re.sub(r'^--- !u!114 &-?\d+', '--- !u!114 &' + cid, TMP_TEMPLATE)
    b = re.sub(r'm_GameObject: \{fileID: -?\d+\}', 'm_GameObject: {fileID: %s}' % o['go'], b, count=1)
    b = re.sub(r'\n  m_text: .*', lambda m: '\n  m_text: ' + s, b, count=1)
    b = re.sub(r'\n  m_fontSize: [\d.]+', '\n  m_fontSize: %s' % f(float(size)), b, count=1)
    b = re.sub(r'\n  m_fontStyle: \d+', '\n  m_fontStyle: %d' % (1 if bold else 0), b, count=1)
    o['docs'].append(b if b.endswith('\n') else b + '\n')
    themed(o, role)

READ = find(S + '/Cards/Readings')
# card background (ignored by the layout)
for nm, guid, role in (('Fill', ROUNDED, 3), ('Outline', ROUNDED_LINE, 4)):
    o = d.go(nm, READ, anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
    image(o, guid, C[role], sliced=True, ppu=2.0); themed(o, role); ignore_layout(o)
# header (like System Health's)
hdr = d.go('Header', READ, anchor_min=(0, 1), anchor_max=(0, 1), size=(290, 20))
d.layout_element(hdr, h=20, mh=20)
hi = d.go('Icon', hdr['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pos=(8, 0), size=(16, 16))
image(hi, ICON['pulse'], C[20], preserve=True); themed(hi, 20)
ht = d.go('Title', hdr['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0, 0.5), pos=(24, 0), size=(250, 20))
tmp(ht, 'Sensor Readings', 14, 10, bold=True)
NEW_HDR = (hdr['tr'], [o['tr'] for o in d.new if o['name'] in ('Fill', 'Outline')])
# an icon on each reading
ROW_ICON = {'Vibration (RMS)': 'vibration', 'Strain (Wing Root)': 'layers', 'Humidity (Cockpit)': 'cloud', 'Engine RPM': 'fan',
            'Oil Pressure': 'drop', 'Fuel Flow': 'gauge', 'Hydraulic Pressure': 'hydraulic'}
rows = [r for r in kids[READ] if name.get(go_of[r]) in ROW_ICON]
assert len(rows) == 7, len(rows)
for r in rows:
    ic = d.go('Icon', r, anchor_min=(0, 0.5), anchor_max=(0, 0.5), pos=(9, 0), size=(15, 15))
    image(ic, ICON[ROW_ICON[name[go_of[r]]]], C[20], preserve=True); themed(ic, 20)
d.save()
d.new, d.children = [], {}
scan()

# ---------------------------------------------------------------- phase 2: restructure (raw edits)
# menu: Sensors -> "Sensor & Health", Health off, IETM second
items = find(UI + '/Menu/Items')
text(find(UI + '/Menu/Items/Sensors/Label'), 'Sensor & Health')
active(find(UI + '/Menu/Items/Health'), False)
ietm = find(UI + '/Menu/Items/IETM')
order = [c for c in kids[items] if c != ietm]; order.insert(1, ietm); set_children(items, order)
menu = find(UI + '/Menu')
rect(menu, size=(143, 262 - 36))

# Sensors page
active(find(S + '/Live Bar'), False)
READ = find(S + '/Cards/Readings')
rename(READ, 'Sensor Readings')
hdr_tr, bg = NEW_HDR
set_children(READ, bg + [hdr_tr] + [c for c in kids[READ] if c not in bg and c != hdr_tr])
vlg(READ, left=14, right=14, top=12, bottom=10, spacing=0)
rect(READ, size=(318, 258))
for r in [c for c in kids[READ] if name.get(go_of[c]) in ROW_ICON]:
    le_height(r, 30); rect(r, size=(290, 30))
    for k_ in kids[r]:
        nm = name.get(go_of[k_])
        if nm in ('Fill', 'Outline', 'Sparkline'): active(k_, False)
        elif nm == 'Name': rect(k_, pos=(26, 0), size=(170, 30))
        elif nm == 'Value': rect(k_, pos=(0, 0), size=(110, 30))

CARDS = find(S + '/Cards')
SH = find(H + '/Cards/System Health Overview')
move(SH, CARDS, 1)
rename(SH, 'System Health')
text(find(S + '/Cards/System Health/Header/Title'), 'System Health')
for nm in ('Systems 1', 'Systems 2'):
    r = find(S + '/Cards/System Health/' + nm); le_height(r, 60); rect(r, size=(290, 60))
OV = find(H + '/Cards/Health Trend/Overall Health')
move(OV, SH)
le_height(OV, 30); rect(OV, size=(290, 30))
active(find(S + '/Cards/Sensor Health'), False)
d.save()

# check: ids unique, parent / child links agree
scan()
ids = re.findall(r'^--- !u!\d+ &(-?\d+)', d.t, re.M)
assert len(ids) == len(set(ids))
for p, cs in kids.items():
    for c in cs: assert father.get(c) == p, (p, c)
for c, p in father.items():
    if p != '0': assert c in kids.get(p, []), (c, p)
print('Sensor & Health page done')
