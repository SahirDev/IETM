"""Sensor & Health, part 2 (UI only, Figma 1021:12072): the Health page's Recent Events card is the last card on the
Sensors page, and the right-hand cards scroll (mouse wheel / drag / a slim scrollbar) since they are taller than the
screen. Cards becomes the Content of a new 'Cards Scroll' view (RectMask2D + ScrollRect + vertical Scrollbar).
Run once from the repo root; it refuses to run twice."""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_yaml import Doc, col, f
import prefab_edit as pe

P = 'Assets/HELIOS/Prefabs/HELIOS UI.prefab'
pe.d = d = Doc(P)
if 'Cards Scroll' in d.t: sys.exit('already done')
pe.scan()
S = 'HELIOS UI/UI/Pages/Sensors'
PAGE = pe.find(S)
CARDS = pe.find(S + '/Cards')

SCROLL, MASK2D, SCROLLBAR = '1aa08ab6e0800fa44ae55d278d1423e3', '3312d7739989d2b4e91e6319e9a96d76', '2a4db7a114972834c8e4117be1d82ba3'
ROUNDED = 'bd47ad8e7de76dd458fc070441db5007'
THEMED = '346a0a1447b7ab8b6ace9c7385c9dfe7'

def image(o, guid, color, sliced=False, raycast=False, ppu=1.0):
    d.canvas_renderer(o)
    sp = '{fileID: 21300000, guid: %s, type: 3}' % guid if guid else '{fileID: 0}'
    return d.mono(o, 'fe87c0e1cc204ed48ad3b37840f39efc', 'UnityEngine.UI::UnityEngine.UI.Image',
        '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: %s\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: %s\n  m_Type: %d\n'
        '  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n'
        '  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: %s\n' % (col(color), f(raycast), sp, 1 if sliced else 0, f(float(ppu))))
def themed(o, role, opacity=1.0):
    d.mono(o, THEMED, 'Assembly-CSharp::Helios.HeliosThemed', '  role: %d\n  opacity: %s\n  useThemeFont: 1\n' % (role, f(opacity)))

# the scroll view takes Cards' place (right column, full height below the top bar); 8 px wider for the scrollbar
cb = pe.B(CARDS)
view = d.go('Cards Scroll', PAGE, anchor_min=(1, 0), anchor_max=(1, 1), pivot=(1, 1), pos=(0, -46), size=(326, -58 + 0))
image(view, None, (0, 0, 0, 0), raycast=True)
d.mono(view, MASK2D, 'UnityEngine.UI::UnityEngine.UI.RectMask2D', '  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')

bar = d.go('Scrollbar', view['tr'], anchor_min=(1, 0), anchor_max=(1, 1), pivot=(1, 0.5), pos=(0, 0), size=(4, -8))
bar_img = image(bar, ROUNDED, (0.16470589, 0.21960784, 0.3764706, 0.35), sliced=True, raycast=True, ppu=12); themed(bar, 4, 0.35)
area = d.go('Sliding Area', bar['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
handle = d.go('Handle', area['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
h_img = image(handle, ROUNDED, (0.4392157, 0.47058824, 1, 0.9), sliced=True, raycast=True, ppu=12); themed(handle, 23)
sb = d.mono(bar, SCROLLBAR, 'UnityEngine.UI::UnityEngine.UI.Scrollbar',
    '  m_Navigation:\n    m_Mode: 0\n    m_WrapAround: 0\n    m_SelectOnUp: {fileID: 0}\n    m_SelectOnDown: {fileID: 0}\n'
    '    m_SelectOnLeft: {fileID: 0}\n    m_SelectOnRight: {fileID: 0}\n  m_Transition: 1\n  m_Colors:\n'
    '    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n    m_HighlightedColor: {r: 1, g: 1, b: 1, a: 1}\n    m_PressedColor: {r: 0.85, g: 0.85, b: 0.85, a: 1}\n'
    '    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}\n    m_DisabledColor: {r: 1, g: 1, b: 1, a: 0.35}\n    m_ColorMultiplier: 1\n    m_FadeDuration: 0.1\n'
    '  m_SpriteState:\n    m_HighlightedSprite: {fileID: 0}\n    m_PressedSprite: {fileID: 0}\n    m_SelectedSprite: {fileID: 0}\n    m_DisabledSprite: {fileID: 0}\n'
    '  m_AnimationTriggers:\n    m_NormalTrigger: Normal\n    m_HighlightedTrigger: Highlighted\n    m_PressedTrigger: Pressed\n'
    '    m_SelectedTrigger: Selected\n    m_DisabledTrigger: Disabled\n  m_Interactable: 1\n  m_TargetGraphic: {fileID: %s}\n'
    '  m_HandleRect: {fileID: %s}\n  m_Direction: 2\n  m_Value: 1\n  m_Size: 0.8\n  m_NumberOfSteps: 0\n'
    '  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n' % (h_img, handle['tr']))
d.mono(view, SCROLL, 'UnityEngine.UI::UnityEngine.UI.ScrollRect',
    '  m_Content: {fileID: %s}\n  m_Horizontal: 0\n  m_Vertical: 1\n  m_MovementType: 2\n  m_Elasticity: 0.1\n  m_Inertia: 1\n'
    '  m_DecelerationRate: 0.12\n  m_ScrollSensitivity: 30\n  m_Viewport: {fileID: %s}\n  m_HorizontalScrollbar: {fileID: 0}\n'
    '  m_VerticalScrollbar: {fileID: %s}\n  m_HorizontalScrollbarVisibility: 0\n  m_VerticalScrollbarVisibility: 1\n'
    '  m_HorizontalScrollbarSpacing: 0\n  m_VerticalScrollbarSpacing: 0\n  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
    % (CARDS, view['tr'], sb))
VIEW_TR, BAR_TR = view['tr'], bar['tr']
d.save()
d.new, d.children = [], {}
pe.scan()

# Cards -> content of the view (top, full width minus the scrollbar), sized by its cards
pe.move(CARDS, VIEW_TR, 0)
b = pe.B(CARDS)
for key, v in (('m_AnchorMin', '{x: 0, y: 1}'), ('m_AnchorMax', '{x: 1, y: 1}'), ('m_AnchoredPosition', '{x: -4, y: 0}'),
               ('m_SizeDelta', '{x: -8, y: 0}'), ('m_Pivot', '{x: 0.5, y: 1}')):
    b = re.sub(r'%s: \{[^}]*\}' % key, '%s: %s' % (key, v), b, count=1)
pe.put(CARDS, b)
# ContentSizeFitter (vertical = preferred) on Cards
g = pe.go_of[CARDS]
cid = d.nid()
gb = pe.B(g)
gb = re.sub(r'(  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)*)', lambda m: m.group(1) + '  - component: {fileID: %s}\n' % cid, gb, count=1)
pe.put(g, gb)
d.t += ('--- !u!114 &%s\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n'
        '  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %s}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
        '  m_Script: {fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}\n  m_Name: \n'
        '  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter\n  m_HorizontalFit: 0\n  m_VerticalFit: 2\n' % (cid, g))
pe.scan()
# Recent Events (Health page) -> last card
EV = pe.find('HELIOS UI/UI/Pages/Health/Cards/Recent Events')
pe.move(EV, CARDS)
open(P, 'w', encoding='utf-8', newline='').write(d.t)

pe.scan()
ids = re.findall(r'^--- !u!\d+ &(-?\d+)', d.t, re.M)
assert len(ids) == len(set(ids))
for p, cs in pe.kids.items():
    for c in cs: assert pe.father.get(c) == p, (p, c)
for c, p in pe.father.items():
    if p != '0': assert c in pe.kids.get(p, []), (c, p)
print('scrolling cards + Recent Events done')
