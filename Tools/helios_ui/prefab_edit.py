"""Small raw-YAML edit helpers for HELIOS UI.prefab (move / reorder / activate / rename / text / rect / layout).
Use: import prefab_edit as pe; pe.d = Doc(path); pe.scan(); then pe.find(path) ... and pe.d.save()."""
import re
from unity_yaml import f
d = None

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

