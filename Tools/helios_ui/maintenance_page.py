"""Maintenance page, right column as in Figma (UI only): Maintenance Overview as four status tiles (12 Completed,
6 In Progress, 4 Due (30d), 2 Overdue), Next Major Service (A-Check), Overdue Maintenance (4 days overdue), Upcoming
Maintenance (3, work orders), Recommended Actions, Maintenance History (6 months, planned vs completed bars).
The column scrolls (Cards Scroll). The old gauge is switched off (not deleted). Run once from the repo root."""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc
import prefab_edit as pe
import ui_kit as ui
from ui_kit import C, GREEN, AMBER, RED, CYAN, PURPLE, MUTED

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
pe.d = ui.d = d = Doc(P)
if 'Next Major Service' in d.t: sys.exit('already done')
pe.scan()
M = 'HELIOS UI/UI/Pages/Maintenance'
PAGE, CARDS = pe.find(M), pe.find(M + '/Cards')
ui.TMP_TEMPLATE = pe.B(pe.comp_of(pe.find(M + '/Header/Title'), 'TextMeshProUGUI'))
IC = {k: open('Assets/HELIOS/Art/Icons/%s.png.meta' % k).read().split('guid: ')[1].split()[0]
      for k in ('checkcircle', 'refresh', 'alert', 'xcircle', 'calendar', 'barchart')}
OV = pe.find(M + '/Cards/Maintenance Overview')

# ---------------------------------------------------------------- overview: four status tiles
tiles = d.go('Status Tiles', OV, anchor_min=(0, 1), anchor_max=(0, 1), size=(290, 78))
d.layout_group(tiles, False, 6, (0, 0, 0, 0), 0, True, True)
d.layout_element(tiles, h=78, mh=78)
for nm, n, icon, colr in (('Completed', '12', 'checkcircle', GREEN), ('In Progress', '6', 'refresh', CYAN),
                          ('Due (30d)', '4', 'alert', AMBER), ('Overdue', '2', 'xcircle', RED)):
    t = d.go(nm, tiles['tr'], anchor_min=(0, 1), anchor_max=(0, 1), size=(68, 78))
    ui.card_bg(t, 5, 4, 3.0)
    ui.icon(t, IC[icon], colr, 10, (0.5, 1), (0, -17), 17)
    ui.label(t, 'Count', n, 19, 10, (0, 1), (1, 1), (0, -40), (0, 24), pivot=(0.5, 0.5), bold=True, halign=2)
    ui.label(t, 'Name', nm, 9, 11, (0, 1), (1, 1), (0, -61), (0, 14), pivot=(0.5, 0.5), halign=2)

# ---------------------------------------------------------------- Next Major Service
nms = d.go('Next Major Service', CARDS, anchor_min=(0, 1), anchor_max=(0, 1), size=(318, 96))
ui.card_bg(nms); d.layout_element(nms, h=96, mh=96)
ui.icon(nms, IC['calendar'], PURPLE, 20, (0, 1), (22, -24), 14)
ui.label(nms, 'Title', '<b>Next Major Service</b>', 11.5, 10, (0, 1), (0, 1), (36, -24), (170, 20))
ui.pill(nms, 'Due In', 'In 120 days', PURPLE, (1, 1), (1, 1), (-14, -24), (84, 22), pivot=(1, 0.5), alpha=0.35)
ui.label(nms, 'Check', '<b>A-Check</b>', 20, 10, (0, 1), (0, 1), (14, -52), (200, 26))
ui.label(nms, 'Details', '<color={m}>Due:</color> 15 Mar 2027   <color={m}>|   Est. Duration:</color> 12 days'.format(m=MUTED),
         10, 10, (0, 1), (0, 1), (14, -78), (280, 16))

# ---------------------------------------------------------------- Maintenance History (bars)
hist = d.go('Maintenance History', CARDS, anchor_min=(0, 1), anchor_max=(0, 1), size=(318, 206))
ui.card_bg(hist); d.layout_element(hist, h=206, mh=206)
ui.icon(hist, IC['barchart'], PURPLE, 20, (0, 1), (22, -24), 15)
ui.label(hist, 'Title', '<b>Maintenance History</b> <size=11><color=%s>(6 Months)</color></size>' % MUTED, 13, 10, (0, 1), (0, 1), (36, -24), (250, 20))
ui.label(hist, 'Legend', '<color=#A855F7>●</color> Planned      <color=#22C55E>●</color> Completed', 10, 11, (0, 1), (0, 1), (14, -48), (250, 14))
plot = d.go('Chart', hist['tr'], anchor_min=(0, 1), anchor_max=(1, 1), pivot=(0.5, 1), pos=(10, -66), size=(-48, 104))
TOP = 12.0
for i, v in enumerate((0, 4, 8, 12)):
    y = v / TOP
    gl = d.go('Grid %d' % v, plot['tr'], anchor_min=(0, y), anchor_max=(1, y), size=(0, 1))
    ui.image(gl, None, (1, 1, 1, 0.07))
    ui.label(plot, 'Y %d' % v, str(v), 8.5, 11, (0, y), (0, y), (-8, 0), (20, 12), pivot=(1, 0.5), halign=4)
for i, (mon, pl, co) in enumerate((('Jun', 7, 5), ('Jul', 8, 6), ('Aug', 6, 5), ('Sep', 10, 8), ('Oct', 8, 7), ('Nov', 7, 6))):
    x = (i + 0.5) / 6.0
    grp = d.go(mon, plot['tr'], anchor_min=(x, 0), anchor_max=(x, 1), pivot=(0.5, 0), size=(30, 0))
    for nm, val, colr, bx in (('Planned', pl, PURPLE, -7.5), ('Completed', co, GREEN, 7.5)):
        h = val / TOP
        b = d.go(nm, grp['tr'], anchor_min=(0.5, 0), anchor_max=(0.5, h), pivot=(0.5, 0), pos=(bx, 0), size=(12, 0))
        ui.image(b, ui.ROUNDED, colr, sliced=True, ppu=8)
        ui.label(grp, nm + ' Value', str(val), 8, 11, (0.5, h), (0.5, h), (bx, 3), (20, 11), pivot=(0.5, 0), halign=2)
    ui.label(grp, 'Month', mon, 9, 11, (0.5, 0), (0.5, 0), (0, -6), (40, 12), pivot=(0.5, 1), halign=2)

VIEW = ui.scroll_view(PAGE, CARDS)
NEW = (nms['tr'], hist['tr'], tiles['tr'])
d.save()
d.new, d.children = [], {}
pe.scan()

# ---------------------------------------------------------------- texts
pe.active(pe.find(M + '/Cards/Maintenance Overview/Gauge and Counts'), False)
pe.text(pe.find(M + '/Cards/Overdue Maintenance/Overdue Button/Text'), '4 days overdue  ›')
U = M + '/Cards/Upcoming Maintenance/'
pe.text(pe.find(U + 'Header/Title'), 'Upcoming Maintenance <color=%s>(3)</color>' % MUTED)
def item(rt, title, wo, date, due):
    for k in pe.kids[rt]:
        nm = pe.name.get(pe.go_of[k])
        if nm == 'Title': pe.text(k, title)
        elif nm == 'Date': pe.text(k, '%s   |   %s' % (wo, date)); pe.rect(k, pos=(34, -24), size=(190, 14))
        elif nm == 'Icon' and '{x: 40, y: -31}' in pe.B(k): pe.active(k, False)          # the small calendar: not in Figma
        elif nm == 'Due':
            for t in pe.kids[k]:
                if pe.name.get(pe.go_of[t]) == 'Text': pe.text(t, due)
UP = pe.find(M + '/Cards/Upcoming Maintenance')
eng, wing = pe.find(U + 'Engine'), pe.find(U + 'Wing Structure')
div = pe.find(U + 'Divider')
item(eng, 'Engine Borescope Inspection', 'WO-8842', '15 Nov 2026', 'In 10 days')
item(wing, 'Wing Structure (NDT)', 'WO-8871', '20 Nov 2026', 'In 15 days')
idx = pe.kids[UP].index(wing) + 1
ui.clone(div, UP, idx, 'Divider')
gear = ui.clone(wing, UP, idx + 1, 'Landing Gear')
item(gear, 'Landing Gear Inspection', 'WO-8890', '28 Nov 2026', 'In 23 days')

# ---------------------------------------------------------------- order + scroll
tiles_rt = NEW[2]
hdr = pe.find(M + '/Cards/Maintenance Overview/Header')
ov_kids = [k for k in pe.kids[OV] if k != tiles_rt]
ov_kids.insert(ov_kids.index(hdr) + 1, tiles_rt)
pe.set_children(OV, ov_kids)
order = [pe.find(M + '/Cards/' + n) for n in ('Maintenance Overview',)] + [NEW[0]] + \
        [pe.find(M + '/Cards/' + n) for n in ('Overdue Maintenance', 'Upcoming Maintenance', 'Recommended Actions')] + [NEW[1]]
pe.set_children(CARDS, order + [c for c in pe.kids[CARDS] if c not in order])
ui.finish_scroll(VIEW, CARDS)
open(P, 'w', encoding='utf-8', newline='').write(d.t)
ui.check()
print('Maintenance page done')
