"""HELIOS 3D View page, matched to Figma frame 956:5442 (HELIOS units = Figma x 0.8125, same as the top bar and menu):
  1. Menu > Shadow off (its 9-slice seam drew a line on both sides of the menu at the 3D View item)
  2. Cards > Flight Hours: Figma 200 x 131 card (padding 11, rows 44, tile 37)
  3. View Tools: Figma 56 x 402 strip, 38 px buttons (+ the Cross Section controls moved beside it)
  4. Aircraft Hierarchy panel (Figma 266 x 520, right of the menu): header, scrolling tree with chevrons, icons,
     count badges, guide lines, selection; HeliosTree / HeliosTreeNode, all built here (edit time)."""
import re, sys
sys.path.insert(0, __import__('os').path.dirname(__import__('os').path.abspath(__file__)))
from unity_yaml import Doc, col, f

P = 'Assets/HELIOS/Resources/HELIOS UI.prefab'
K = 0.8125
def k(v): return round(v * K, 2)

d = Doc(P)
assert 'm_Name: Aircraft Hierarchy' not in d.t, 'already added'

def index(t):
    hs = list(re.finditer(r'^--- !u!(\d+) &(-?\d+)', t, re.M))
    D = {m.group(2): (m.start(), hs[i + 1].start() if i + 1 < len(hs) else len(t)) for i, m in enumerate(hs)}
    return D
def body(t, D, k_): s, e = D[k_]; return t[s:e]
D = index(d.t)
name = {k_: re.search(r'm_Name: (.*)', body(d.t, D, k_)).group(1) for k_ in D if d.t[D[k_][0]:].startswith('--- !u!1 ') and 'm_Name' in body(d.t, D, k_)}
rt, father, kids = {}, {}, {}
for k_ in D:
    b = body(d.t, D, k_)
    if b.startswith('--- !u!224') or b.startswith('--- !u!4 '):
        g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b).group(1); rt[g] = k_
        father[k_] = re.search(r'm_Father: \{fileID: (-?\d+)', b).group(1)
go_of = {v: k_ for k_, v in rt.items()}
for k_, fa in father.items(): kids.setdefault(fa, []).append(k_)
def find(path):
    p = path.split('/')
    c = [r for r, fa in father.items() if fa == '0' and name.get(go_of[r]) == p[0]]
    for nm in p[1:]: c = [x for r in c for x in kids.get(r, []) if name.get(go_of[x]) == nm]
    assert len(c) >= 1, path
    return c
def comps(g): return re.findall(r'component: \{fileID: (-?\d+)\}', body(d.t, D, g))

edits = {}
def sub(k_, a, b_, count=1):
    cur = edits.get(k_, body(d.t, D, k_)); assert a in cur, (k_, a); edits[k_] = cur.replace(a, b_, count)
def rect_set(rt_id, pos=None, size=None):
    cur = edits.get(rt_id, body(d.t, D, rt_id))
    if pos is not None: cur = re.sub(r'm_AnchoredPosition: \{[^}]*\}', 'm_AnchoredPosition: {x: %s, y: %s}' % (f(float(pos[0])), f(float(pos[1]))), cur, count=1)
    if size is not None: cur = re.sub(r'm_SizeDelta: \{[^}]*\}', 'm_SizeDelta: {x: %s, y: %s}' % (f(float(size[0])), f(float(size[1]))), cur, count=1)
    edits[rt_id] = cur
def comp_of(g, cls_marker):
    return next(c for c in comps(g) if cls_marker in body(d.t, D, c))

# ---------------------------------------------------------------- 1. menu shadow off
sh = go_of[find('HELIOS UI/UI/Menu/Shadow')[0]]
sub(sh, 'm_IsActive: 1', 'm_IsActive: 0')

# ---------------------------------------------------------------- 2. flight hours card (Figma 200 x 131)
CARD_W = k(200)
cards = find('HELIOS UI/UI/Pages/3D View/Cards')[0]
rect_set(cards, pos=(-k(76), -46), size=(CARD_W, -58))
fh = go_of[find('HELIOS UI/UI/Pages/3D View/Cards/Flight Hours')[0]]
vlg = comp_of(fh, 'VerticalLayoutGroup')
pad = k(11)
cur = body(d.t, D, vlg)
cur = re.sub(r'm_Left: \S+', 'm_Left: %d' % round(pad), cur, 1); cur = re.sub(r'm_Right: \S+', 'm_Right: %d' % round(pad), cur, 1)
cur = re.sub(r'm_Top: \S+', 'm_Top: %d' % round(pad), cur, 1); cur = re.sub(r'm_Bottom: \S+', 'm_Bottom: %d' % round(pad), cur, 1)
cur = re.sub(r'm_Spacing: \S+', 'm_Spacing: %s' % f(k(10)), cur, 1)
edits[vlg] = cur
ROW_H, TILE, ICON_S, TX = k(44), k(37), k(20), k(51)
for row in ('Total Flight Hours', 'Next Service Due'):
    rr = find('HELIOS UI/UI/Pages/3D View/Cards/Flight Hours/' + row)[0]; rg = go_of[rr]
    le = comp_of(rg, 'LayoutElement')
    sub(le, 'm_MinHeight: 48', 'm_MinHeight: %s' % f(ROW_H)); sub(le, 'm_PreferredHeight: 48', 'm_PreferredHeight: %s' % f(ROW_H))
    rect_set(find('HELIOS UI/UI/Pages/3D View/Cards/Flight Hours/%s/Tile' % row)[0], size=(TILE, TILE))
    rect_set(find('HELIOS UI/UI/Pages/3D View/Cards/Flight Hours/%s/Tile/Icon' % row)[0], size=(k(18), k(18)))
    inner = CARD_W - 2 * round(pad) - TX
    lab = find('HELIOS UI/UI/Pages/3D View/Cards/Flight Hours/%s/Label' % row)[0]
    rect_set(lab, pos=(TX, -k(2)), size=(inner, k(16)))
    val = find('HELIOS UI/UI/Pages/3D View/Cards/Flight Hours/%s/Value' % row)[0]
    rect_set(val, pos=(TX, -k(17)), size=(inner, k(26)))
    for tr_, size in ((lab, 10), (val, 15)):
        tm = comp_of(go_of[tr_], 'TextMeshProUGUI')
        cur = edits.get(tm, body(d.t, D, tm))
        cur = re.sub(r'\n  m_fontSize: [\d.]+', '\n  m_fontSize: %d' % size, cur, 1)
        cur = re.sub(r'\n  m_fontSizeBase: [\d.]+', '\n  m_fontSizeBase: %d' % size, cur, 1)
        edits[tm] = cur

# ---------------------------------------------------------------- 3. view tools (Figma 56 x 402, 38 px buttons)
vt = find('HELIOS UI/UI/Pages/3D View/View Tools')[0]
rect_set(vt, pos=(-k(10), -46), size=(k(56), k(402)))
BTN = k(38)
centres = [28, 80, 118, 156, 194, 246, 284, 322, 374]      # Figma button centres (top of the strip = 0)
seps = [53.5, 219.5, 347.5]
order = [go_of[c] for c in kids[vt]]
bi = si = 0
scale_icon = BTN / 21.75
for g in order:
    n = name[g]; r_ = rt[g]
    if n in ('Fill', 'Outline'): continue
    if n == 'Separator':
        rect_set(r_, pos=(0, -k(seps[si])), size=(k(20), 1)); si += 1
    else:
        rect_set(r_, pos=(0, -k(centres[bi])), size=(BTN, BTN)); bi += 1
        for c in kids.get(r_, []):                       # icons / highlight inside the button
            cb = edits.get(c, body(d.t, D, c))
            m = re.search(r'm_SizeDelta: \{x: ([-\d.]+), y: ([-\d.]+)\}', cb)
            am = re.search(r'm_AnchorMin: \{x: ([-\d.]+), y: ([-\d.]+)\}', cb).groups()
            ax = re.search(r'm_AnchorMax: \{x: ([-\d.]+), y: ([-\d.]+)\}', cb).groups()
            if am == ax:                                  # fixed-size child: scale it with the button
                rect_set(c, size=(round(float(m.group(1)) * scale_icon, 2), round(float(m.group(2)) * scale_icon, 2)))
assert bi == 9 and si == 3, (bi, si)
csc = find('HELIOS UI/UI/Pages/3D View/Cross Section Controls')[0]
rect_set(csc, pos=(-(k(10) + k(56) + 4.6), -46 - k(374)))

# apply the edits to the existing docs (back to front so offsets stay valid)
for k_ in sorted(edits, key=lambda x: -D[x][0]):
    s, e = D[k_]; d.t = d.t[:s] + edits[k_] + d.t[e:]

# ---------------------------------------------------------------- 4. aircraft hierarchy panel
ROUNDED, ROUNDED_LINE, SQUARE = 'bd47ad8e7de76dd458fc070441db5007', '5e10806c54123544cb7cf367a35f1b02', '877cdca0fe09b29418ae0d92cfa4eee7'
ICON = {'chevron': '71da8d57c04ea4f41baca442d3de7691', 'chevrondown': 'aa42448b2779b3246812102f064cbded', 'layers': '2a41c357c28d77e4b827259b33f70e35',
        'plane': 'f7c376fc7aaa0164b902996151cc5de1', 'fan': '4435564b9cc69f444bba9c676d2f2aa5', 'chip': '82d5e7794fa647c47861fe2032d2ec0e',
        'wing': 'e4c4dfa6d2ecf124ab49b7f063326c19', 'search': 'c7324f2bc9c849ca84a02050636a9153', 'bolt': 'bd637ba67c43483b9a17f6344e286ad4',
        'thermo': 'df95cc5de8c242469fbdef4da4b585ac', 'fuselage': '412cd4d35d6f4f15aa4e1ad63ac33aea', 'fin': 'ab937a2daa5945879b8472129ce7e265',
        'wheel': '1212bb92795e4046baf3ddedf0c2df24', 'engine': '5cfc29f4914343c6a283bb41ffb2c3c7'}
THEMED = '346a0a1447b7ab8b6ace9c7385c9dfe7'
TREE, NODE = 'b71166389a9e4db787af7d5a305b8f24', 'a7dbbb5ed0b347739ddb6d31a6132e58'
SCROLL, MASK2D = '1aa08ab6e0800fa44ae55d278d1423e3', '3312d7739989d2b4e91e6319e9a96d76'
D2 = index(d.t)
TMP_TEMPLATE = body(d.t, D2, '7240462431924624')        # Cards > Total Flight Hours > Label (TextMeshProUGUI, HELIOS font)
assert 'TextMeshProUGUI' in TMP_TEMPLATE
PAGE_RT = find('HELIOS UI/UI/Pages/3D View')[0]
# theme colours (dark) by role
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
def stretch(nm, parent, inset=(0, 0, 0, 0)):
    l, t_, r_, b_ = inset
    return d.go(nm, parent, anchor_min=(0, 0), anchor_max=(1, 1), pos=((l - r_) / 2, (b_ - t_) / 2), size=(-(l + r_), -(t_ + b_)))
def rounded(o, fill_role, line_role, radius, fill_opacity=1.0):
    mult = round(24.0 / radius, 3)
    fo = stretch('Fill', o['tr']); c_ = C[fill_role]
    image(fo, ROUNDED, (c_[0], c_[1], c_[2], c_[3] * fill_opacity), sliced=True, ppu=mult); themed(fo, fill_role, fill_opacity)
    if line_role:
        lo = stretch('Outline', o['tr']); image(lo, ROUNDED_LINE, C[line_role], sliced=True, ppu=mult); themed(lo, line_role)
    return fo
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
def icon(parent, key, x, size, role=10, anchor=(0, 0.5), y=0):
    o = d.go('Icon', parent, anchor_min=anchor, anchor_max=anchor, pivot=(0.5, 0.5), pos=(x + size / 2, y), size=(size, size))
    image(o, ICON[key], C[role], preserve=True); themed(o, role); return o
def ignore_layout(o):
    d.mono(o, '306cc8c2b49d7114eaa3623786fc2126', 'UnityEngine.UI::UnityEngine.UI.LayoutElement',
           '  m_IgnoreLayout: 1\n  m_MinWidth: -1\n  m_MinHeight: -1\n  m_PreferredWidth: -1\n  m_PreferredHeight: -1\n'
           '  m_FlexibleWidth: -1\n  m_FlexibleHeight: -1\n  m_LayoutPriority: 1\n')
def call(target, type_name, method, mode):
    return ('      - m_Target: {fileID: %s}\n        m_TargetAssemblyTypeName: %s, Assembly-CSharp\n        m_MethodName: %s\n        m_Mode: %d\n'
            '        m_Arguments:\n          m_ObjectArgument: {fileID: 0}\n          m_ObjectArgumentAssemblyTypeName: \n          m_IntArgument: 0\n'
            '          m_FloatArgument: 0\n          m_StringArgument: \n          m_BoolArgument: 0\n        m_CallState: 2\n' % (target, type_name, method, mode))
def hover_button(o, radius, calls):
    h = stretch('Hover', o['tr'])
    hi = image(h, ROUNDED, (1, 1, 1, 1), sliced=True, raycast=True, ppu=round(24.0 / radius, 3))
    ignore_layout(h)
    d.mono(o, '4e29b1a8efbd4b44bb3f3716e73f07ff', 'UnityEngine.UI::UnityEngine.UI.Button',
        '  m_Navigation:\n    m_Mode: 0\n    m_WrapAround: 0\n    m_SelectOnUp: {fileID: 0}\n    m_SelectOnDown: {fileID: 0}\n'
        '    m_SelectOnLeft: {fileID: 0}\n    m_SelectOnRight: {fileID: 0}\n  m_Transition: 1\n  m_Colors:\n'
        '    m_NormalColor: {r: 1, g: 1, b: 1, a: 0}\n    m_HighlightedColor: {r: 1, g: 1, b: 1, a: 0.06}\n    m_PressedColor: {r: 1, g: 1, b: 1, a: 0.12}\n'
        '    m_SelectedColor: {r: 1, g: 1, b: 1, a: 0}\n    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}\n'
        '    m_ColorMultiplier: 1\n    m_FadeDuration: 0.08\n  m_SpriteState:\n    m_HighlightedSprite: {fileID: 0}\n    m_PressedSprite: {fileID: 0}\n'
        '    m_SelectedSprite: {fileID: 0}\n    m_DisabledSprite: {fileID: 0}\n  m_AnimationTriggers:\n    m_NormalTrigger: Normal\n'
        '    m_HighlightedTrigger: Highlighted\n    m_PressedTrigger: Pressed\n    m_SelectedTrigger: Selected\n    m_DisabledTrigger: Disabled\n'
        '  m_Interactable: 1\n  m_TargetGraphic: {fileID: %s}\n  m_OnClick:\n    m_PersistentCalls:\n%s'
        % (hi, '      m_Calls: []\n' if not calls else '      m_Calls:\n' + ''.join(calls)))

# panel (Figma x 196 = menu 10 + 176 + 10; 266 wide; down to 10 above the bottom - here like the Cards: -58)
MENU_X, MENU_W = 12, 143
panel = d.go('Aircraft Hierarchy', PAGE_RT, anchor_min=(0, 0), anchor_max=(0, 1), pivot=(0, 1), pos=(MENU_X + MENU_W + k(10), -46), size=(k(266), -58))
rounded(panel, 3, 4, k(12))
PAD = k(11)
# header
hdr = d.go('Header', panel['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, -PAD), size=(-2 * PAD, k(40)))
tile = d.go('Tile', hdr['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0, 0.5), pos=(0, 0), size=(k(40), k(40)))
rounded(tile, 5, 23, k(10))
icon(tile['tr'], 'layers', -k(12), k(24), role=20, anchor=(0.5, 0.5))
t1 = d.go('Title', hdr['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0, 1), pos=(k(54), 0), size=(-k(54) - k(46), k(24)))
tmp(t1, 'Aircraft Hierarchy', 13, 10, bold=True)
t2 = d.go('Serial', hdr['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0, 1), pos=(k(54), -k(24)), size=(-k(54) - k(46), k(16)))
tmp(t2, 'F-16 (AF-3287)', 9.5, 11)
srch = d.go('Search', hdr['tr'], anchor_min=(1, 0.5), anchor_max=(1, 0.5), pivot=(1, 0.5), pos=(0, 0), size=(k(36), k(36)))
rounded(srch, 5, 4, k(9))
icon(srch['tr'], 'search', -k(8), k(16), role=11, anchor=(0.5, 0.5))
hover_button(srch, k(9), [])
d.tooltip(srch, 'Search components')

# tree: scroll view (Figma: from y 56, 21 above the bottom)
TOP, BOTTOM = k(56), k(21)
view = d.go('Tree', panel['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, -TOP + (TOP - BOTTOM) / 2 + (TOP - BOTTOM) / 2), size=(-2 * PAD, -(TOP + BOTTOM)))
view['pos'] = (0, -(TOP - BOTTOM) / 2) ; view['piv'] = (0.5, 0.5)
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

# tree data (Figma 956:5442): name, count, icon, expanded, children
TREE_DATA = ('F-16 (AF-3287)', 132, 'plane', True, [
    ('Airframe', 48, 'layers', True, [
        ('Fuselage', 12, 'fuselage', True, [('Nose Section', 2), ('Forward Fuselage', 3), ('Center Fuselage', 3), ('Aft Fuselage', 2), ('Tail Cone', 2)]),
        ('Wing Assembly', 14, 'wing', False, []), ('Empennage', 8, 'fin', False, []), ('Landing Gear', 14, 'wheel', False, [])]),
    ('Propulsion', 24, 'fan', True, [('Engine (L)', 12, 'engine', False, []), ('Engine (R)', 12, 'engine', False, [])]),
    ('Avionics', 32, 'chip', False, []), ('Electrical', 16, 'bolt', False, []), ('Environmental', 12, 'thermo', False, [])])
SELECTED = 'Fuselage'
# per depth (Figma px): row height, chevron x / size, icon x / size, label x, label size (HELIOS pt), bold, badge w x h, badge right inset
STYLE = {0: dict(h=42, cx=10, cs=16, ix=36, isz=20, lx=66, fs=12, bold=True, bw=43, bh=22, br=10),
         1: dict(h=34, cx=13, cs=14, ix=37, isz=20, lx=67, fs=11.5, bold=True, bw=32, bh=22, br=8),
         2: dict(h=34, cx=29, cs=14, ix=53, isz=16, lx=77, fs=11, bold=False, bw=32, bh=22, br=8),
         3: dict(h=32, cx=None, cs=0, ix=None, isz=0, lx=89, fs=10.5, bold=False, bw=24, bh=20, br=10)}
nodes = {}
selected_node = [None]
tree_comp_ref = []

def build(parent, data, depth):
    if len(data) == 2: data = (data[0], data[1], None, False, None)
    label, count, ico, expanded, children = data
    s = STYLE[depth]
    node = d.go(label, parent, anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, 0))
    d.layout_group(node, True, 0, (0, 0, 0, 0), 0, True, False)
    row = d.go('Row', node['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, k(s['h'])))
    d.layout_element(row, h=k(s['h']), mh=k(s['h']))
    sel = d.go('Selected', row['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pos=(0, 0), size=(0, -2), active=(label == SELECTED))
    rounded(sel, 22, 23, k(8))
    chev = None
    if s['cx'] is not None and children is not None:
        chev = icon(row['tr'], 'chevrondown' if expanded else 'chevron', k(s['cx']), k(s['cs']), role=11)
    if ico:
        icon(row['tr'], ico, k(s['ix']), k(s['isz']), role=20 if depth <= 1 else 11)
    if depth == 3:          # tree tick from the guide line
        tick = d.go('Tick', row['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0, 0.5), pos=(k(36.5), 0), size=(k(46), 1))
        image(tick, SQUARE, (1, 1, 1, 0.1)); themed(tick, 13, 0.1)
    lab = d.go('Label', row['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pivot=(0, 0.5), pos=(k(s['lx']), 0),
               size=(-k(s['lx']) - k(s['bw'] + s['br'] + 6), 0))
    tmp(lab, label, s['fs'], 10 if depth <= 2 else 11, bold=s['bold'])
    badge = d.go('Count', row['tr'], anchor_min=(1, 0.5), anchor_max=(1, 0.5), pivot=(1, 0.5), pos=(-k(s['br']), 0), size=(k(s['bw']), k(s['bh'])))
    rounded(badge, 5, 23 if depth == 0 else 4, k(s['bh']) / 2)
    bt = d.go('Text', badge['tr'], anchor_min=(0, 0), anchor_max=(1, 1), pos=(0, 0), size=(0, 0))
    tmp(bt, str(count), 9.5, 10 if depth == 0 else 11, bold=depth == 0, halign=2)
    kids_go = None
    if children:
        kids_go = d.go('Children', node['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(0, 0), size=(0, 0), active=expanded)
        d.layout_group(kids_go, True, 0, (0, 0, 0, 0), 0, True, False)
        gx = k(s['cx'] + s['cs'] / 2) if depth >= 1 else k(STYLE[1]['cx'] + STYLE[1]['cs'] / 2) - 2
        if depth >= 1:      # vertical guide line under the parent's chevron
            gl = d.go('Guide', kids_go['tr'], anchor_min=(0, 0), anchor_max=(0, 1), pivot=(0.5, 0.5), pos=(gx, 0), size=(1, -k(8)))
            image(gl, SQUARE, (1, 1, 1, 0.1)); themed(gl, 13, 0.1); ignore_layout(gl)
        for c in children: build(kids_go['tr'], c, depth + 1)
    # node behaviour + click
    nc = d.mono(row, NODE, 'Assembly-CSharp::Helios.HeliosTreeNode',
                '  children: {fileID: %s}\n  chevron: {fileID: %s}\n  selectedMark: {fileID: %s}\n  expanded: %s\n'
                % (kids_go['go'] if kids_go else 0, chev_img(chev), sel['go'], f(bool(expanded and children))))
    hover_button(row, k(8), [call(nc, 'Helios.HeliosTreeNode', 'OnClick', 1)])
    if label == SELECTED: selected_node[0] = nc
    return node

_chev_imgs = {}
def chev_img(o):
    if o is None: return 0
    # the Image component is the second component of the icon object (after CanvasRenderer)
    return next(c for c, doc in zip(o['comps'][1:], o['docs']) if 'UnityEngine.UI.Image' in doc)

build(content['tr'], TREE_DATA, 0)
d.mono(panel, TREE, 'Assembly-CSharp::Helios.HeliosTree',
       '  chevronRight: {fileID: 21300000, guid: %s, type: 3}\n  chevronDown: {fileID: 21300000, guid: %s, type: 3}\n  selected: {fileID: %s}\n'
       % (ICON['chevron'], ICON['chevrondown'], selected_node[0]))

d.save()
print('done:', len(d.new), 'new objects')
