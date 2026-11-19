// HELIOS - builds the whole UI as normal GameObjects (Canvas, Images, TextMeshPro texts, Buttons): top bar, menu,
// camera toolbar, the six pages, the labels on the model and a popup template. Tools > HELIOS > Build UI runs it
// once and saves the result as the "HELIOS UI" prefab; from then on you edit the prefab, not this code.
// Sizes are the prototype's (a 1568 x 744 frame); the canvas scales to the screen and every part is anchored.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Dt = Helios.HeliosData;

namespace Helios
{
    public partial class HeliosUiFactory
    {
        public const float RefW = 1568f, RefH = 744f, TopH = 36f, MenuX = 12f, MenuW = 143f, ColW = 318f, Gap = 8f, ColTop = 46f;

        /// <summary>Where the factory gets its sprites and font, and how it wires buttons (saved in the prefab, or at runtime).</summary>
        public interface IBinder
        {
            Sprite Sprite(string name);
            TMP_FontAsset Font { get; }
            void OnClick(Button b, UnityAction<string> action, string arg);
            void OnClick(Button b, UnityAction<int> action, int arg);
            void OnClick(Button b, UnityAction<GameObject> action, GameObject arg);
        }

        /// <summary>Sprites drawn in memory and listeners added at runtime (used when there is no prefab yet).</summary>
        public class RuntimeBinder : IBinder
        {
            public Sprite Sprite(string name) { return HeliosStyle.Sprite(name); }
            public TMP_FontAsset Font { get { return HeliosTheme.Current.font; } }
            public void OnClick(Button b, UnityAction<string> action, string arg) { b.onClick.AddListener(() => action(arg)); }
            public void OnClick(Button b, UnityAction<int> action, int arg) { b.onClick.AddListener(() => action(arg)); }
            public void OnClick(Button b, UnityAction<GameObject> action, GameObject arg) { b.onClick.AddListener(() => action(arg)); }
        }

        readonly IBinder bind;
        HeliosNavigator nav;
        HeliosActions actions;
        RectTransform ui, pages;

        HeliosUiFactory(IBinder binder) { bind = binder; }

        /// <summary>Builds the UI and returns its root ("HELIOS UI": the backdrop canvas and the UI canvas).</summary>
        public static GameObject Build(IBinder binder)
        {
            var f = new HeliosUiFactory(binder);
            return f.BuildAll();
        }

        GameObject BuildAll()
        {
            var root = new GameObject("HELIOS UI");
            BuildBackdrop(root.transform);

            ui = NewCanvas("UI", root.transform, 10);
            ui.gameObject.AddComponent<GraphicRaycaster>();
            nav = ui.gameObject.AddComponent<HeliosNavigator>();
            actions = ui.gameObject.AddComponent<HeliosActions>();

            var vig = Img(Fill(RT("Vignette", ui), 0, 0, 0, 0), "vignette", HeliosRole.Background, 0.9f, false);
            vig.raycastTarget = false;
            var area = Fill(RT("3D View Area", ui), MenuX + MenuW + Gap, 0f, Gap + ColW + Gap, TopH);
            area.gameObject.AddComponent<HeliosViewArea>();

            pages = Fill(RT("Pages", ui), 0, 0, 0, 0);
            BuildPages();
            BuildTopBar();
            BuildMenu();
            var toolbar = BuildToolbar();
            BuildPopupTemplate();

            nav.toolbar = toolbar.gameObject;
            nav.pages.Clear();
            nav.pages.AddRange(built);
            nav.startPage = Dt.Menu[0]; nav.editPage = Dt.Menu[0];
            for (int i = 0; i < nav.pages.Count; i++) nav.pages[i].gameObject.SetActive(i == 0);
            return root;
        }

        RectTransform NewCanvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(RefW, RefH);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 1f;
            s.referencePixelsPerUnit = 100f;
            return (RectTransform)go.transform;
        }

        void BuildBackdrop(Transform parent)
        {
            var b = NewCanvas("Backdrop", parent, -100);
            b.gameObject.AddComponent<HeliosBackdrop>();
            Img(Fill(RT("Base", b), 0, 0, 0, 0), "square", HeliosRole.Background, 1f, false);
            var g1 = Anchor(RT("Glow", b), new Vector2(0.42f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 1000f));
            Img(g1, "backdrop", HeliosRole.Sky, 0.22f, false);
            var g2 = Anchor(RT("Glow Centre", b), new Vector2(0.42f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f));
            Img(g2, "backdrop", HeliosRole.Cyan, 0.12f, false);
        }

        // =====================================================================
        // top bar, menu, camera toolbar, popup template
        // =====================================================================

        void BuildTopBar()
        {
            var bar = RT("Top Bar", ui);
            bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, TopH); bar.anchoredPosition = Vector2.zero;
            Img(bar, "square", HeliosRole.TopBar, 1f, true);
            var line = RT("Bottom Line", bar); line.anchorMin = Vector2.zero; line.anchorMax = new Vector2(1f, 0f); line.pivot = new Vector2(0.5f, 0f); line.sizeDelta = new Vector2(0f, 1f);
            Img(line, "square", HeliosRole.PanelLine, 1f, false);

            var logo = At(RT("Logo", bar), 8f, 4f, 28f, 28f);
            Img(logo, "circle", HeliosRole.Orange, 1f, false);
            Text(Fill(RT("Letter", logo), 0, 0, 0, 0), "H", 15, true, HeliosRole.Background, TextAlignmentOptions.Center, false);
            var chars = Dt.Brand.ToUpperInvariant().ToCharArray();
            Text(At(RT("Wordmark", bar), 42f, 0f, 112f, TopH), string.Join(" ", System.Array.ConvertAll(chars, c => c.ToString())), 17, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Title", bar), 155f, 0f, 120f, TopH), Dt.AppTitle, 13, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);

            var tail = At(RT("Tail No", bar), 262f, 6f, 118f, 24f);
            Hover(tail, Panel(tail, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, true));
            Text(At(RT("Text", tail), 10f, 0f, 90f, 24f), "Tail No.  <color=#ECF1FA>" + Dt.TailNo + "</color>", 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            Icon(tail, "chevrondown", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-13f, 0f), 12f);

            var logout = AtRight(RT("Sign Out", bar), 10f, 7f, 22f, 22f);
            HoverWash(logout, 6f);
            Icon(logout, "logout", HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, 16f);
            var user = AtRight(RT("User", bar), 34f, 4f, 128f, 28f);
            HoverWash(user, 6f);
            Icon(user, "chevrondown", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-9f, 0f), 12f);
            Text(AtRight(RT("Name", user), 22f, 3f, 76f, 13f), Dt.UserName, 11, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(AtRight(RT("Role", user), 22f, 15f, 76f, 11f), Dt.UserRole, 9, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            var av = AtRight(RT("Avatar", user), 102f, 3f, 22f, 22f);
            Img(av, "circle", HeliosRole.AccentFill, 0.55f, false);
            Img(Fill(RT("Ring", av), 0, 0, 0, 0), "ring-thin", HeliosRole.Sky, 1f, false);
            Text(Fill(RT("Initials", av), 0, 0, 0, 0), Dt.UserInitials, 8, true, HeliosRole.White, TextAlignmentOptions.Center, false);
            Img(AtRight(RT("Divider", bar), 170f, 9f, 1f, 18f), "square", HeliosRole.PanelLine, 1f, false);
            var light = AtRight(RT("Light", bar), 180f, 6f, 56f, 24f);
            Hover(light, Panel(light, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, true));
            Icon(light, "sun", HeliosRole.Text, new Vector2(0f, 0.5f), new Vector2(14f, 0f), 12f);
            Text(At(RT("Text", light), 24f, 0f, 30f, 24f), "Light", 11, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var bell = AtRight(RT("Notifications", bar), 246f, 7f, 22f, 22f);
            HoverWash(bell, 6f);
            Icon(bell, "bell", HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, 16f);
            Img(Anchor(RT("Badge", bell), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-5f, -5f), new Vector2(6f, 6f)), "circle", HeliosRole.Bad, 1f, false);
        }

        void BuildMenu()
        {
            var menu = At(RT("Menu", ui), MenuX, ColTop, MenuW, 262f);
            Panel(menu, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, true);
            var items = VList(menu, "Items", 6f, new RectOffset(7, 7, 7, 7));
            Fill(items, 0, 0, 0, 0);
            for (int i = 0; i < Dt.Menu.Length; i++)
            {
                var it = RT(Dt.Menu[i], items); Size(it.gameObject, -1f, 30f);
                var mi = it.gameObject.AddComponent<HeliosMenuItem>();
                mi.pageId = Dt.Menu[i];
                var sel = Fill(RT("Selected", it), 0, 0, 0, 0);
                Panel(sel, HeliosRole.NavOn, 1f, HeliosRole.NavOnLine, 1f, 7f, false, false);
                mi.selectedLook = sel.gameObject;
                sel.gameObject.SetActive(i == 0);
                var btn = HoverWash(it, 7f);
                var ic = Icon(it, Dt.MenuIcon[i], i == 0 ? HeliosRole.White : HeliosRole.TextMuted, new Vector2(0f, 0.5f), new Vector2(17f, 0f), 14f);
                var tx = Text(At(RT("Label", it), 34f, 0f, MenuW - 50f, 30f), Dt.Menu[i], 12, false, i == 0 ? HeliosRole.White : HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                mi.tinted.Add(ic.GetComponent<HeliosThemed>()); mi.tinted.Add(tx.GetComponent<HeliosThemed>());
                if (i < 6) bind.OnClick(btn, nav.OpenPage, Dt.Menu[i]);       // Settings: no action, as in the prototype
            }
        }

        RectTransform BuildToolbar()
        {
            var tb = Anchor(RT("Camera Toolbar", ui), new Vector2(0f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(430f, 38f));
            // centred under the 3D view area: anchored between the menu and the cards
            tb.anchorMin = new Vector2(0f, 0f); tb.anchorMax = new Vector2(1f, 0f);
            tb.offsetMin = new Vector2(MenuX + MenuW + Gap, 8f); tb.offsetMax = new Vector2(-(Gap + ColW + Gap), 46f);
            var row = HList(tb, "Tools", 14f, new RectOffset(16, 16, 0, 0), TextAnchor.MiddleCenter);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f); row.pivot = new Vector2(0.5f, 0.5f); row.sizeDelta = new Vector2(430f, 38f);
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Panel(row, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 9f, true, true);
            for (int i = 0; i < Dt.Tools.Length; i++)
            {
                if (i > 0) { var sep = RT("Separator", row); Img(sep, "square", HeliosRole.PanelLine, 1f, false); Size(sep.gameObject, 1f, 18f); }
                var item = HList(row, Dt.Tools[i], 6f, new RectOffset(6, 6, 6, 6), TextAnchor.MiddleCenter);
                var btn = HoverWash(item, 6f);
                var ic = Img(RT("Icon", item), "icon-" + Dt.ToolIcon[i], HeliosRole.Text, 1f, false); Size(ic.gameObject, 14f, 14f); ic.preserveAspect = true;
                var tx = Text(RT("Label", item), Dt.Tools[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                var ti = item.gameObject.AddComponent<HeliosToolbarItem>();
                ti.dragMode = i < 3 ? i : -1;
                ti.tinted.Add(ic.GetComponent<HeliosThemed>()); ti.tinted.Add(tx.GetComponent<HeliosThemed>());
                bind.OnClick(btn, actions.CameraTool, i);
            }
            return tb;
        }

        void BuildPopupTemplate()
        {
            var pop = Fill(RT("Popup Template", ui), 0, 0, 0, 0);
            Img(pop, "square", HeliosRole.Background, 0.6f, true);
            var box = Anchor(RT("Window", pop), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 230f));
            Panel(box, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 12f, true, true);
            Text(At(RT("Title", box), 22f, 18f, 360f, 26f), "Popup title", 17, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Body", box), 22f, 54f, 396f, 110f), "Duplicate this popup, change its texts and contents, and open it from any button: in the button's OnClick list choose HeliosActions > ShowObject and drag this popup in.", 12, false, HeliosRole.TextMuted, TextAlignmentOptions.TopLeft, true);
            var close = AtRight(RT("Close", box), 14f, 14f, 24f, 24f);
            bind.OnClick(HoverWash(close, 6f), actions.HideObject, pop.gameObject);
            Icon(close, "close", HeliosRole.TextMuted, new Vector2(0.5f, 0.5f), Vector2.zero, 14f);
            var ok = Anchor(RT("OK Button", box), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(104f, 34f));
            var okImg = Panel(ok, HeliosRole.AccentFill, 1f, HeliosRole.AccentFill, 0f, 8f, false, true);
            bind.OnClick(Hover(ok, okImg), actions.HideObject, pop.gameObject);
            Text(Fill(RT("Text", ok), 0, 0, 0, 0), "OK", 13, true, HeliosRole.White, TextAlignmentOptions.Center, false);
            pop.gameObject.SetActive(false);
        }

        // =====================================================================
        // helpers
        // =====================================================================

        static RectTransform RT(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Placed from the parent's top-left corner (y down), in layout units.</summary>
        static RectTransform At(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Placed from the parent's top-right corner (x leftwards, y down).</summary>
        static RectTransform AtRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = Vector2.one; rt.pivot = Vector2.one;
            rt.anchoredPosition = new Vector2(-x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Stretched over the parent with insets (left, bottom, right, top).</summary>
        static RectTransform Fill(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        static void IgnoreLayout(GameObject g) { var le = g.GetComponent<LayoutElement>(); if (le == null) le = g.AddComponent<LayoutElement>(); le.ignoreLayout = true; }

        static LayoutElement Size(GameObject g, float w, float h)
        {
            var le = g.GetComponent<LayoutElement>(); if (le == null) le = g.AddComponent<LayoutElement>();
            if (w >= 0f) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0f) { le.preferredHeight = h; le.minHeight = h; }
            return le;
        }

        static void Theme(GameObject g, HeliosRole role, float opacity)
        {
            if (role == HeliosRole.None) return;
            var th = g.GetComponent<HeliosThemed>(); if (th == null) th = g.AddComponent<HeliosThemed>();
            th.role = role; th.opacity = opacity; th.Apply();
        }

        Image Img(RectTransform rt, string sprite, HeliosRole role, float opacity, bool raycast)
        {
            var i = rt.gameObject.AddComponent<Image>();
            i.sprite = bind.Sprite(sprite); i.raycastTarget = raycast;
            if (i.sprite != null && i.sprite.border.sqrMagnitude > 0f) i.type = Image.Type.Sliced;
            if (HeliosRoundEnds.Wants(i.sprite)) rt.gameObject.AddComponent<HeliosRoundEnds>();     // pills keep round ends at any size
            Theme(rt.gameObject, role, opacity);
            return i;
        }

        /// <summary>A rounded panel: optional soft shadow, the fill and a 1-unit outline, as the first children of rt.
        /// Returns the fill (the graphic buttons tint).</summary>
        Image Panel(RectTransform rt, HeliosRole fill, float fillOpacity, HeliosRole line, float lineOpacity, float radius, bool shadow, bool raycast)
        {
            float mult = HeliosStyle.RoundedRadius / Mathf.Max(1f, radius);
            if (shadow)
            {
                var sh = Fill(RT("Shadow", rt), -14f, -18f, -14f, -10f);
                var si = Img(sh, "shadow", HeliosRole.Shadow, 1f, false);
                si.pixelsPerUnitMultiplier = 1.5f;
                IgnoreLayout(sh.gameObject);
            }
            var f = Fill(RT("Fill", rt), 0, 0, 0, 0);
            var fi = Img(f, "rounded", fill, fillOpacity, raycast);
            fi.pixelsPerUnitMultiplier = mult;
            IgnoreLayout(f.gameObject);
            if (lineOpacity > 0f)
            {
                var o = Fill(RT("Outline", rt), 0, 0, 0, 0);
                var oi = Img(o, "rounded-line", line, lineOpacity, false);
                oi.pixelsPerUnitMultiplier = mult;
                IgnoreLayout(o.gameObject);
            }
            return fi;
        }

        TextMeshProUGUI Text(RectTransform rt, string text, float size, bool bold, HeliosRole role, TextAlignmentOptions align, bool wrap)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (bind.Font != null) t.font = bind.Font;
            t.text = text; t.fontSize = size; t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = align; t.richText = true; t.raycastTarget = false;
#if UNITY_2023_2_OR_NEWER
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
#else
            t.enableWordWrapping = wrap;
#endif
            t.overflowMode = wrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            Theme(rt.gameObject, role, 1f);
            return t;
        }

        Image Icon(RectTransform parent, string icon, HeliosRole role, Vector2 anchor, Vector2 pos, float size)
        {
            var i = Img(Anchor(RT("Icon", parent), anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size)), "icon-" + icon, role, 1f, false);
            i.preserveAspect = true;
            return i;
        }

        static void Tint(Button b, Color normal, Color hover, Color pressed)
        {
            var cb = b.colors;
            cb.normalColor = normal; cb.highlightedColor = hover; cb.pressedColor = pressed; cb.selectedColor = normal;
            cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
            b.colors = cb;
            var n = b.navigation; n.mode = Navigation.Mode.None; b.navigation = n;
        }

        /// <summary>A button that brightens its graphic on hover.</summary>
        static Button Hover(RectTransform rt, Graphic target)
        {
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = target;
            Tint(b, Color.white, new Color(1.15f, 1.15f, 1.25f, 1f), new Color(0.85f, 0.85f, 0.9f, 1f));
            var cb = b.colors; cb.colorMultiplier = 1.25f; b.colors = cb;
            return b;
        }

        /// <summary>A button with a faint light wash on hover (for rows and icons without a panel of their own).</summary>
        Button HoverWash(RectTransform rt, float radius)
        {
            var h = Fill(RT("Hover", rt), 0, 0, 0, 0);
            h.SetSiblingIndex(0);
            var hi = Img(h, "rounded", HeliosRole.None, 1f, true);
            hi.color = Color.white; hi.pixelsPerUnitMultiplier = HeliosStyle.RoundedRadius / Mathf.Max(1f, radius);
            IgnoreLayout(h.gameObject);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = hi;
            Tint(b, new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.07f), new Color(1f, 1f, 1f, 0.13f));
            return b;
        }

        static RectTransform VList(RectTransform parent, string name, float spacing, RectOffset pad)
        {
            var r = RT(name, parent);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = pad ?? new RectOffset(0, 0, 0, 0); v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return r;
        }

        static RectTransform HList(RectTransform parent, string name, float spacing, RectOffset pad, TextAnchor align)
        {
            var r = RT(name, parent);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = pad ?? new RectOffset(0, 0, 0, 0); h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return r;
        }

        /// <summary>A pill that sizes itself to its text (edit the text and it follows).</summary>
        RectTransform Pill(RectTransform parent, string name, string text, HeliosRole tone, float size, bool solid)
        {
            var r = HList(parent, name, 0f, new RectOffset(9, 9, 2, 2), TextAnchor.MiddleCenter);
            r.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var f = Fill(RT("Fill", r), 0, 0, 0, 0); Img(f, "pill", tone, solid ? 1f : 0.16f, false); IgnoreLayout(f.gameObject);
            if (!solid) { var o = Fill(RT("Outline", r), 0, 0, 0, 0); Img(o, "pill-line", tone, 0.55f, false); IgnoreLayout(o.gameObject); }
            Text(RT("Text", r), text, size, true, solid ? HeliosRole.White : tone, TextAlignmentOptions.Center, false);
            r.sizeDelta = new Vector2(60f, size + 10f);
            return r;
        }

        /// <summary>A round progress ring with the percentage inside.</summary>
        RectTransform Ring(RectTransform parent, float pct, HeliosRole tone, float size, float fontSize)
        {
            var r = RT("Ring", parent); r.sizeDelta = new Vector2(size, size);
            Img(r, "ring", HeliosRole.White, 0.08f, false);
            var f = Img(Fill(RT("Fill", r), 0, 0, 0, 0), "ring", tone, 1f, false);
            f.type = Image.Type.Filled; f.fillMethod = Image.FillMethod.Radial360; f.fillOrigin = (int)Image.Origin360.Top; f.fillClockwise = true;
            f.fillAmount = Mathf.Clamp01(pct / 100f);
            Text(Fill(RT("Value", r), 0, 0, 0, 0), pct.ToString("0") + "%", fontSize, true, HeliosRole.Text, TextAlignmentOptions.Center, false);
            return r;
        }
    }
}
