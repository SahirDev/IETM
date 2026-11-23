"""Tiny Unity YAML writer: adds GameObjects / components to a scene or prefab, using the exact
serialisation Unity 6 already wrote in the file (component docs are written from the same field lists)."""
import re, random

IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc'
RAWIMAGE = '1344c3c82d62a2a41a3576d8abb8e3ea'
TEXT = '5f7201a12d95ffc409449d95f23cf332'
OUTLINE = 'e19747de3f5aca642ab2be37e372fb86'
BUTTON = '4e29b1a8efbd4b44bb3f3716e73f07ff'
LE = '306cc8c2b49d7114eaa3623786fc2126'
HLG = '30649d3a9faa99c48a7b1166b86bf2a0'
VLG = '59f8146938fff824cb5fd77236b75775'
CSF = '3245ec927659c4140ac4f8d17403cc18'
ARF = '86710e43de46f6f4bac7c8e50813a599'
RECTMASK = '3312d7739989d2b4e91e6319e9a96d76'
UITOOLTIP = '1e22e8d19d184140b2e23705b9e5afc0'
HDR = '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'


def f(v):
    if isinstance(v, bool): return '1' if v else '0'
    if isinstance(v, float):
        s = ('%.6f' % v).rstrip('0').rstrip('.')
        return s if s not in ('-0', '') else '0'
    return str(v)


def col(c): return '{r: %s, g: %s, b: %s, a: %s}' % tuple(f(float(x)) for x in c)


def hexc(h, a=1.0):
    return (int(h[0:2], 16) / 255, int(h[2:4], 16) / 255, int(h[4:6], 16) / 255, a)


def v2(x, y): return '{x: %s, y: %s}' % (f(float(x)), f(float(y)))


class Doc:
    def __init__(self, path):
        self.path = path
        self.t = open(path, encoding='utf-8').read()
        self.ids = set(re.findall(r'^--- !u!\d+ &(-?\d+)', self.t, re.M))
        self.new = []          # docs to insert
        self.children = {}     # parent rt -> [child rt] appended

    def nid(self):
        while True:
            n = str(random.randint(10 ** 15, 9 * 10 ** 17))
            if n not in self.ids: self.ids.add(n); return n

    # ---------------------------------------------------------------- objects
    def go(self, name, parent_rt, layer=5, active=True, rect=True, anchor_min=(0.5, 0.5), anchor_max=(0.5, 0.5),
           pos=(0, 0), size=(100, 100), pivot=(0.5, 0.5), rot=None):
        g, tr = self.nid(), self.nid()
        o = {'go': g, 'tr': tr, 'comps': [tr], 'name': name, 'layer': layer, 'active': active, 'kids': [],
             'rect': rect, 'parent': parent_rt, 'am': anchor_min, 'ax': anchor_max, 'pos': pos, 'size': size, 'piv': pivot,
             'rot': rot or (0, 0, 0, 1), 'docs': []}
        self.new.append(o)
        if parent_rt is not None:
            p = next((x for x in self.new if x['tr'] == parent_rt), None)
            if p is not None: p['kids'].append(tr)
            else: self.children.setdefault(parent_rt, []).append(tr)
        return o

    def comp(self, o, cls, body):
        cid = self.nid()
        o['comps'].append(cid)
        o['docs'].append('--- !u!%s &%s\n%s:\n%s  m_GameObject: {fileID: %s}\n%s' % (cls[0], cid, cls[1], HDR, o['go'], body))
        return cid

    def mono(self, o, guid, cls_id, body, enabled=True):
        return self.comp(o, (114, 'MonoBehaviour'),
                         '  m_Enabled: %s\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: %s\n%s'
                         % (f(enabled), guid, cls_id, body))

    # ---------------------------------------------------------------- UI components
    def canvas_renderer(self, o):
        return self.comp(o, (222, 'CanvasRenderer'), '  m_CullTransparentMesh: 1\n')

    def image(self, o, color, sprite_guid=None, sliced=True, raycast=False, preserve=False):
        self.canvas_renderer(o)
        sp = '{fileID: 21300000, guid: %s, type: 3}' % sprite_guid if sprite_guid else '{fileID: 0}'
        return self.mono(o, IMAGE, 'UnityEngine.UI::UnityEngine.UI.Image',
            '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: %s\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
            '  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: %s\n  m_Type: %s\n'
            '  m_PreserveAspect: %s\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n'
            '  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: 1\n'
            % (col(color), f(raycast), sp, 1 if (sliced and sprite_guid) else 0, f(preserve)))

    def raw_image(self, o, color, tex_guid):
        self.canvas_renderer(o)
        return self.mono(o, RAWIMAGE, 'UnityEngine.UI::UnityEngine.UI.RawImage',
            '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: 0\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
            '  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'
            '  m_Texture: {fileID: 2800000, guid: %s, type: 3}\n  m_UVRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1\n'
            % (col(color), tex_guid))

    def text(self, o, text, size, color, align=3, bold=False, hover=0, vover=0, line=1.0, font_guid='f53a4e0193702b943b3ab8e764ae64f1'):
        self.canvas_renderer(o)
        return self.mono(o, TEXT, 'UnityEngine.UI::UnityEngine.UI.Text',
            '  m_Material: {fileID: 0}\n  m_Color: %s\n  m_RaycastTarget: 0\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
            '  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_FontData:\n'
            '    m_Font: {fileID: 12800000, guid: %s, type: 3}\n    m_FontSize: %d\n    m_FontStyle: %d\n    m_BestFit: 0\n'
            '    m_MinSize: 2\n    m_MaxSize: 40\n    m_Alignment: %d\n    m_AlignByGeometry: 0\n    m_RichText: 1\n'
            '    m_HorizontalOverflow: %d\n    m_VerticalOverflow: %d\n    m_LineSpacing: %s\n  m_Text: %s\n'
            % (col(color), font_guid, size, 1 if bold else 0, align, hover, vover, f(float(line)), text))

    def outline(self, o, color, w=1.0):
        return self.mono(o, OUTLINE, 'UnityEngine.UI::UnityEngine.UI.Outline',
            '  m_EffectColor: %s\n  m_EffectDistance: %s\n  m_UseGraphicAlpha: 0\n' % (col(color), v2(w, -w)))

    def layout_element(self, o, w=-1, h=-1, fw=-1, fh=-1, mw=-1, mh=-1):
        return self.mono(o, LE, 'UnityEngine.UI::UnityEngine.UI.LayoutElement',
            '  m_IgnoreLayout: 0\n  m_MinWidth: %s\n  m_MinHeight: %s\n  m_PreferredWidth: %s\n  m_PreferredHeight: %s\n'
            '  m_FlexibleWidth: %s\n  m_FlexibleHeight: %s\n  m_LayoutPriority: 1\n' % tuple(f(x) for x in (mw, mh, w, h, fw, fh)))

    def layout_group(self, o, vertical, spacing, pad, align, expand_w, expand_h):
        guid, cls = (VLG, 'VerticalLayoutGroup') if vertical else (HLG, 'HorizontalLayoutGroup')
        return self.mono(o, guid, 'UnityEngine.UI::UnityEngine.UI.' + cls,
            '  m_Padding:\n    m_Left: %d\n    m_Right: %d\n    m_Top: %d\n    m_Bottom: %d\n  m_ChildAlignment: %d\n  m_Spacing: %s\n'
            '  m_ChildForceExpandWidth: %s\n  m_ChildForceExpandHeight: %s\n  m_ChildControlWidth: 1\n  m_ChildControlHeight: 1\n'
            '  m_ChildScaleWidth: 0\n  m_ChildScaleHeight: 0\n  m_ReverseArrangement: 0\n'
            % (pad[0], pad[1], pad[2], pad[3], align, f(float(spacing)), f(expand_w), f(expand_h)))

    def fitter(self, o, h=0, v=2):
        return self.mono(o, CSF, 'UnityEngine.UI::UnityEngine.UI.ContentSizeFitter', '  m_HorizontalFit: %d\n  m_VerticalFit: %d\n' % (h, v))

    def aspect_fitter(self, o, mode, ratio):
        return self.mono(o, ARF, 'UnityEngine.UI::UnityEngine.UI.AspectRatioFitter', '  m_AspectMode: %d\n  m_AspectRatio: %s\n' % (mode, f(float(ratio))))

    def tooltip(self, o, text):
        return self.mono(o, UITOOLTIP, 'Assembly-CSharp::IETM.UITooltip', '  text: %s\n' % text)

    def button(self, o, target, normal=(1, 1, 1, 1), hi=(1.25, 1.25, 1.25, 1), pressed=(0.8, 0.8, 0.8, 1), disabled=(1, 1, 1, 0.35), mult=1.4):
        return self.mono(o, BUTTON, 'UnityEngine.UI::UnityEngine.UI.Button',
            '  m_Navigation:\n    m_Mode: 0\n    m_WrapAround: 0\n    m_SelectOnUp: {fileID: 0}\n    m_SelectOnDown: {fileID: 0}\n'
            '    m_SelectOnLeft: {fileID: 0}\n    m_SelectOnRight: {fileID: 0}\n  m_Transition: 1\n  m_Colors:\n'
            '    m_NormalColor: %s\n    m_HighlightedColor: %s\n    m_PressedColor: %s\n    m_SelectedColor: %s\n    m_DisabledColor: %s\n'
            '    m_ColorMultiplier: %s\n    m_FadeDuration: 0.1\n  m_SpriteState:\n    m_HighlightedSprite: {fileID: 0}\n'
            '    m_PressedSprite: {fileID: 0}\n    m_SelectedSprite: {fileID: 0}\n    m_DisabledSprite: {fileID: 0}\n'
            '  m_AnimationTriggers:\n    m_NormalTrigger: Normal\n    m_HighlightedTrigger: Highlighted\n    m_PressedTrigger: Pressed\n'
            '    m_SelectedTrigger: Selected\n    m_DisabledTrigger: Disabled\n  m_Interactable: 1\n  m_TargetGraphic: {fileID: %s}\n'
            '  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n'
            % (col(normal), col(hi), col(pressed), col(normal), col(disabled), f(float(mult)), target))

    def canvas_group(self, o, alpha=1, interactable=False, blocks=False):
        return self.comp(o, (225, 'CanvasGroup'), '  m_Enabled: 1\n  m_Alpha: %s\n  m_Interactable: %s\n  m_BlocksRaycasts: %s\n  m_IgnoreParentGroups: 0\n'
                         % (f(float(alpha)), f(interactable), f(blocks)))

    # ---------------------------------------------------------------- write
    def _go_doc(self, o):
        comps = ''.join('  - component: {fileID: %s}\n' % c for c in o['comps'])
        return ('--- !u!1 &%s\nGameObject:\n%s  serializedVersion: 6\n  m_Component:\n%s  m_Layer: %d\n  m_Name: %s\n  m_TagString: Untagged\n'
                '  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: %s\n'
                % (o['go'], HDR, comps, o['layer'], o['name'], f(o['active'])))

    def _tr_doc(self, o):
        kids = 'm_Children: []\n' if not o['kids'] else 'm_Children:\n' + ''.join('  - {fileID: %s}\n' % k for k in o['kids'])
        father = o['parent'] or '0'
        r = o['rot']
        if o['rect']:
            return ('--- !u!224 &%s\nRectTransform:\n%s  m_GameObject: {fileID: %s}\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n'
                    '  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n'
                    '  %s  m_Father: {fileID: %s}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: %s\n  m_AnchorMax: %s\n'
                    '  m_AnchoredPosition: %s\n  m_SizeDelta: %s\n  m_Pivot: %s\n'
                    % (o['tr'], HDR, o['go'], kids, father, v2(*o['am']), v2(*o['ax']), v2(*o['pos']), v2(*o['size']), v2(*o['piv'])))
        return ('--- !u!4 &%s\nTransform:\n%s  m_GameObject: {fileID: %s}\n  serializedVersion: 2\n  m_LocalRotation: {x: %s, y: %s, z: %s, w: %s}\n'
                '  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n'
                '  %s  m_Father: {fileID: %s}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
                % (o['tr'], HDR, o['go'], *(f(float(x)) for x in r), kids, father))

    def add_component_to_existing(self, go_id, doc_text_fn):
        """Adds a component doc to an existing GameObject. doc_text_fn(cid) -> full doc text."""
        cid = self.nid()
        m = re.search(r'(--- !u!1 &%s\nGameObject:\n(?:.*\n)*?  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)+)' % go_id, self.t)
        assert m, go_id
        self.t = self.t[:m.end()] + '  - component: {fileID: %s}\n' % cid + self.t[m.end():]
        self.extra = getattr(self, 'extra', '') + doc_text_fn(cid)
        return cid

    def save(self, roots_add=None, before_marker=None):
        blob = ''
        for o in self.new:
            blob += self._go_doc(o) + self._tr_doc(o) + ''.join(o['docs'])
        blob += getattr(self, 'extra', '')
        for parent, kids in self.children.items():
            m = re.search(r'(--- !u!(?:224|4) &%s\n\w+:\n(?:.*\n)*?)(  m_Children: \[\]\n|  m_Children:\n(?:  - \{fileID: -?\d+\}\n)*)' % parent, self.t)
            assert m, parent
            existing = re.findall(r'fileID: (-?\d+)', m.group(2))
            new = '  m_Children:\n' + ''.join('  - {fileID: %s}\n' % k for k in existing + kids)
            self.t = self.t[:m.start(2)] + new + self.t[m.end(2):]
        if roots_add:
            m = re.search(r'(--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n(?:  - \{fileID: -?\d+\}\n)*)', self.t)
            self.t = self.t[:m.end()] + ''.join('  - {fileID: %s}\n' % r for r in roots_add) + self.t[m.end():]
        i = self.t.index(before_marker) if before_marker and before_marker in self.t else len(self.t)
        self.t = self.t[:i] + blob + self.t[i:]
        open(self.path, 'w', encoding='utf-8', newline='').write(self.t)
