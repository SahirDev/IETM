"""Rebuilds the HELIOS 'Aircraft Hierarchy' panel (3D View page, HELIOS UI.prefab) from the real F-16 model:
part names in Assets/3DModels/F16_V01.glb are F16_<Region>_Region_<Left|Center|Right>_Part_NNNN, so
the tree is Fuselage (Cockpit, Nose / Forward / Mid / Aft Fuselage / Underside), Wing (Left / Right), Empennage (Vertical /
Horizontal Tail), each region split Left / Center / Right, with the real part counts.
Each row is a HeliosTreeNode whose Part Names are the name prefixes it stands for (edit them in the Inspector).
Rows: click = select (parts outlined, camera frames them); chevron = expand / collapse (no hide / show column).
MOVES: parts the model names in another region than where they sit (the radome's left half is named Forward Fuselage,
its right half Nose): the row of their real place lists their name, the other row leaves them out (!name).
Also builds UI > Part Tooltip (hover tip over the 3D model).
Panel layout / style: Figma frame 956:5442 (x 0.8125), as before. Run from the repo root."""
import json, os, re, struct, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc, col, f

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
GLB = 'Assets/3DModels/F16_V01.glb'
K = 0.8125
def k(v): return round(v * K, 2)

# ---------------------------------------------------------------- model part counts by name prefix
d_ = open(GLB, 'rb').read()
j = json.loads(d_[20:20 + struct.unpack('<I', d_[12:16])[0]])
NAMES = [n.get('name', '') for n in j['nodes'] if 'mesh' in n]
MOVES = {'F16_Forward_Fuselage_Region_Left_Part_0331': 'F16_Nose_Region_Left_',           # radome, left half (right half = Nose)
         'F16_Forward_Fuselage_Region_Right_Part_0142': 'F16_Mid_Fuselage_Region_Right_'}   # side skin -10..-4.7 (its mirror = Mid)
assert all(m in NAMES for m in MOVES)
def with_moves(prefixes):
    if not prefixes: return prefixes
    add = [n for n, t in MOVES.items() if any(t.startswith(p) for p in prefixes) and not any(n.startswith(p) for p in prefixes)]
    out = ['!' + n for n, t in MOVES.items() if any(n.startswith(p) for p in prefixes) and not any(t.startswith(p) for p in prefixes)]
    return list(prefixes) + add + out
def matches(n, names):
    if any(x[0] == '!' and n.startswith(x[1:]) for x in names): return False
    return any(x[0] != '!' and n.startswith(x) for x in names)
def count(prefixes):
    names = with_moves(prefixes)
    return sum(1 for n in NAMES if matches(n, names))

def region(label, key, icon, sides=('Left', 'Center', 'Right')):
    kids = [(s, ['F16_%s_Region_%s_' % (key, s)], None, False, None) for s in sides if count(['F16_%s_Region_%s_' % (key, s)])]
    return (label, ['F16_%s_Region_' % key], icon, False, kids)
TREE = ('F-16 (AF-3287)', ['F16_', 'Cockpit'], 'plane', True, [
    ('Fuselage', [], 'fuselage', True, [('Cockpit', ['Cockpit'], 'plane', False, None), region('Nose', 'Nose', 'plane'), region('Forward Fuselage', 'Forward_Fuselage', 'fuselage'),
                                        region('Mid Fuselage', 'Mid_Fuselage', 'fuselage'), region('Aft Fuselage', 'Aft_Fuselage', 'fuselage'),
                                        region('Underside', 'Underside', 'layers')]),
    ('Wing', [], 'wing', True, [('Left Wing', ['F16_Wing_Region_Left_'], 'wing', False, None),
                                ('Right Wing', ['F16_Wing_Region_Right_'], 'wing', False, None)]),
    ('Empennage', [], 'fin', True, [region('Vertical Tail', 'Vertical_Tail', 'fin'), region('Horizontal Tail', 'Horizontal_Tail', 'fin', ('Left', 'Right'))])])

def prefixes_of(node):
    label, names, icon, exp, kids = node
    if names: return names
    out = []
    for c in kids or []: out += prefixes_of(c)
    return out
total = count(prefixes_of(TREE))
assert total == len(NAMES), (total, len(NAMES))       # every part is in the tree

# ---------------------------------------------------------------- prefab: remove the old panel
d = Doc(P)
def index(t):
    hs = list(re.finditer(r'^--- !u!(\d+) &(-?\d+)', t, re.M))
    return {m.group(2): (m.start(), hs[i + 1].start() if i + 1 < len(hs) else len(t)) for i, m in enumerate(hs)}
def scan():
    global D, name, rt, father, kids_of, go_of
    D = index(d.t)
    name, rt, father, kids_of = {}, {}, {}, {}
    for k_ in D:
        b = B(k_)
        if b.startswith('--- !u!1 ') and 'm_Name' in b: name[k_] = re.search(r'm_Name: (.*)', b).group(1)
        if b.startswith('--- !u!224') or b.startswith('--- !u!4 '):
            g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b).group(1); rt[g] = k_
            father[k_] = re.search(r'm_Father: \{fileID: (-?\d+)', b).group(1)
    go_of = {v: k_ for k_, v in rt.items()}
    for k_, fa in father.items(): kids_of.setdefault(fa, []).append(k_)
B = lambda k_: d.t[D[k_][0]:D[k_][1]]
def find(path):
    p = path.split('/')
    c = [r for r, fa in father.items() if fa == '0' and name.get(go_of[r]) == p[0]]
    for nm in p[1:]: c = [x for r in c for x in kids_of.get(r, []) if name.get(go_of[x]) == nm]
    return c
def remove(path):
    scan()
    old = find(path)
    if not old: return
    parent = father[old[0]]
    dead = set()
    stack = [old[0]]
    while stack:
        r = stack.pop(); g = go_of[r]
        dead.add(r); dead.add(g)
        dead.update(re.findall(r'component: \{fileID: (-?\d+)\}', B(g)))
        stack += kids_of.get(r, [])
    pb = B(parent); pb2 = pb.replace('  - {fileID: %s}\n' % old[0], '')
    assert pb2 != pb
    spans = sorted([D[x] for x in dead] + [D[parent]], key=lambda s: -s[0])
    for s_, e in spans:
        piece = d.t[s_:e]
        d.t = d.t[:s_] + (pb2 if piece == pb else '') + d.t[e:]
    d.ids = set(re.findall(r'^--- !u!\d+ &(-?\d+)', d.t, re.M))
    print('removed', path, len(dead), 'docs')
remove('HELIOS UI/UI/Pages/3D View/Aircraft Hierarchy')
remove('HELIOS UI/UI/Part Tooltip')
scan()
PAGE_RT = find('HELIOS UI/UI/Pages/3D View')[0]
UI_RT = find('HELIOS UI/UI')[0]

# ---------------------------------------------------------------- styling helpers (HELIOS theme roles)
ROUNDED, ROUNDED_LINE, SQUARE = 'bd47ad8e7de76dd458fc070441db5007', '5e10806c54123544cb7cf367a35f1b02', '877cdca0fe09b29418ae0d92cfa4eee7'
ICON = {'chevron': '71da8d57c04ea4f41baca442d3de7691', 'chevrondown': 'aa42448b2779b3246812102f064cbded', 'layers': '2a41c357c28d77e4b827259b33f70e35',
        'plane': 'f7c376fc7aaa0164b902996151cc5de1', 'wing': 'e4c4dfa6d2ecf124ab49b7f063326c19', 'search': 'c7324f2bc9c849ca84a02050636a9153',
        'fuselage': '412cd4d35d6f4f15aa4e1ad63ac33aea', 'fin': 'ab937a2daa5945879b8472129ce7e265',
        'eye': 'f2756f1cc799459585563ad87f165968', 'eyeoff': 'd2adce4a60344fe5bf79cad2c65d39c1'}
THEMED = '346a0a1447b7ab8b6ace9c7385c9dfe7'
TREE_GUID, NODE_GUID = 'b71166389a9e4db787af7d5a305b8f24', 'a7dbbb5ed0b347739ddb6d31a6132e58'
SCROLL, MASK2D = '1aa08ab6e0800fa44ae55d278d1423e3', '3312d7739989d2b4e91e6319e9a96d76'
TMP_TEMPLATE = d.t[D['7240462431924624'][0]:D['7240462431924624'][1]]     # Cards > Total Flight Hours > Label (TMP, HELIOS font)
assert 'TextMeshProUGUI' in TMP_TEMPLATE
C = {3: (0.043137256, 0.07450981, 0.15686275, 0.9), 4: (0.16470589, 0.21960784, 0.3764706, 1), 5: (0.078431375, 0.12156863, 0.23529412, 1),
     10: (0.9254902, 0.94509804, 0.98039216, 1), 11: (0.59607846, 0.64705884, 0.7607843, 1), 13: (1, 1, 1, 1),
     20: (0.4392157, 0.47058824, 1, 1), 22: (0.1764706, 0.19215687, 0.5019608, 1), 23: (0.4392157, 0.47058824, 1, 0.9019608)}

def image(o, guid, color, sliced=False, raycast=False, ppu=1.0, preserve=False):
    d.canvas_renderer(o)
    sp = '{fileID: 21300000, guid: %s, type: 3}' % guid if guid else '{fileID: 0}'
    return d.mono(o, 'fe87c0e1cc204ed48ad3b37840f39efc', 'UnityEngine.UI::UnityEngine.UI.Image',
        '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: %s\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: %s\n  m_Type: %d\n'
        '  m_PreserveAspect: %s\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n'
        '  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: %s\n'
        % (col(color), f(raycast), sp, 1 if sliced else 0, f(preserve), f(float(ppu))))
def themed(o, role, opacity=1.0):
    return d.mono(o, THEMED, 'Assembly-CSharp::Helios.HeliosThemed', '  role: %d\n  opacity: %s\n  useThemeFont: 1\n' % (role, f(float(opacity))))
def stretch(nm, parent):
    return d.go(nm, parent, anchor_min=(0, 0), anchor_max=(1, 1), pos=(0, 0), size=(0, 0))
def rounded(o, fill_role, line_role, radius):
    mult = round(24.0 / radius, 3)
    fo = stretch('Fill', o['tr']); image(fo, ROUNDED, C[fill_role], sliced=True, ppu=mult); themed(fo, fill_role)
    if line_role:
        lo = stretch('Outline', o['tr']); image(lo, ROUNDED_LINE, C[line_role], sliced=True, ppu=mult); themed(lo, line_role)
def tmp(o, text, size, role, bold=False, halign=1, valign=512):
    d.canvas_renderer(o)
    cid = d.nid(); o['comps'].append(cid)
    b = re.sub(r'^--- !u!114 &-?\d+', '--- !u!114 &' + cid, TMP_TEMPLATE)
    b = re.sub(r'm_GameObject: \{fileID: -?\d+\}', 'm_GameObject: {fileID: %s}' % o['go'], b, count=1)
    c_ = C[role]
    rep = {r'\n  m_text: .*': '\n  m_text: ' + text, r'\n  m_fontSize: [\d.]+': '\n  m_fontSize: %s' % f(float(size)),
           r'\n  m_fontSizeBase: [\d.]+': '\n  m_fontSizeBase: %s' % f(float(size)), r'\n  m_fontStyle: \d+': '\n  m_fontStyle: %d' % (1 if bold else 0),
           r'\n  m_HorizontalAlignment: \d+': '\n  m_HorizontalAlignment: %d' % halign, r'\n  m_VerticalAlignment: \d+': '\n  m_VerticalAlignment: %d' % valign,
           r'\n  m_fontColor: \{[^}]*\}': '\n  m_fontColor: ' + col(c_), r'\n  m_overflowMode: \d+': '\n  m_overflowMode: 1',
           r'\n  m_enableWordWrapping: \d': '\n  m_enableWordWrapping: 0', r'\n  m_TextWrappingMode: \d': '\n  m_TextWrappingMode: 0'}
    for kk, v in rep.items(): b = re.sub(kk, lambda m: v, b, count=1)
    c32 = (255 << 24) | (int(round(c_[2] * 255)) << 16) | (int(round(c_[1] * 255)) << 8) | int(round(c_[0] * 255))
    b = re.sub(r'(\n  m_fontColor32:\n    serializedVersion: 2\n    rgba: )\d+', lambda m: m.group(1) + str(c32), b, count=1)
    o['docs'].append(b if b.endswith('\n') else b + '\n')
    themed(o, role)
    return cid
def icon_obj(parent, key, cx, size, role, anchor=(0, 0.5), raycast=False):
    o = d.go('Icon', parent, anchor_min=anchor, anchor_max=anchor, pivot=(0.5, 0.5), pos=(cx, 0), size=(size, size))
    img = image(o, ICON[key], C[role], preserve=True, raycast=raycast); themed(o, role)
    return o, img
def ignore_layout(o):
    d.mono(o, '306cc8c2b49d7114eaa3623786fc2126', 'UnityEngine.UI::UnityEngine.UI.LayoutElement',
           '  m_IgnoreLayout: 1\n  m_MinWidth: -1\n  m_MinHeight: -1\n  m_PreferredWidth: -1\n  m_PreferredHeight: -1\n'
           '  m_FlexibleWidth: -1\n  m_FlexibleHeight: -1\n  m_LayoutPriority: 1\n')
def call(target, method):
    return ('      - m_Target: {fileID: %s}\n        m_TargetAssemblyTypeName: Helios.HeliosTreeNode, Assembly-CSharp\n        m_MethodName: %s\n        m_Mode: 1\n'
            '        m_Arguments:\n          m_ObjectArgument: {fileID: 0}\n          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine\n          m_IntArgument: 0\n'
            '          m_FloatArgument: 0\n          m_StringArgument: \n          m_BoolArgument: 0\n        m_CallState: 2\n' % (target, method))
def button(o, target_graphic, calls, tint=True):
    """tint: HELIOS hover wash (white, alpha on hover); else no transition (icon buttons)."""
    cols = ('    m_NormalColor: {r: 1, g: 1, b: 1, a: 0}\n    m_HighlightedColor: {r: 1, g: 1, b: 1, a: 0.06}\n    m_PressedColor: {r: 1, g: 1, b: 1, a: 0.12}\n'
            '    m_SelectedColor: {r: 1, g: 1, b: 1, a: 0}\n') if tint else (
            '    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n    m_HighlightedColor: {r: 1, g: 1, b: 1, a: 1}\n    m_PressedColor: {r: 1, g: 1, b: 1, a: 1}\n'
            '    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}\n')
    return d.mono(o, '4e29b1a8efbd4b44bb3f3716e73f07ff', 'UnityEngine.UI::UnityEngine.UI.Button',
        '  m_Navigation:\n    m_Mode: 0\n    m_WrapAround: 0\n    m_SelectOnUp: {fileID: 0}\n    m_SelectOnDown: {fileID: 0}\n'
        '    m_SelectOnLeft: {fileID: 0}\n    m_SelectOnRight: {fileID: 0}\n  m_Transition: %d\n  m_Colors:\n%s'
        '    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}\n'
        '    m_ColorMultiplier: 1\n    m_FadeDuration: 0.08\n  m_SpriteState:\n    m_HighlightedSprite: {fileID: 0}\n    m_PressedSprite: {fileID: 0}\n'
        '    m_SelectedSprite: {fileID: 0}\n    m_DisabledSprite: {fileID: 0}\n  m_AnimationTriggers:\n    m_NormalTrigger: Normal\n'
        '    m_HighlightedTrigger: Highlighted\n    m_PressedTrigger: Pressed\n    m_SelectedTrigger: Selected\n    m_DisabledTrigger: Disabled\n'
        '  m_Interactable: 1\n  m_TargetGraphic: {fileID: %s}\n  m_OnClick:\n    m_PersistentCalls:\n%s'
        % (1 if tint else 0, cols, target_graphic, '      m_Calls: []\n' if not calls else '      m_Calls:\n' + ''.join(calls)))

# ---------------------------------------------------------------- panel + header
MENU_X, MENU_W, PAD = 12, 143, k(11)
panel = d.go('Aircraft Hierarchy', PAGE_RT, anchor_min=(0, 0), anchor_max=(0, 1), pivot=(0, 1), pos=(MENU_X + MENU_W + k(10), -46), size=(k(266), -58))
rounded(panel, 3, 4, k(12))
hdr = d.go('Header', panel['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, -PAD), size=(-2 * PAD, k(40)))
tile = d.go('Tile', hdr['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0, 0.5), pos=(0, 0), size=(k(40), k(40)))
rounded(tile, 5, 23, k(10)); icon_obj(tile['tr'], 'layers', 0, k(24), 20, anchor=(0.5, 0.5))
t1 = d.go('Title', hdr['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0, 1), pos=(k(54), 0), size=(-k(54) - k(46), k(24)))
tmp(t1, 'Aircraft Hierarchy', 13, 10, bold=True)
t2 = d.go('Serial', hdr['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0, 1), pos=(k(54), -k(24)), size=(-k(54) - k(46), k(16)))
tmp(t2, 'F-16 (AF-3287)  \xb7  %d parts' % len(NAMES), 9.5, 11)
srch = d.go('Search', hdr['tr'], anchor_min=(1, 0.5), anchor_max=(1, 0.5), pivot=(1, 0.5), pos=(0, 0), size=(k(36), k(36)))
rounded(srch, 5, 4, k(9)); icon_obj(srch['tr'], 'search', 0, k(16), 11, anchor=(0.5, 0.5))
h = stretch('Hover', srch['tr']); hi = image(h, ROUNDED, (1, 1, 1, 1), sliced=True, raycast=True, ppu=round(24.0 / k(9), 3))
button(srch, hi, [])
d.tooltip(srch, 'Search components')

TOP, BOTTOM = k(56), k(21)
view = d.go('Tree', panel['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pivot=(0.5, 0.5), pos=(0, -(TOP - BOTTOM) / 2), size=(-2 * PAD, -(TOP + BOTTOM)))
image(view, None, (0, 0, 0, 0), raycast=True)
d.mono(view, MASK2D, 'UnityEngine.UI::UnityEngine.UI.RectMask2D', '  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')
content = d.go('Content', view['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, 0))
d.layout_group(content, True, 0, (0, 0, 0, 0), 0, True, False)
d.fitter(content, 0, 2)
d.mono(view, SCROLL, 'UnityEngine.UI::UnityEngine.UI.ScrollRect',
    '  m_Content: {fileID: %s}\n  m_Horizontal: 0\n  m_Vertical: 1\n  m_MovementType: 2\n  m_Elasticity: 0.1\n  m_Inertia: 1\n'
    '  m_DecelerationRate: 0.12\n  m_ScrollSensitivity: 30\n  m_Viewport: {fileID: %s}\n  m_HorizontalScrollbar: {fileID: 0}\n'
    '  m_VerticalScrollbar: {fileID: 0}\n  m_HorizontalScrollbarVisibility: 0\n  m_VerticalScrollbarVisibility: 0\n'
    '  m_HorizontalScrollbarSpacing: 0\n  m_VerticalScrollbarSpacing: 0\n  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
    % (content['tr'], view['tr']))

# ---------------------------------------------------------------- rows
# per depth (Figma px): row height, chevron x / size, icon x / size, label x, font, bold, badge w x h, badge right inset
STYLE = {0: dict(h=42, cx=10, cs=16, ix=36, isz=20, lx=66, fs=12, bold=True, bw=43, bh=22, br=6, ir=20),
         1: dict(h=34, cx=13, cs=14, ix=37, isz=20, lx=67, fs=11.5, bold=True, bw=36, bh=22, br=4, ir=11),
         2: dict(h=34, cx=29, cs=14, ix=53, isz=16, lx=77, fs=11, bold=False, bw=32, bh=22, br=4, ir=11),
         3: dict(h=32, cx=None, cs=0, ix=None, isz=0, lx=89, fs=10.5, bold=False, bw=32, bh=20, br=4, ir=11)}
EYE = 15          # eye icon (Figma px); its button is 24 wide

def build(parent, node, depth):
    label, names, ico, expanded, children = node
    s = STYLE[depth]
    n_parts = count(prefixes_of(node))
    holder = d.go(label, parent, anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, 0))
    d.layout_group(holder, True, 0, (0, 0, 0, 0), 0, True, False)
    row = d.go('Row', holder['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, k(s['h'])))
    d.layout_element(row, h=k(s['h']), mh=k(s['h']))
    hov = stretch('Hover', row['tr']); hov_img = image(hov, ROUNDED, (1, 1, 1, 1), sliced=True, raycast=True, ppu=round(24.0 / k(8), 3))
    sel = d.go('Selected', row['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pos=(0, 0), size=(0, -2), active=False)
    rounded(sel, 22, 23, k(8))
    chev_img = 0; chev_btn = None
    if s['cx'] is not None and children:
        cb = d.go('Chevron', row['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0.5, 0.5), pos=(k(s['cx'] + s['cs'] / 2), 0), size=(k(s['cs'] + 10), k(s['h'])))
        hit = image(cb, None, (0, 0, 0, 0), raycast=True)
        ci, chev_img = icon_obj(cb['tr'], 'chevrondown' if expanded else 'chevron', 0, k(s['cs']), 11, anchor=(0.5, 0.5))
        chev_btn = (cb, hit)
    if ico: icon_obj(row['tr'], ico, k(s['ix'] + s['isz'] / 2), k(s['isz']), 20 if depth <= 1 else 11)
    if depth == 3:
        tick = d.go('Tick', row['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0, 0.5), pos=(k(36.5), 0), size=(k(46), 1))
        image(tick, SQUARE, (1, 1, 1, 0.1)); themed(tick, 13, 0.1)
    right = k(s['br'] + s['bw'] + 6)
    lab = d.go('Label', row['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pivot=(0, 0.5), pos=(k(s['lx']), 0), size=(-k(s['lx']) - right, 0))
    tmp(lab, label, s['fs'], 10 if depth <= 2 else 11, bold=s['bold'])
    eye_img = 0
    badge = d.go('Count', row['tr'], anchor_min=(1, 0.5), anchor_max=(1, 0.5), pivot=(1, 0.5), pos=(-k(s['br']), 0), size=(k(s['bw']), k(s['bh'])))
    rounded(badge, 5, 23 if depth == 0 else 4, k(s['bh']) / 2)
    bt = d.go('Text', badge['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pos=(0, 0), size=(0, 0))
    count_tmp = tmp(bt, str(n_parts), 9.5, 10 if depth == 0 else 11, bold=depth == 0, halign=2)
    kids_go = None
    if children:
        kids_go = d.go('Children', holder['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, 0), active=expanded)
        d.layout_group(kids_go, True, 0, (0, 0, 0, 0), 0, True, False)
        if depth >= 1:
            gl = d.go('Guide', kids_go['tr'], anchor_min=(0, 0), anchor_max=(0, 1), pivot=(0.5, 0.5), pos=(k(s['cx'] + s['cs'] / 2), 0), size=(1, -k(8)))
            image(gl, SQUARE, (1, 1, 1, 0.1)); themed(gl, 13, 0.1); ignore_layout(gl)
        for c in children: build(kids_go['tr'], c, depth + 1)
    names = with_moves(names)
    names_yaml = 'partNames: []\n' if not names else 'partNames:\n' + ''.join('  - %s\n' % (("'%s'" % nm) if nm.startswith('!') else nm) for nm in names)
    nc = d.mono(row, NODE_GUID, 'Assembly-CSharp::Helios.HeliosTreeNode',
                '  %s  children: {fileID: %s}\n  chevron: {fileID: %s}\n  selectedMark: {fileID: %s}\n  eye: {fileID: %s}\n  count: {fileID: %s}\n  expanded: %s\n'
                % (names_yaml, kids_go['go'] if kids_go else 0, chev_img, sel['go'], eye_img, count_tmp, f(bool(expanded and children))))
    button(row, hov_img, [call(nc, 'OnClick')])
    if chev_btn: button(chev_btn[0], chev_btn[1], [call(nc, 'OnChevron')], tint=False)

build(content['tr'], TREE, 0)
d.mono(panel, TREE_GUID, 'Assembly-CSharp::Helios.HeliosTree',
       '  chevronRight: {fileID: 21300000, guid: %s, type: 3}\n  chevronDown: {fileID: 21300000, guid: %s, type: 3}\n'
       '  eyeOpen: {fileID: 0}\n  eyeClosed: {fileID: 0}\n  selected: {fileID: 0}\n  liveCounts: 1\n'
       % (ICON['chevron'], ICON['chevrondown']))
# ---------------------------------------------------------------- hover tooltip over the 3D model (HeliosPartTooltip)
TIP_GUID = '59b4da8067144ec590ae8f0a54571979'
tipa = stretch('Part Tooltip', UI_RT)
box = d.go('Box', tipa['tr'], anchor_min=(0.5, 0.5), anchor_max=(0.5, 0.5), pivot=(0, 0), pos=(0, 0), size=(180, 26), active=False)
rounded(box, 3, 20, 7)
bt = d.go('Text', box['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pos=(0, 0), size=(-20, 0))
tip_text = tmp(bt, 'Part 0000   Nose \xb7 Left   Fuselage', 11, 10, halign=1)
d.mono(tipa, TIP_GUID, 'Assembly-CSharp::Helios.HeliosPartTooltip',
       '  box: {fileID: %s}\n  text: {fileID: %s}\n  offset: {x: 16, y: 14}\n  padding: 10\n  height: 26\n' % (box['tr'], tip_text))

d.save()
print('built:', len(d.new), 'objects;', len(NAMES), 'parts in the tree')
