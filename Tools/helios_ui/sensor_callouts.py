"""Sensor & Health: clicking a reading flies to its sensor and shows its callout (as in Figma).
- The seven reading rows are clickable again: their Fill (the Button's raycast target) is back on, invisible (alpha 0).
- Every sensor has its own callout on the model in the prefab (no copies made at Play): the three existing ones
  re-texted (title + value + status) and four new ones (Engine RPM, Oil Pressure, Fuel Flow, Hydraulic), copies of the
  Vibration callout with their own point, icon and texts.
Run once from the repo root."""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc, f
import prefab_edit as pe
import ui_kit as ui

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
pe.d = ui.d = d = Doc(P)
if 'Label Engine RPM' in d.t: sys.exit('already done')
pe.scan()
S = 'HELIOS UI/UI/Pages/Sensors'
IC = lambda k: open('Assets/HELIOS/Art/Icons/%s.png.meta' % k).read().split('guid: ')[1].split()[0]
G, A = '#22C55E', '#F59E0B'
def status(v, word='Normal', c=G): return '<b>%s</b>   <size=9><color=%s>● %s</color></size>' % (v, c, word)

# ---------------------------------------------------------------- rows clickable
for r in pe.kids[pe.find(S + '/Cards Scroll/Cards/Sensor Readings')]:
    for k in pe.kids.get(r, []):
        if pe.name.get(pe.go_of[k]) != 'Fill': continue
        pe.active(k, True)
        img = pe.comp_of(k, 'UnityEngine.UI.Image')
        pe.put(img, re.sub(r'm_Color: \{[^}]*\}', 'm_Color: {r: 1, g: 1, b: 1, a: 0}', pe.B(img), count=1))
        th = pe.comp_of(k, 'HeliosThemed')
        if th: pe.put(th, re.sub(r'opacity: [\d.]+', 'opacity: 0', pe.B(th), count=1))

# ---------------------------------------------------------------- callouts
L = S + '/Labels on Model/'
def set_label(rt, title, sub, icon=None, point=None, offset=None):
    box = [k for k in pe.kids[rt] if pe.name[pe.go_of[k]] == 'Box'][0]
    for k in pe.kids[box]:
        nm = pe.name[pe.go_of[k]]
        if nm == 'Title': pe.text(k, title)
        elif nm == 'Subtitle': pe.text(k, sub); pe.rect(k, size=(150, 18))
        elif nm == 'Icon' and icon:
            img = pe.comp_of(k, 'UnityEngine.UI.Image')
            pe.put(img, re.sub(r'm_Sprite: \{[^}]*\}', 'm_Sprite: {fileID: 21300000, guid: %s, type: 3}' % IC(icon), pe.B(img), count=1))
    pe.rect(box, size=(212, 54))
    c = pe.comp_of(rt, 'HeliosModelLabel'); b = pe.B(c)
    if point is not None: b = re.sub(r'\n  point: \d+', '\n  point: %d' % point, b, count=1)
    if offset is not None:
        b = re.sub(r'boxOffset: \{[^}]*\}', 'boxOffset: {x: %s, y: %s}' % (f(offset[0]), f(offset[1])), b, count=1)
        pe.rect(box, pos=offset)
    pe.put(c, b)

set_label(pe.find(L + 'Label Environment'), 'Humidity', status('42 %RH'), 'cloud')
set_label(pe.find(L + 'Label Strain Gauging'), 'Structural Strain', status('0.28%', 'Monitor', A), 'layers')
set_label(pe.find(L + 'Label Vibration'), 'Vibration', status('1.8 mm/s'), 'vibration')
parent = pe.find(S + '/Labels on Model')
vib = pe.find(L + 'Label Vibration')
for name, title, sub, icon, point, off in (
        ('Label Engine RPM', 'Engine RPM', status('8,920 RPM'), 'fan', 3, (-150, -95)),
        ('Label Oil Pressure', 'Oil Pressure', status('52.8 psi'), 'drop', 4, (150, -95)),
        ('Label Fuel Flow', 'Fuel Flow', status('780 lb/hr'), 'gauge', 5, (-160, 90)),
        ('Label Hydraulic', 'Hydraulic', status('3,120 PSI'), 'hydraulic', 6, (170, 80))):
    rt = ui.clone(vib, parent, None, name)
    set_label(rt, title, sub, icon, point, off)
open(P, 'w', encoding='utf-8', newline='').write(d.t)
ui.check()
print('sensor callouts done')
