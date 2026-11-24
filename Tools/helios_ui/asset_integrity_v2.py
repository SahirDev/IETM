"""Asset Integrity, client revision (UI only; other pages untouched). Right column, scrolling:
Integrity Overview (gauge 93 %, 12 Healthy / 2 At Risk / 1 Critical), Critical Integrity (engine tile, Engine (Borescope),
Critical Condition, vibration +18 %), Key Findings (3 rows with values), Degradation Trend (7 days, 3 lines, D1-D7,
legend), Recommended Actions (3 numbered). Callouts on the model: AVIONICS BAY / WING ROOT / ENGINE with values and status.
Switched off (not deleted): Integrity Status bar, Integrity Metrics tiles, Component Risk. Run once from the repo root."""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc, f
import prefab_edit as pe
import ui_kit as ui
from ui_kit import C, GREEN, AMBER, RED, MUTED

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
pe.d = ui.d = d = Doc(P)
if 'Degradation Trend' in d.t: sys.exit('already done')
pe.scan()
A = 'HELIOS UI/UI/Pages/Asset Integrity'
PAGE, CARDS = pe.find(A), pe.find(A + '/Cards')
ui.TMP_TEMPLATE = pe.B(pe.comp_of(pe.find(A + '/Header/Title'), 'TextMeshProUGUI'))
IC = lambda k: open('Assets/HELIOS/Art/Icons/%s.png.meta' % k).read().split('guid: ')[1].split()[0]
R_HEX, A_HEX, G_HEX = '#EF4444', '#F59E0B', '#22C55E'
def all_tmp(rt):
    out = []
    stack = [rt]
    while stack:
        r = stack.pop()
        if pe.comp_of(r, 'TextMeshProUGUI'): out.append(r)
        stack += pe.kids.get(r, [])
    return out

# ---------------------------------------------------------------- phase 1: new objects
OV = pe.find(A + '/Cards/Overall Integrity')
ovh = d.go('Header', OV, anchor_min=(0, 1), anchor_max=(0, 1), size=(290, 20))
d.layout_element(ovh, h=20, mh=20)
ui.icon(ovh, IC('shield'), (0.65882355, 0.33333334, 0.96862745, 1), 20, (0, 0.5), (8, 0), 16)
ui.label(ovh, 'Title', '<b>Integrity Overview</b>', 13, 10, (0, 0.5), (0, 0.5), (24, 0), (220, 20))
ui.icon(ovh, IC('info'), C[11], 11, (1, 0.5), (-8, 0), 14)

# Degradation Trend legend + Recommended Actions card
leg = None
recs = d.go('Recommended Actions', CARDS, anchor_min=(0, 1), anchor_max=(0, 1), size=(318, 176))
ui.card_bg(recs); d.layout_element(recs, h=176, mh=176)
ui.icon(recs, IC('target') if os.path.exists('Assets/HELIOS/Art/Icons/target.png.meta') else IC('checkcircle'),
        (0.65882355, 0.33333334, 0.96862745, 1), 20, (0, 1), (22, -24), 15)
ui.label(recs, 'Title', '<b>Recommended Actions</b>', 13, 10, (0, 1), (0, 1), (36, -24), (250, 20))
for i, (txt, colr) in enumerate((('Inspect engine (borescope) and validate vibration source.', RED),
                                 ('Review wing-root strain data and schedule detailed inspection.', AMBER),
                                 ('Continue monitoring avionics-bay environmental conditions.', GREEN))):
    y = -58 - i * 42
    row = d.go('Action %d' % (i + 1), recs['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 0.5), pos=(0, y), size=(-28, 38))
    badge = d.go('Badge', row['tr'], anchor_min=(0, 0.5), anchor_max=(0, 0.5), pivot=(0, 0.5), pos=(0, 0), size=(22, 22))
    ui.image(badge, ui.ROUNDED, (colr[0], colr[1], colr[2], 0.85), sliced=True, ppu=4.0)
    ui.label(badge, 'Number', '<b>%d</b>' % (i + 1), 11, 10, (0, 0), (1, 1), (0, 0), (0, 0), pivot=(0.5, 0.5), halign=2)
    ui.label(row, 'Text', txt, 10, 10, (0, 0), (1, 1), (17, 0), (-34, 0), pivot=(0.5, 0.5), wrap=True)
REC = recs['tr']
d.save(); d.new, d.children = [], {}
pe.scan()

# ---------------------------------------------------------------- Integrity Overview
pe.set_children(OV, [k for k in pe.kids[OV] if pe.name[pe.go_of[k]] in ('Shadow', 'Fill', 'Outline')] + [ovh['tr']] +
                [k for k in pe.kids[OV] if pe.name[pe.go_of[k]] not in ('Shadow', 'Fill', 'Outline') and k != ovh['tr']])
C_ = A + '/Cards/Overall Integrity/Gauge and Counts/Counts/'
for nm, n in (('Healthy', '12'), ('At Risk', '2'), ('Critical', '1')):
    pe.text(pe.find(C_ + nm + '/Count'), n)
    pe.text(pe.find(C_ + nm + '/Name'), nm)

# ---------------------------------------------------------------- Critical Integrity (on, engine tile on the left)
CR = pe.find(A + '/Cards/Critical Integrity')
pe.active(CR, True)
tile = ui.clone(pe.find('HELIOS UI/UI/Pages/Maintenance/Cards Scroll/Cards/Overdue Maintenance/Tile'), CR, None, 'Engine Tile')
ui.add_component(pe.go_of[tile], ui.LEG, 'UnityEngine.UI::UnityEngine.UI.LayoutElement',
                 '  m_IgnoreLayout: 1\n  m_MinWidth: -1\n  m_MinHeight: -1\n  m_PreferredWidth: -1\n  m_PreferredHeight: -1\n'
                 '  m_FlexibleWidth: -1\n  m_FlexibleHeight: -1\n  m_LayoutPriority: 1\n')
pe.rect(tile, pos=(14, -44), size=(70, 70))
for nm, h, s in (('Subject', 22, '<b>Engine (Borescope)</b>'), ('Status', 18, '<color=%s>● Critical Condition</color>' % R_HEX),
                 ('Detail', 40, '<color=%s>↑</color> Vibration trend increased by 18%% in the last 24 hours.' % R_HEX)):
    r = pe.find(A + '/Cards/Critical Integrity/' + nm)
    pe.le_height(r, h)
    t = pe.kids[r][0]
    pe.text(t, s)
    b = pe.B(t)
    b = re.sub(r'm_AnchoredPosition: \{[^}]*\}', 'm_AnchoredPosition: {x: 42, y: 0}', b, count=1)
    b = re.sub(r'm_SizeDelta: \{[^}]*\}', 'm_SizeDelta: {x: -84, y: 0}', b, count=1)
    pe.put(t, b)
    tc = pe.comp_of(t, 'TextMeshProUGUI'); tb = pe.B(tc)
    tb = re.sub(r'\n  m_enableWordWrapping: \d', '\n  m_enableWordWrapping: 1', tb, count=1)
    tb = re.sub(r'\n  m_TextWrappingMode: \d', '\n  m_TextWrappingMode: 1', tb, count=1)
    tb = re.sub(r'\n  m_isRichText: \d', '\n  m_isRichText: 1', tb, count=1)
    if nm == 'Subject': tb = re.sub(r'\n  m_fontSize: [\d.]+', '\n  m_fontSize: 13', tb, count=1)
    pe.put(tc, tb)

# ---------------------------------------------------------------- Key Findings (on, value on the right)
KF = A + '/Cards/Key Findings/'
pe.active(pe.find(A + '/Cards/Key Findings'), True)
pe.text(pe.find(KF + 'Header/Title'), '<b>Key Findings</b>')
pe.active(pe.find(KF + 'Header/View All'), False)
for k in pe.kids[pe.find(A + '/Cards/Key Findings')]:
    nm = pe.name[pe.go_of[k]]
    val = {'"Elevated vibration \\u2013 Engine"': ('+18%', R_HEX), '"Strain variation \\u2013 Wing Root"': ('+10%', A_HEX),
           '"Humidity \\u2013 Avionics Bay"': ('62% RH', G_HEX)}.get(nm)
    if not val: continue
    pe.le_height(k, 32)
    for c in pe.kids[k]:
        cn = pe.name[pe.go_of[c]]
        if cn == 'Detail': pe.active(c, False)
        elif cn == 'Title': pe.rect(c, pos=(40, -7), size=(190, 18))
        elif cn == 'Tile': pe.rect(c, size=(24, 24))
        elif cn == 'Trend':
            for t in all_tmp(c): pe.text(t, '<color=%s><b>%s</b></color>' % val)
            for g in pe.kids[c]:
                if pe.name[pe.go_of[g]] in ('Fill', 'Outline', 'Icon'): pe.active(g, False)

# ---------------------------------------------------------------- Degradation Trend (the trend card, 3 lines)
TR = pe.find(A + '/Cards/Integrity Trend')
pe.rename(TR, 'Degradation Trend')
T = A + '/Cards/Degradation Trend/'
pe.text(pe.find(T + 'Header/Title'), '<b>Degradation Trend</b>  <size=11><color=%s>(7 Days)</color></size>' % MUTED)
pe.active(pe.find(T + 'Header/Overall System'), False)
ch = pe.comp_of(pe.find(T + 'Chart Area/Chart'), 'HeliosChart')
series = (('Engine Vibration', '{r: 0.9372549, g: 0.26666668, b: 0.26666668, a: 1}', [8.0, 9.0, 10.5, 11.2, 12.4, 14.4, 17.8]),
          ('Wing Root Strain', '{r: 0.9607843, g: 0.61960787, b: 0.043137256, a: 1}', [2.6, 3.0, 4.8, 6.4, 7.4, 8.8, 10.0]),
          ('Avionics Humidity', '{r: 0.13333334, g: 0.77254903, b: 0.36862746, a: 1}', [1.2, 1.2, 1.2, 1.2, 1.2, 1.2, 1.2]))
b = pe.B(ch)
b = re.sub(r'  min: [-\d.]+', '  min: 0', b, count=1); b = re.sub(r'  max: [-\d.]+', '  max: 20', b, count=1)
b = re.sub(r'  fillAlpha: [\d.]+', '  fillAlpha: 0', b, count=1)
b = re.sub(r'  series:\n(?:  - name: .*\n    color: \{[^}]*\}\n    values:\n(?:    - [-\d.]+\n)+)+',
           lambda m: '  series:\n' + ''.join('  - name: %s\n    color: %s\n    values:\n%s' % (n, c, ''.join('    - %s\n' % v for v in vs)) for n, c, vs in series), b, count=1)
assert 'Avionics Humidity' in b
pe.put(ch, b)
pe.active(pe.find(T + 'Chart Area/Chart/Marker'), False)
# y axis +20 .. 0 (five labels: the 4th is a copy)
CA = pe.find(T + 'Chart Area')
ylab = [pe.find(T + 'Chart Area/' + n) for n in ('96% ', '93% ', '90% ', '87% ')]
ylab.append(ui.clone(ylab[3], CA, None, 'y extra'))
for r, (txt, y) in zip(ylab, (('+20%', 1), ('+15%', 0.75), ('+10%', 0.5), ('+5%', 0.25), ('0%', 0))):
    pe.text(r, txt); pe.rename(r, 'Y ' + txt)
    bb = pe.B(r)
    bb = re.sub(r'm_AnchorMin: \{[^}]*\}', 'm_AnchorMin: {x: 0, y: %s}' % f(y), bb, count=1)
    bb = re.sub(r'm_AnchorMax: \{[^}]*\}', 'm_AnchorMax: {x: 0, y: %s}' % f(y), bb, count=1)
    pe.put(r, bb)
pe.le_height(CA, 100); pe.rect(CA, size=(290, 100))
# x axis D1 .. D7
LB = pe.find(T + 'Hours/Labels')
for i, r in enumerate(pe.kids[LB]):
    pe.active(r, True); pe.text(r, 'D%d' % (i + 1))
    bb = pe.B(r); x = i / 6.0
    bb = re.sub(r'm_AnchorMin: \{[^}]*\}', 'm_AnchorMin: {x: %s, y: 0.5}' % f(x), bb, count=1)
    bb = re.sub(r'm_AnchorMax: \{[^}]*\}', 'm_AnchorMax: {x: %s, y: 0.5}' % f(x), bb, count=1)
    bb = re.sub(r'm_Pivot: \{[^}]*\}', 'm_Pivot: {x: 0.5, y: 0.5}', bb, count=1)
    pe.put(r, bb)
assert len(pe.kids[LB]) == 7
# legend row: a copy of the Hours row with one label
leg = ui.clone(pe.find(T + 'Hours'), TR, None, 'Legend')
lab = pe.find(T + 'Legend/Labels')
first = pe.kids[lab][0]
pe.text(first, '<color=%s>●</color> Engine Vibration   <color=%s>●</color> Wing Root Strain   <color=%s>●</color> Avionics Humidity' % (R_HEX, A_HEX, G_HEX))
bb = pe.B(first)
bb = re.sub(r'm_AnchorMin: \{[^}]*\}', 'm_AnchorMin: {x: 0, y: 0.5}', bb, count=1)
bb = re.sub(r'm_AnchorMax: \{[^}]*\}', 'm_AnchorMax: {x: 0, y: 0.5}', bb, count=1)
bb = re.sub(r'm_Pivot: \{[^}]*\}', 'm_Pivot: {x: 0, y: 0.5}', bb, count=1)
bb = re.sub(r'm_SizeDelta: \{[^}]*\}', 'm_SizeDelta: {x: 290, y: 14}', bb, count=1)
bb = re.sub(r'm_AnchoredPosition: \{[^}]*\}', 'm_AnchoredPosition: {x: -16, y: 0}', bb, count=1)
pe.put(first, bb)
for r in pe.kids[lab][1:]: pe.active(r, False)

# ---------------------------------------------------------------- callouts on the model
L = A + '/Labels on Model/'
for lbl, title, sub, icon in (('Label Environment', '<b>AVIONICS BAY</b>', 'Humidity <b>62%% RH</b>  <size=9><color=%s>● Stable</color></size>' % G_HEX, 'drop'),
                              ('Label Strain Gauging', '<b>WING ROOT</b>', 'Strain <b>+10%%</b>  <size=9><color=%s>● 7 days</color></size>' % A_HEX, 'layers'),
                              ('Label Vibration', '<b>ENGINE</b>', 'Vibration <b><color=%s>+18%%</color></b>  <size=9><color=%s>● Critical</color></size>' % (R_HEX, R_HEX), 'fan')):
    box = pe.find(L + lbl + '/Box')
    for k in pe.kids[box]:
        nm = pe.name[pe.go_of[k]]
        if nm == 'Title': pe.text(k, title)
        elif nm == 'Subtitle': pe.text(k, sub); pe.rect(k, size=(165, 18))
        elif nm == 'Ring': pe.active(k, False)
        elif nm == 'Icon':
            img = pe.comp_of(k, 'UnityEngine.UI.Image')
            pe.put(img, re.sub(r'm_Sprite: \{[^}]*\}', 'm_Sprite: {fileID: 21300000, guid: %s, type: 3}' % IC(icon), pe.B(img), count=1))

# ---------------------------------------------------------------- off, order, scroll
for n in ('Integrity Metrics', 'Component Risk'): pe.active(pe.find(A + '/Cards/' + n), False)
pe.active(pe.find(A + '/Integrity Status'), False)
order = [OV, CR, pe.find(A + '/Cards/Key Findings'), TR, REC]
pe.set_children(CARDS, order + [c for c in pe.kids[CARDS] if c not in order])
open(P, 'w', encoding='utf-8', newline='').write(d.t)
pe.scan()
d.new, d.children = [], {}
view = ui.scroll_view(PAGE, CARDS)
d.save(); d.new, d.children = [], {}
pe.scan()
ui.finish_scroll(view, CARDS)
open(P, 'w', encoding='utf-8', newline='').write(d.t)
ui.check()
print('Asset Integrity v2 done')
