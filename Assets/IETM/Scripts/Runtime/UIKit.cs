// Code-only uGUI toolkit: theme, rounded sprites and widget factories.
// Used by IETMUIBuilder both in the Editor (to pre-build the UI into the scene)
// and at runtime (fallback when no pre-built UI exists).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IETM
{
    public static class Theme
    {
        // HELIOS 3D Application theme (from the HELIOS Figma design).
        public static Color Bg, Panel, Panel2, Panel3, Border, Text, Muted, Dim;
        public static Color Accent, Accent2, Ok, Warn, Danger, RowAlt, RowHover, RowSel;

        static Theme() { ResetToDefaults(); }

        /// <summary>Restores the HELIOS palette.</summary>
        public static void ResetToDefaults()
        {
            Bg = Hex("0A0E27");        // page / status bar
            Panel = Hex("11132D");     // side panels, top bar
            Panel2 = Hex("151836");    // cards
            Panel3 = Hex("1E2347");    // raised controls
            Border = Hex("2F325E");
            Text = Hex("E2E8F0");
            Muted = Hex("94A3B8");
            Dim = Hex("64748B");
            Accent = Hex("6366F1");    // HELIOS indigo (selection, active state, primary)
            Accent2 = Hex("A5B4FC");   // light indigo (secondary accent)
            Ok = Hex("34D399");
            Warn = Hex("FBBF24");
            Danger = Hex("EF4444");
            RowAlt = Hex("13162F");
            RowHover = Hex("1E2347");
            RowSel = new Color(0.388f, 0.4f, 0.945f, 0.22f);
        }

        /// <summary>The palette of the first IETM version (used to restyle scenes built with it).</summary>
        public static readonly Color[] LegacyPalette =
        {
            Hex("0B1020"), Hex("121829"), Hex("182036"), Hex("1F2944"), Hex("2A3552"), Hex("E6EAF2"), Hex("8B95B0"), Hex("5B6582"),
            Hex("22D3EE"), Hex("8B5CF6"), Hex("34D399"), Hex("F59E0B"), Hex("F43F5E"), Hex("141B2E"), Hex("1E2A47"),
        };

        public static Color[] CurrentPalette()
        {
            return new[] { Bg, Panel, Panel2, Panel3, Border, Text, Muted, Dim, Accent, Accent2, Ok, Warn, Danger, RowAlt, RowHover };
        }

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        public static string ToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        public static Color A(this Color c, float a) { c.a = a; return c; }
    }

    public enum BtnStyle { Ghost, Solid, Accent, Danger, Chip }

    public static class UIKit
    {
        // ---------------------------------------------------------------- sprites / font

        /// <summary>Editor hook: return persistent sprite assets instead of runtime-generated ones.</summary>
        public static Func<int, Sprite> RoundedOverride;
        public static Func<Sprite> CircleOverride;

        /// <summary>Font used for new text (set from IETMView.font, e.g. Inter for the HELIOS theme).</summary>
        public static Font FontOverride;

        static Font font;
        public static Font Font
        {
            get
            {
                if (FontOverride != null) return FontOverride;
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                    if (font == null) try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
                return font;
            }
        }

        static Font boldFont; static bool boldSearched;
        /// <summary>A real bold face for the HELIOS font (Inter SemiBold, Resources/IETM/Fonts). Legacy Text has no bold
        /// of its own for a single font file: FontStyle.Bold is a synthetic bold that looks smeared, worst in WebGL.</summary>
        public static Font BoldFont
        {
            get
            {
                if (FontOverride == null) return null;            // built-in font: keep its own (synthetic) bold
                if (!boldSearched) { boldSearched = true; boldFont = Resources.Load<Font>("IETM/Fonts/Inter-SemiBold"); }
                return boldFont;
            }
        }

        /// <summary>Bold on / off for a label: the SemiBold font when there is one, else FontStyle.Bold.</summary>
        public static void SetBold(Text t, bool bold)
        {
            if (t == null) return;
            var bf = BoldFont;
            if (bf != null) { t.font = bold ? bf : Font; t.fontStyle = FontStyle.Normal; }
            else t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        static readonly Dictionary<int, Sprite> rounded = new Dictionary<int, Sprite>();
        static Sprite circle;

        public static int ClampRadius(int r) => Mathf.Clamp(r, 1, 32);

        /// <summary>9-sliced rounded rectangle sprite with the given corner radius (px).</summary>
        public static Sprite Rounded(int radius)
        {
            radius = ClampRadius(radius);
            if (RoundedOverride != null) { var o = RoundedOverride(radius); if (o != null) return o; }
            if (rounded.TryGetValue(radius, out var s) && s != null) return s;
            var tex = RoundedTexture(radius);
            int b = radius + 1;
            s = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = tex.name;
            rounded[radius] = s;
            return s;
        }

        public static Texture2D RoundedTexture(int radius)
        {
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "IETM_Round" + radius };
            var px = new Color32[size * size];
            float c0 = radius, c1 = size - radius - 1;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, c0, c1), cy = Mathf.Clamp(y, c0, c1);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        public static Texture2D CircleTexture()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "IETM_Circle" };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        public static Sprite Circle
        {
            get
            {
                if (CircleOverride != null) { var o = CircleOverride(); if (o != null) return o; }
                if (circle != null) return circle;
                var tex = CircleTexture();
                circle = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                circle.name = tex.name;
                return circle;
            }
        }

        // ---------------------------------------------------------------- rect helpers

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchor to all edges with insets (left, top, right, bottom).</summary>
        public static RectTransform Stretch(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static LayoutElement LE(this Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1, float minW = -1, float minH = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = h; le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            if (minW >= 0) le.minWidth = minW;
            if (minH >= 0) le.minHeight = minH;
            return le;
        }

        public static HorizontalLayoutGroup HLayout(this Component c, float spacing = 6, int padL = 0, int padR = 0, int padT = 0, int padB = 0, TextAnchor align = TextAnchor.MiddleLeft, bool expandW = false, bool expandH = true)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing; g.padding = new RectOffset(padL, padR, padT, padB);
            g.childAlignment = align; g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            return g;
        }

        public static VerticalLayoutGroup VLayout(this Component c, float spacing = 6, int padL = 0, int padR = 0, int padT = 0, int padB = 0, TextAnchor align = TextAnchor.UpperLeft, bool expandW = true, bool expandH = false)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing; g.padding = new RectOffset(padL, padR, padT, padB);
            g.childAlignment = align; g.childControlWidth = true; g.childControlHeight = true;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            return g;
        }

        public static ContentSizeFitter FitVertical(this Component c)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return f;
        }

        // ---------------------------------------------------------------- widgets

        public static Image Panel(Transform parent, Color color, int radius = 0, string name = "Panel", bool raycast = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (radius > 0) { img.sprite = Rounded(radius); img.type = Image.Type.Sliced; }
            return img;
        }

        public static Outline Border(this Graphic g, Color c, float w = 1f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = c; o.effectDistance = new Vector2(w, -w); o.useGraphicAlpha = false;
            return o;
        }

        public static Text Label(Transform parent, string text, int size = 14, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, string name = "Label")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.color = color ?? Theme.Text;
            t.alignment = align; t.fontStyle = style; t.text = text;
            if (style == FontStyle.Bold && BoldFont != null) { t.font = BoldFont; t.fontStyle = FontStyle.Normal; }
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = true;
            return t;
        }

        /// <summary>Button wired to an IETM command (connected automatically at Play).</summary>
        public static IETMButton Button(Transform parent, string text, IETMCommand command, BtnStyle style = BtnStyle.Ghost, int fontSize = 13, float width = -1, float height = 32, string tooltip = null, string argument = null)
        {
            var b = MakeButton(parent, text, style, fontSize, width, height, tooltip);
            b.command = command;
            b.argument = argument;
            return b;
        }

        /// <summary>Button with a code listener (runtime-only, e.g. inside generated list rows).</summary>
        public static IETMButton Button(Transform parent, string text, UnityAction onClick, BtnStyle style = BtnStyle.Ghost, int fontSize = 13, float width = -1, float height = 32, string tooltip = null)
        {
            var b = MakeButton(parent, text, style, fontSize, width, height, tooltip);
            if (onClick != null) b.button.onClick.AddListener(onClick);
            return b;
        }

        static IETMButton MakeButton(Transform parent, string text, BtnStyle style, int fontSize, float width, float height, string tooltip)
        {
            var bg = Panel(parent, Color.white, style == BtnStyle.Chip ? 12 : 7, "Btn " + text);
            var button = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = bg;
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;

            var label = Label(bg.transform, text, fontSize, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Normal);
            label.rectTransform.Stretch(8, 0, 8, 0);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            var b = bg.gameObject.AddComponent<IETMButton>();
            b.button = button; b.background = bg; b.label = label;
            switch (style)
            {
                case BtnStyle.Ghost: b.normal = Theme.Panel2; b.hover = Theme.Panel3; b.active = Theme.Accent; break;
                case BtnStyle.Solid: b.normal = Theme.Panel3; b.hover = Color.Lerp(Theme.Panel3, Color.white, 0.08f); b.active = Theme.Accent; break;
                case BtnStyle.Accent: b.normal = Theme.Accent.A(0.9f); b.hover = Theme.Accent; b.active = Theme.Accent; break;
                case BtnStyle.Danger: b.normal = Theme.Danger.A(0.25f); b.hover = Theme.Danger.A(0.45f); b.active = Theme.Danger; break;
                case BtnStyle.Chip: b.normal = Theme.Panel3; b.hover = Theme.Border; b.active = Theme.Accent; break;
            }
            b.labelColor = style == BtnStyle.Accent ? Color.white : Theme.Text;
            b.labelOnColor = Color.white;
            b.Apply();

            float pw = width > 0 ? width : label.preferredWidth + 26f;
            bg.LE(pw, height, -1, -1, width > 0 ? width : -1, height > 0 ? height : -1);
            if (!string.IsNullOrEmpty(tooltip)) bg.gameObject.AddComponent<UITooltip>().text = tooltip;
            return b;
        }

        /// <summary>
        /// Adds (or updates) an icon on a button. iconOnly = the icon replaces the label text;
        /// otherwise the icon sits left of the label.
        /// </summary>
        public static Image AttachIcon(IETMButton b, Sprite sprite, bool iconOnly, float size = 16f)
        {
            if (b == null || sprite == null) return null;
            Image icon = b.icon;
            if (icon == null)
            {
                var t = b.transform.Find("Icon");
                if (t != null) icon = t.GetComponent<Image>();
            }
            if (icon == null)
            {
                var rt = Rect("Icon", b.transform);
                icon = rt.gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
            }
            icon.sprite = sprite;
            icon.preserveAspect = true;
            var irt = icon.rectTransform;
            if (iconOnly || b.label == null || string.IsNullOrEmpty(b.label.text))
            {
                irt.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                if (b.label) b.label.text = "";
            }
            else
            {
                irt.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(size, size));
                var lrt = b.label.rectTransform;
                lrt.offsetMin = new Vector2(10 + size + 6, lrt.offsetMin.y);
                b.label.alignment = TextAnchor.MiddleLeft;
                var le = b.GetComponent<LayoutElement>();
                if (le != null && le.preferredWidth > 0)
                {
                    float need = 10 + size + 6 + b.label.preferredWidth + 12;
                    if (le.preferredWidth < need) { le.preferredWidth = need; if (le.minWidth > 0) le.minWidth = need; }
                }
            }
            b.icon = icon;
            b.Apply();
            return icon;
        }

        public static ScrollRect Scroll(Transform parent, out RectTransform content, string name = "Scroll")
        {
            var root = Rect(name, parent);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0); // catches scroll events
            var sr = root.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            sr.inertia = true; sr.decelerationRate = 0.12f;

            var viewport = Rect("Viewport", root).Stretch(0, 0, 8, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            sr.viewport = viewport;

            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 0);
            sr.content = content;

            var sbRt = Rect("Scrollbar", root);
            sbRt.anchorMin = new Vector2(1, 0); sbRt.anchorMax = new Vector2(1, 1); sbRt.pivot = new Vector2(1, 0.5f);
            sbRt.sizeDelta = new Vector2(6, 0); sbRt.anchoredPosition = new Vector2(-1, 0);
            var sbBg = sbRt.gameObject.AddComponent<Image>(); sbBg.color = new Color(1, 1, 1, 0.03f); sbBg.sprite = Rounded(3); sbBg.type = Image.Type.Sliced;
            var sb = sbRt.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            var area = Rect("Sliding", sbRt).Stretch();
            var handle = Rect("Handle", area).Stretch();
            var hImg = handle.gameObject.AddComponent<Image>(); hImg.color = Theme.Border; hImg.sprite = Rounded(3); hImg.type = Image.Type.Sliced;
            sb.handleRect = handle; sb.targetGraphic = hImg;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return sr;
        }

        public static InputField Input(Transform parent, string placeholder, float height = 32)
        {
            var bg = Panel(parent, Theme.Bg, 8, "Input");
            bg.Border(Theme.Border);
            var f = bg.gameObject.AddComponent<InputField>();
            var txt = Label(bg.transform, "", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Text");
            txt.rectTransform.Stretch(12, 0, 10, 0);
            txt.supportRichText = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ph = Label(bg.transform, placeholder, 13, Theme.Dim, TextAnchor.MiddleLeft, FontStyle.Italic, "Placeholder");
            ph.rectTransform.Stretch(12, 0, 10, 0);
            f.textComponent = txt; f.placeholder = ph;
            f.caretColor = Theme.Accent; f.selectionColor = Theme.Accent.A(0.3f);
            f.customCaretColor = true;
            var nav = f.navigation; nav.mode = Navigation.Mode.None; f.navigation = nav;
            bg.LE(-1, height, 1, -1, -1, height);
            return f;
        }

        public static Slider Slider(Transform parent, float value, float width = 120)
        {
            var root = Rect("Slider", parent);
            root.LE(width, 24, -1, -1, width, 24);
            var s = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            var track = Panel(root, Theme.Border, 3, "Track", false);
            track.rectTransform.anchorMin = new Vector2(0, 0.5f); track.rectTransform.anchorMax = new Vector2(1, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(-12, 5);
            var fillArea = Rect("FillArea", root);
            fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f); fillArea.sizeDelta = new Vector2(-12, 5);
            var fill = Panel(fillArea, Theme.Accent, 3, "Fill", false);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect("HandleArea", root).Stretch(6, 0, 6, 0);
            var handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(16, 0);
            var hImg = handle.gameObject.AddComponent<Image>(); hImg.sprite = Circle; hImg.color = Color.white;
            s.fillRect = fill.rectTransform; s.handleRect = handle; s.targetGraphic = hImg;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0; s.maxValue = 1; s.value = value;
            var nav = s.navigation; nav.mode = Navigation.Mode.None; s.navigation = nav;
            return s;
        }

        public static Image Divider(Transform parent, bool vertical = false)
        {
            var d = Panel(parent, Theme.Border, 0, "Divider", false);
            if (vertical) d.LE(1, 22, -1, -1, 1, 22); else d.LE(-1, 1, 1, -1, -1, 1);
            return d;
        }

        /// <summary>Destroys all children (works in edit mode and play mode).</summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var g = t.GetChild(i).gameObject;
                g.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(g);
                else UnityEngine.Object.DestroyImmediate(g);
            }
        }

        /// <summary>Rendered width of a (rich) string with a Text's font settings, in canvas units.</summary>
        public static float MeasureWidth(Text t, string s)
        {
            if (t == null || string.IsNullOrEmpty(s)) return 0f;
            var settings = t.GetGenerationSettings(Vector2.zero);
            return t.cachedTextGeneratorForLayout.GetPreferredWidth(s, settings) / Mathf.Max(0.0001f, t.pixelsPerUnit);
        }

        /// <summary>Trims plain text with "…" so it fits maxWidth.</summary>
        public static string Ellipsize(Text t, string s, float maxWidth)
        {
            if (string.IsNullOrEmpty(s) || MeasureWidth(t, s) <= maxWidth) return s;
            int lo = 0, hi = s.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (MeasureWidth(t, s.Substring(0, mid).TrimEnd() + "…") <= maxWidth) lo = mid; else hi = mid - 1;
            }
            return lo <= 0 ? "…" : s.Substring(0, lo).TrimEnd() + "…";
        }

        /// <summary>
        /// uGUI's Outline draws offset copies of the whole graphic behind it. With a see-through fill those
        /// copies show through as a solid block of the border colour (making same-coloured text unreadable).
        /// This makes every bordered panel opaque by blending its tint over the panel behind it.
        /// </summary>
        public static int FlattenBorderedPanels(Transform root, Color fallbackBehind)
        {
            int n = 0;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (!img.TryGetComponent<Shadow>(out var sh) || !sh.enabled) continue;
                var c = img.color;
                if (c.a >= 0.99f || c.a <= 0.01f) continue;
                img.color = OpaqueOver(c, BehindColor(img.transform, fallbackBehind));
                n++;
            }
            return n;
        }

        /// <summary>Colour of the nearest opaque Image behind 't' (parents), or the fallback.</summary>
        public static Color BehindColor(Transform t, Color fallback)
        {
            for (var p = t.parent; p != null; p = p.parent)
                if (p.TryGetComponent<Image>(out var pi) && pi.enabled && pi.color.a >= 0.99f && pi.GetComponent<UnityEngine.UI.Button>() == null) return pi.color;
            return fallback;
        }

        public static Color OpaqueOver(Color tint, Color behind)
        {
            var c = Color.Lerp(behind, tint, tint.a);
            c.a = 1f;
            return c;
        }

        public static PointerHooks Hooks(this Component c)
        {
            return c.TryGetComponent<PointerHooks>(out var h) ? h : c.gameObject.AddComponent<PointerHooks>();
        }

        /// <summary>Instantiate a template row (inactive in the Templates container) under a parent.</summary>
        public static UIRefs Spawn(UIRefs template, Transform parent)
        {
            var r = UnityEngine.Object.Instantiate(template, parent, false);
            r.name = template.name.Replace("Template ", "");
            r.gameObject.SetActive(true);
            return r;
        }
    }
}
