"""Building blocks for HELIOS UI.prefab edits (HELIOS theme roles, TMP with the HELIOS font, rounded cards, scroll views,
subtree copies). Use with prefab_edit: ui_kit.d = pe.d = Doc(path); pe.scan(); ui_kit.TMP_TEMPLATE = <a TMP doc>."""
import re
from unity_yaml import col, f
import prefab_edit as pe

d = None
TMP_TEMPLATE = None
ROUNDED, ROUNDED_LINE = 'bd47ad8e7de76dd458fc070441db5007', '5e10806c54123544cb7cf367a35f1b02'
THEMED, LEG = '346a0a1447b7ab8b6ace9c7385c9dfe7', '306cc8c2b49d7114eaa3623786fc2126'
SCROLL, MASK2D, SCROLLBAR, CSF = '1aa08ab6e0800fa44ae55d278d1423e3', '3312d7739989d2b4e91e6319e9a96d76', '2a4db7a114972834c8e4117be1d82ba3', '3245ec927659c4140ac4f8d17403cc18'
C = {3: (0.043137256, 0.07450981, 0.15686275, 0.9), 4: (0.16470589, 0.21960784, 0.3764706, 1), 5: (0.078431375, 0.12156863, 0.23529412, 1),
     10: (0.9254902, 0.94509804, 0.98039216, 1), 11: (0.59607846, 0.64705884, 0.7607843, 1), 20: (0.4392157, 0.47058824, 1, 1),
     23: (0.4392157, 0.47058824, 1, 0.9019608)}
GREEN, AMBER, RED, CYAN, PURPLE = (0.13333334, 0.77254903, 0.36862746, 1), (0.9607843, 0.61960787, 0.043137256, 1), \
    (0.9372549, 0.26666668, 0.26666668, 1), (0.13333334, 0.827451, 0.93333334, 1), (0.65882355, 0.33333334, 0.96862745, 1)
G_HEX, A_HEX, R_HEX, MUTED = '#22C55E', '#F59E0B', '#EF4444', '#98A5C2'

def image(o, guid, color, sliced=False, ppu=1.0, preserve=False, raycast=False):
    d.canvas_renderer(o)
    sp = '{fileID: 21300000, guid: %s, type: 3}' % guid if guid else '{fileID: 0}'
    return d.mono(o, 'fe87c0e1cc204ed48ad3b37840f39efc', 'UnityEngine.UI::UnityEngine.UI.Image',
        '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: %s\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n  m_Maskable: 1\n'
        '  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: %s\n  m_Type: %d\n'
        '  m_PreserveAspect: %s\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n'
        '  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: %s\n' % (col(color), f(raycast), sp, 1 if sliced else 0, f(preserve), f(float(ppu))))
def themed(o, role, opacity=1.0):
    d.mono(o, THEMED, 'Assembly-CSharp::Helios.HeliosThemed', '  role: %d\n  opacity: %s\n  useThemeFont: 1\n' % (role, f(opacity)))
def ignore_layout(o):
    d.mono(o, LEG, 'UnityEngine.UI::UnityEngine.UI.LayoutElement', '  m_IgnoreLayout: 1\n  m_MinWidth: -1\n  m_MinHeight: -1\n'
           '  m_PreferredWidth: -1\n  m_PreferredHeight: -1\n  m_FlexibleWidth: -1\n  m_FlexibleHeight: -1\n  m_LayoutPriority: 1\n')
def tmp(o, s, size, role, bold=False, halign=1, valign=512, wrap=False):
    d.canvas_renderer(o)
    cid = d.nid(); o['comps'].append(cid)
    b = re.sub(r'^--- !u!114 &-?\d+', '--- !u!114 &' + cid, TMP_TEMPLATE)
    b = re.sub(r'm_GameObject: \{fileID: -?\d+\}', 'm_GameObject: {fileID: %s}' % o['go'], b, count=1)
    rep = {r'\n  m_text: .*': '\n  m_text: ' + s, r'\n  m_fontSize: [\d.]+': '\n  m_fontSize: %s' % f(float(size)),
           r'\n  m_fontSizeBase: [\d.]+': '\n  m_fontSizeBase: %s' % f(float(size)), r'\n  m_fontStyle: \d+': '\n  m_fontStyle: %d' % (1 if bold else 0),
           r'\n  m_HorizontalAlignment: \d+': '\n  m_HorizontalAlignment: %d' % halign, r'\n  m_VerticalAlignment: \d+': '\n  m_VerticalAlignment: %d' % valign,
           r'\n  m_overflowMode: \d+': '\n  m_overflowMode: 0', r'\n  m_enableWordWrapping: \d': '\n  m_enableWordWrapping: %d' % (1 if wrap else 0),
           r'\n  m_TextWrappingMode: \d': '\n  m_TextWrappingMode: %d' % (1 if wrap else 0), r'\n  m_isRichText: \d': '\n  m_isRichText: 1'}
    for k_, v in rep.items(): b = re.sub(k_, lambda m: v, b, count=1)
    o['docs'].append(b if b.endswith('\n') else b + '\n')
    themed(o, role)
def card_bg(o, fill=3, line=4, ppu=2.4):
    for nm, guid, role in (('Fill', ROUNDED, fill), ('Outline', ROUNDED_LINE, line)):
        x = d.go(nm, o['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
        image(x, guid, C[role], sliced=True, ppu=ppu); themed(x, role); ignore_layout(x)
def pill(parent, nm, s, color, am, ax, pos, size, pivot=(0.5, 0.5), text_role=10, alpha=0.25):
    o = d.go(nm, parent['tr'], anchor_min=am, anchor_max=ax, pivot=pivot, pos=pos, size=size)
    image(o, ROUNDED, (color[0], color[1], color[2], alpha), sliced=True, ppu=24.0 / (size[1] / 2.0))
    t = d.go('Text', o['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
    tmp(t, s, 10, text_role, True, 2)
    return o
def label(parent, nm, s, size, role, am, ax, pos, sz, pivot=(0, 0.5), bold=False, halign=1, wrap=False):
    o = d.go(nm, parent['tr'] if isinstance(parent, dict) else parent, anchor_min=am, anchor_max=ax, pivot=pivot, pos=pos, size=sz)
    tmp(o, s, size, role, bold, halign, wrap=wrap); return o
def icon(parent, key_guid, color, role, am, pos, size):
    o = d.go('Icon', parent['tr'] if isinstance(parent, dict) else parent, anchor_min=am, anchor_max=am, pos=pos, size=(size, size))
    image(o, key_guid, color, preserve=True); themed(o, role); return o

def add_component(go, cls_guid, cls_name, body):
    """A MonoBehaviour added to an existing GameObject (raw text)."""
    cid = d.nid()
    gb = pe.B(go)
    gb = re.sub(r'(  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)*)', lambda m: m.group(1) + '  - component: {fileID: %s}\n' % cid, gb, count=1)
    pe.put(go, gb)
    d.t += ('--- !u!114 &%s\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n'
            '  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %s}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
            '  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: %s\n%s' % (cid, go, cls_guid, cls_name, body))
    pe.scan()
    return cid

def scroll_view(page_rt, cards_rt):
    """Phase A (Doc): a 'Cards Scroll' view in Cards' place with a slim scrollbar; returns its rt. Then call
    finish_scroll(view_rt, cards_rt) after d.save()."""
    view = d.go('Cards Scroll', page_rt, anchor_min=(1, 0), anchor_max=(1, 1), pivot=(1, 1), pos=(0, -46), size=(326, -58))
    image(view, None, (0, 0, 0, 0), raycast=True)
    d.mono(view, MASK2D, 'UnityEngine.UI::UnityEngine.UI.RectMask2D', '  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')
    bar = d.go('Scrollbar', view['tr'], anchor_min=(1, 0), anchor_max=(1, 1), pivot=(1, 0.5), pos=(0, 0), size=(4, -8))
    image(bar, ROUNDED, (0.16470589, 0.21960784, 0.3764706, 0.35), sliced=True, raycast=True, ppu=12); themed(bar, 4, 0.35)
    area = d.go('Sliding Area', bar['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
    handle = d.go('Handle', area['tr'], anchor_min=(0, 0), anchor_max=(1, 1), size=(0, 0))
    h_img = image(handle, ROUNDED, C[23], sliced=True, raycast=True, ppu=12); themed(handle, 23)
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
        % (cards_rt, view['tr'], sb))
    return view['tr']

def finish_scroll(view_rt, cards_rt):
    pe.move(cards_rt, view_rt, 0)
    b = pe.B(cards_rt)
    for key, v in (('m_AnchorMin', '{x: 0, y: 1}'), ('m_AnchorMax', '{x: 1, y: 1}'), ('m_AnchoredPosition', '{x: -4, y: 0}'),
                   ('m_SizeDelta', '{x: -8, y: 0}'), ('m_Pivot', '{x: 0.5, y: 1}')):
        b = re.sub(r'%s: \{[^}]*\}' % key, '%s: %s' % (key, v), b, count=1)
    pe.put(cards_rt, b)
    add_component(pe.go_of[cards_rt], CSF, 'UnityEngine.UI::UnityEngine.UI.ContentSizeFitter', '  m_HorizontalFit: 0\n  m_VerticalFit: 2\n')

def clone(rt, new_parent, index=None, new_name=None):
    """A copy of a whole subtree (raw text: every doc of it with new ids, references inside it re-pointed)."""
    docs = []
    stack = [rt]
    while stack:
        r = stack.pop()
        g = pe.go_of[r]
        docs.append(g)
        docs += pe.comps(g)
        stack += pe.kids.get(r, [])
    docs = list(dict.fromkeys(docs))
    ids = {x: d.nid() for x in docs}
    out = ''
    for x in docs:
        b = pe.B(x)
        b = re.sub(r'^(--- !u!\d+ &)(-?\d+)', lambda m: m.group(1) + ids[m.group(2)], b, count=1)
        b = re.sub(r'(fileID: )(-?\d+)(\})', lambda m: m.group(1) + ids.get(m.group(2), m.group(2)) + m.group(3), b)
        out += b
    d.t += out
    pe.scan()
    new_rt = ids[rt]
    old_parent = pe.father[new_rt]
    pe.put(new_rt, re.sub(r'm_Father: \{fileID: -?\d+\}', 'm_Father: {fileID: %s}' % new_parent, pe.B(new_rt), count=1))
    order = list(pe.kids[new_parent]); order.insert(len(order) if index is None else index, new_rt)
    pe.set_children(new_parent, order)
    if new_name: pe.rename(new_rt, new_name)
    return new_rt

def check():
    pe.scan()
    ids = re.findall(r'^--- !u!\d+ &(-?\d+)', d.t, re.M)
    assert len(ids) == len(set(ids)), 'duplicate ids'
    for p, cs in pe.kids.items():
        for c in cs: assert pe.father.get(c) == p, (p, c)
    for c, p in pe.father.items():
        if p != '0': assert c in pe.kids.get(p, []), (c, p)
    S = set(ids)
    dang = [r for r in set(re.findall(r'\{fileID: (-?\d+)\}', d.t)) if r != '0' and r not in S]
    assert not dang, dang[:5]
