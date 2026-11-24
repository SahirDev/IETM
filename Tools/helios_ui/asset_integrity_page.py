"""Asset Integrity page, right column as in Figma (UI only): Overall Integrity (gauge + 64 / 5 / 3 counts with %),
three tiles (Structural 95 %, Corrosion 2.4 %, Fatigue 4,320 hrs), Component Risk (72) with a stacked bar and legend,
Integrity Trend (30 days) - the Health page's trend card (HeliosChart) re-titled with new data and date labels.
Critical Integrity and Key Findings are switched off (not deleted). Run once from the repo root."""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc, col, f
import prefab_edit as pe

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
pe.d = d = Doc(P)
if 'Component Risk' in d.t: sys.exit('already done')
pe.scan()
A = 'HELIOS UI/UI/Pages/Asset Integrity'
CARDS = pe.find(A + '/Cards')

ROUNDED, ROUNDED_LINE = 'bd47ad8e7de76dd458fc070441db5007', '5e10806c54123544cb7cf367a35f1b02'
THEMED, LEG = '346a0a1447b7ab8b6ace9c7385c9dfe7', '306cc8c2b49d7114eaa3623786fc2126'
ICON = {'layers': '2a41c357c28d77e4b827259b33f70e35', 'sun': '896bb09406e6f464b879565c016b915b', 'bolt': 'bd637ba67c43483b9a17f6344e286ad4'}
C = {3: (0.043137256, 0.07450981, 0.15686275, 0.9), 4: (0.16470589, 0.21960784, 0.3764706, 1), 5: (0.078431375, 0.12156863, 0.23529412, 1),
     10: (0.9254902, 0.94509804, 0.98039216, 1), 11: (0.59607846, 0.64705884, 0.7607843, 1), 20: (0.4392157, 0.47058824, 1, 1)}
GREEN, AMBER, RED = (0.13333334, 0.77254903, 0.36862746, 1), (0.9607843, 0.61960787, 0.043137256, 1), (0.9372549, 0.26666668, 0.26666668, 1)
G_HEX, A_HEX, R_HEX, MUTED = '#22C55E', '#F59E0B', '#EF4444', '#98A5C2'
TMP_TEMPLATE = pe.B(pe.comp_of(pe.find(A + '/Header/Title'), 'TextMeshProUGUI'))

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
def tmp(o, s, size, role, bold=False, halign=1, valign=512):
    d.canvas_renderer(o)
    cid = d.nid(); o['comps'].append(cid)
    b = re.sub(r'^--- !u!114 &-?\d+', '--- !u!114 &' + cid, TMP_TEMPLATE)
    b = re.sub(r'm_GameObject: \{fileID: -?\d+\}', 'm_GameObject: {fileID: %s}' % o['go'], b, count=1)
    rep = {r'\n  m_text: .*': '\n  m_text: ' + s, r'\n  m_fontSize: [\d.]+': '\n  m_fontSize: %s' % f(float(size)),
           r'\n  m_fontSizeBase: [\d.]+': '\n  m_fontSizeBase: %s' % f(float(size)), r'\n  m_fontStyle: \d+': '\n  m_fontStyle: %d' % (1 if bold else 0),
           r'\n  m_HorizontalAlignment: \d+': '\n  m_HorizontalAlignment: %d' % halign, r'\n  m_VerticalAlignment: \d+': '\n  m_VerticalAlignment: %d' % valign,
           r'\n  m_overflowMode: \d+': '\n  m_overflowMode: 0', r'\n  m_enableWordWrapping: \d': '\n  m_enableWordWrapping: 0',
           r'\n  m_TextWrappingMode: \d': '\n  m_TextWrappingMode: 0', r'\n  m_isRichText: \d': '\n  m_isRichText: 1'}
    for k_, v in rep.items(): b = re.sub(k_, lambda m: v, b, count=1)
    o['docs'].append(b if b.endswith('\n') else b + '\n')
    themed(o, role)
def card_bg(o, fill=3, line=4, ppu=2.4):
    for nm, guid, role in (('Fill', ROUNDED, fill), ('Outline', ROUNDED_LINE, line)):
        x = d.go(nm, o['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
        image(x, guid, C[role], sliced=True, ppu=ppu); themed(x, role); ignore_layout(x)
def label(parent, nm, s, size, role, am, ax, pos, sz, pivot=(0, 0.5), bold=False, halign=1):
    o = d.go(nm, parent['tr'], anchor_min=am, anchor_max=ax, pivot=pivot, pos=pos, size=sz)
    tmp(o, s, size, role, bold, halign); return o

# ---------------------------------------------------------------- tiles: Structural / Corrosion / Fatigue
tiles = d.go('Integrity Metrics', CARDS, anchor_min=(0, 1), anchor_max=(0, 1), size=(318, 70))
d.layout_group(tiles, False, 8, (0, 0, 0, 0), 0, True, True)
d.layout_element(tiles, h=70, mh=70)
for nm, icon, value, change, up in (('Structural', 'layers', '95%', '+2%', True), ('Corrosion', 'sun', '2.4%', '-0.3%', False),
                                    ('Fatigue', 'bolt', '4,320 <size=10><color=%s>hrs</color></size>' % MUTED, '+6%', True)):
    t = d.go(nm, tiles['tr'], anchor_min=(0, 1), anchor_max=(0, 1), size=(100, 70))
    card_bg(t, 3, 4, 3.0)
    ic = d.go('Icon', t['tr'], anchor_min=(0, 1), anchor_max=(0, 1), pos=(18, -16), size=(13, 13))
    image(ic, ICON[icon], C[20], preserve=True); themed(ic, 20)
    label(t, 'Name', nm, 10.5, 11, (0, 1), (1, 1), (30, -16), (-34, 14))
    label(t, 'Value', value, 17, 10, (0, 1), (1, 1), (10, -36), (-14, 22), bold=True)
    label(t, 'Change', '<color=%s>%s %s</color>' % (G_HEX if up else R_HEX, '▲' if up else '▼', change), 9.5, 10, (0, 1), (1, 1), (10, -55), (-14, 14))

# ---------------------------------------------------------------- Component Risk
risk = d.go('Component Risk', CARDS, anchor_min=(0, 1), anchor_max=(0, 1), size=(318, 96))
card_bg(risk)
d.layout_element(risk, h=96, mh=96)
label(risk, 'Title', '<b>Component Risk</b> <size=11><color=%s>(72)</color></size>' % MUTED, 13, 10, (0, 1), (0, 1), (14, -24), (200, 20))
label(risk, 'View All', '<b>View All ></b>', 10.5, 20, (1, 1), (1, 1), (-14, -24), (90, 20), pivot=(1, 0.5), halign=4)
bar = d.go('Bar', risk['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 0.5), pos=(0, -50), size=(-28, 8))
for nm, a0, a1, c in (('Healthy', 0, 0.89, GREEN), ('At Risk', 0.89, 0.96, AMBER), ('Critical', 0.96, 1, RED)):
    s_ = d.go(nm, bar['tr'], anchor_min=(a0, 0), anchor_max=(a1, 1), size=(0, 0))
    image(s_, ROUNDED, c, sliced=True, ppu=6)
leg = '<color={g}>●</color> Healthy <b>64</b> <color={m}>(89%)</color>'.format(g=G_HEX, m=MUTED)
label(risk, 'Legend Healthy', leg, 9.5, 11, (0, 1), (0, 1), (14, -74), (110, 14))
label(risk, 'Legend At Risk', '<color={a}>●</color> At Risk <b>5</b> <color={m}>(7%)</color>'.format(a=A_HEX, m=MUTED), 9.5, 11, (0, 1), (0, 1), (124, -74), (90, 14))
label(risk, 'Legend Critical', '<color={r}>●</color> Critical <b>3</b> <color={m}>(4%)</color>'.format(r=R_HEX, m=MUTED), 9.5, 11, (1, 1), (1, 1), (-14, -74), (90, 14), pivot=(1, 0.5), halign=4)
NEW = (tiles['tr'], risk['tr'])
d.save()
d.new, d.children = [], {}
pe.scan()

# ---------------------------------------------------------------- Overall Integrity counts 64 / 5 / 3
OI = A + '/Cards/Overall Integrity/Gauge and Counts/Counts/'
for nm, n, pct in (('Healthy', '64', '89%'), ('At Risk', '5', '7%'), ('Critical', '3', '4%')):
    pe.text(pe.find(OI + nm + '/Count'), n)
    pe.text(pe.find(OI + nm + '/Name'), '%s <size=10><color=%s>(%s)</color></size>' % (nm, MUTED, pct))

# ---------------------------------------------------------------- Integrity Trend (the Health page's trend card)
TR = pe.find('HELIOS UI/UI/Pages/Health/Cards/Health Trend')
pe.move(TR, CARDS)
pe.rename(TR, 'Integrity Trend')
T = A + '/Cards/Integrity Trend/'
pe.text(pe.find(T + 'Header/Title'), '<b>Integrity Trend</b>  <size=11><color=%s>(30 Days)</color></size>' % MUTED)
pe.rect(pe.find(T + 'Header/Title'), size=(200, 20))
for nm in ('Fill', 'Outline', 'Icon'): pe.active(pe.find(T + 'Header/Overall System/' + nm), False)
pe.text(pe.find(T + 'Header/Overall System/Text'), '<color=%s><b>▲ +3%%</b></color>' % G_HEX)
ch = pe.comp_of(pe.find(T + 'Chart Area/Chart'), 'HeliosChart')
vals = [89.6, 89.7, 90.0, 90.5, 91.1, 91.6, 91.8, 91.7, 91.5, 91.4, 91.4, 91.6, 92.0, 92.4, 92.7, 92.8, 92.8, 92.9, 93.1, 93.4,
        93.7, 94.0, 94.3, 94.5, 94.6, 94.7, 94.8, 94.9, 95.0, 95.1]
b = pe.B(ch)
b = re.sub(r'  min: [\d.]+', '  min: 87', b, count=1); b = re.sub(r'  max: [\d.]+', '  max: 96', b, count=1)
b = re.sub(r'  fillAlpha: [\d.]+', '  fillAlpha: 0.3', b, count=1)
b = re.sub(r'  - name: Overall System\n    color: \{[^}]*\}\n    values:\n(?:    - [-\d.]+\n)+',
           lambda m: '  - name: Integrity\n    color: {r: 0.13333334, g: 0.77254903, b: 0.6, a: 1}\n    values:\n' + ''.join('    - %s\n' % v for v in vals), b, count=1)
assert 'name: Integrity' in b
pe.put(ch, b)
for old, new in (('100%', '96%'), ('95%', '93%'), ('90%', '90%'), ('85%', '87%')):
    r = pe.find(T + 'Chart Area/' + old); pe.text(r, new); pe.rename(r, new + ' ')
# end-point dot at the last value; its label off
mk = pe.find(T + 'Chart Area/Chart/Marker')
y = (vals[-1] - 87) / 9.0
bb = pe.B(mk)
bb = re.sub(r'm_AnchorMin: \{[^}]*\}', 'm_AnchorMin: {x: 1, y: %s}' % f(y), bb, count=1)
bb = re.sub(r'm_AnchorMax: \{[^}]*\}', 'm_AnchorMax: {x: 1, y: %s}' % f(y), bb, count=1)
pe.put(mk, bb)
pe.active(pe.find(T + 'Chart Area/Chart/Marker Label'), False)
# dates: five labels across, the other two off
LB = T + 'Hours/Labels/'
for old, new, x, pv in (('00:00', '15 Oct', 0, 0), ('08:00', '22 Oct', 0.25, 0.5), ('12:00', '29 Oct', 0.5, 0.5), ('16:00', '05 Nov', 0.75, 0.5), ('24:00', '12 Nov', 1, 1)):
    r = pe.find(LB + old); pe.text(r, new)
    bb = pe.B(r)
    bb = re.sub(r'm_AnchorMin: \{[^}]*\}', 'm_AnchorMin: {x: %s, y: 0.5}' % f(x), bb, count=1)
    bb = re.sub(r'm_AnchorMax: \{[^}]*\}', 'm_AnchorMax: {x: %s, y: 0.5}' % f(x), bb, count=1)
    pe.put(r, bb)
for old in ('04:00', '20:00'): pe.active(pe.find(LB + old), False)

# ---------------------------------------------------------------- order; old cards off
pe.active(pe.find(A + '/Cards/Critical Integrity'), False)
pe.active(pe.find(A + '/Cards/Key Findings'), False)
oi = pe.find(A + '/Cards/Overall Integrity')
rest = [c for c in pe.kids[CARDS] if c not in (oi, NEW[0], NEW[1], TR)]
pe.set_children(CARDS, [oi, NEW[0], NEW[1], TR] + rest)
open(P, 'w', encoding='utf-8', newline='').write(d.t)

pe.scan()
ids = re.findall(r'^--- !u!\d+ &(-?\d+)', d.t, re.M)
assert len(ids) == len(set(ids))
for p, cs in pe.kids.items():
    for c in cs: assert pe.father.get(c) == p, (p, c)
for c, p in pe.father.items():
    if p != '0': assert c in pe.kids.get(p, []), (c, p)
print('Asset Integrity page done')
