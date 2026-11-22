// HELIOS + IETM: Light / Dark mode and the notification bell. Lives in the IETM integration folder: no HELIOS file changes.
//
// Light mode recolours the whole application (HELIOS UI and the IETM) from the colours it already uses:
//   dark surfaces -> white / light grey, light text and icons -> dark grey, accents and status colours keep their hue
//   (darkened a little for contrast on white). Photos, logos and pure-white glyphs keep their colours.
// The original colours are remembered, so Dark mode restores them exactly. Colours set later by code (hover, selection,
// lists rebuilt from data) are picked up and recoloured every frame while Light mode is on.
// The app starts in Dark mode (Remember Last Mode on the component: start in the mode used last time).
//
// Bell: no unread badge; clicking it shows "No new notifications" under the bell for 3 seconds.
// The component sits on "IETM Integration" in the HELIOS scene and the popup is in the HELIOS UI prefab, so nothing is
// built at Play (both are created from code only if missing).
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    [DefaultExecutionOrder(10000)]   // after everything that sets UI colours in Update / LateUpdate
    public class HeliosThemeMode : MonoBehaviour
    {
        const string PrefKey = "HELIOS.LightMode";
        public static HeliosThemeMode Instance { get; private set; }

        [Tooltip("Seconds the 'No new notifications' popup stays.")]
        public float notificationSeconds = 3f;
        [Tooltip("3D view background in Light mode (behind the aircraft and the landing gear).")]
        public Color lightViewBackground = new Color(0.86f, 0.88f, 0.92f, 1f);
        [Tooltip("OFF (default): the app always starts in Dark mode. ON: it starts in the mode used last time.")]
        public bool rememberLastMode = false;

        public bool IsLight { get; private set; }

        Transform heliosRoot, ietmRoot, topBar;
        Transform ietmTemplates;   // IETM row / card templates: lists are cloned from them, they always stay dark
        TMP_Text lightLabel;
        GameObject blueBackground, vignette;
        Camera heliosBackCam; Color heliosBackCamDark;
        RawImage ietmViewBg; Texture ietmViewBgTex; Color ietmViewBgColor;

        struct Entry { public Color original, applied; }
        readonly Dictionary<Object, Entry> graphics = new Dictionary<Object, Entry>();
        readonly List<Graphic> gList = new List<Graphic>();
        readonly List<Shadow> sList = new List<Shadow>();
        struct CbEntry { public ColorBlock original, applied; }
        readonly Dictionary<Selectable, CbEntry> blocks = new Dictionary<Selectable, CbEntry>();
        readonly List<Selectable> selList = new List<Selectable>();

        RectTransform popup; CanvasGroup popupGroup; float popupUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("HELIOS Theme Mode");
            go.AddComponent<HeliosThemeMode>();
        }

        void Awake() { Instance = this; }

        IEnumerator Start()
        {
            // the HELIOS UI is instantiated by HeliosApp at start-up: wait for its top bar
            for (int i = 0; i < 120 && topBar == null; i++)
            {
                var tb = GameObject.Find("Top Bar");
                if (tb != null && tb.transform.Find("Light") != null) topBar = tb.transform;
                else yield return null;
            }
            if (topBar == null) { Destroy(gameObject); yield break; }   // not the HELIOS scene
            heliosRoot = topBar.root;

            var light = topBar.Find("Light");
            if (light != null)
            {
                var t = light.Find("Text"); if (t != null) lightLabel = t.GetComponent<TMP_Text>();
                if (light.TryGetComponent<Button>(out var b)) b.onClick.AddListener(Toggle);
            }
            var bell = topBar.Find("Notifications");
            if (bell != null)
            {
                var badge = bell.Find("Badge"); if (badge != null) badge.gameObject.SetActive(false);
                if (bell.TryGetComponent<Button>(out var b)) b.onClick.AddListener(ShowNoNotifications);
            }

            foreach (var t in heliosRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Blue Background" && t.parent != null && t.parent.name == "Backdrop") blueBackground = t.gameObject;
                if (t.name == "Vignette" && t.parent != null && t.parent.name == "UI") vignette = t.gameObject;   // dark edge shade over the whole screen
            }
            var bc = GameObject.Find("HELIOS Background Camera");
            if (bc != null) { heliosBackCam = bc.GetComponent<Camera>(); if (heliosBackCam) heliosBackCamDark = heliosBackCam.backgroundColor; }

            var app = FindAnyObjectByType<IETMApp>(FindObjectsInactive.Include);
            if (app != null) ietmRoot = app.transform.root;
            if (app != null && app.view != null && app.view.templates != null) ietmTemplates = app.view.templates.transform;

            if (rememberLastMode && PlayerPrefs.GetInt(PrefKey, 0) == 1) SetLight(true);   // default: Dark
            UpdateLabel();
        }

        // ------------------------------------------------------------------ light / dark

        public void Toggle() => SetLight(!IsLight);

        public void SetLight(bool on)
        {
            if (IsLight == on) return;
            IsLight = on;
            PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
            if (on) { RecolourAll(); }
            else { RestoreAll(); }
            ApplyBackgrounds();
            UpdateLabel();
        }

        void UpdateLabel() { if (lightLabel != null) lightLabel.text = IsLight ? "Dark" : "Light"; }

        void LateUpdate()
        {
            if (popup != null && popup.gameObject.activeSelf) UpdatePopup();
            if (!IsLight) return;
            RecolourAll();
            ApplyBackgrounds();   // the IETM viewport background is created when the IETM first opens
        }

        void RecolourAll()
        {
            Recolour(heliosRoot);
            if (ietmRoot != null && ietmRoot != heliosRoot) Recolour(ietmRoot);
        }

        /// <summary>The top bar keeps its dark design in both modes (and so does the bell popup under it).</summary>
        bool Excluded(Component c)
        {
            var tr = c.transform;
            return (topBar != null && tr.IsChildOf(topBar)) || (popup != null && tr.IsChildOf(popup))
                || (ietmTemplates != null && tr.IsChildOf(ietmTemplates));
        }

        void Recolour(Transform root)
        {
            if (root == null) return;
            root.GetComponentsInChildren(false, gList);   // active only: hidden templates must keep their dark colours
            foreach (var g in gList)
            {
                if (g == null || g is RawImage || Excluded(g)) continue;
                var c = g.color;
                if (graphics.TryGetValue(g, out var e) && c == e.applied) continue;   // already done
                if (Keep(g, c)) { graphics.Remove(g); continue; }
                var a = ToLight(c);
                graphics[g] = new Entry { original = c, applied = a };
                g.color = a;
            }
            // buttons tinted by their colour block (IETM buttons: white image, colour from normal / hover / active)
            root.GetComponentsInChildren(false, selList);
            foreach (var sel in selList)
            {
                if (sel == null || sel.transition != Selectable.Transition.ColorTint || Excluded(sel)) continue;
                var cb = sel.colors;
                if (blocks.TryGetValue(sel, out var be) && SameBlock(cb, be.applied)) continue;
                var a = cb;
                a.normalColor = DarkToLight(cb.normalColor); a.highlightedColor = DarkToLight(cb.highlightedColor);
                a.pressedColor = DarkToLight(cb.pressedColor); a.selectedColor = DarkToLight(cb.selectedColor);
                a.disabledColor = DarkToLight(cb.disabledColor);
                blocks[sel] = new CbEntry { original = cb, applied = a };
                if (!SameBlock(cb, a)) sel.colors = a;
            }
            root.GetComponentsInChildren(false, sList);
            foreach (var s in sList)
            {
                if (s == null || Excluded(s)) continue;
                var c = s.effectColor;
                if (graphics.TryGetValue(s, out var e) && c == e.applied) continue;
                var a = ToLight(c);
                graphics[s] = new Entry { original = c, applied = a };
                s.effectColor = a;
            }
        }

        void RestoreAll()
        {
            foreach (var kv in graphics)
            {
                if (kv.Key is Graphic g && g != null) { if (g.color == kv.Value.applied) g.color = kv.Value.original; }
                else if (kv.Key is Shadow s && s != null) { if (s.effectColor == kv.Value.applied) s.effectColor = kv.Value.original; }
            }
            graphics.Clear();
            foreach (var kv in blocks)
                if (kv.Key != null && SameBlock(kv.Key.colors, kv.Value.applied)) kv.Key.colors = kv.Value.original;
            blocks.Clear();
        }

        /// <summary>Colour-block tints: only dark tints change (white / light-grey default tints multiply the image colour and stay).</summary>
        static Color DarkToLight(Color c)
        {
            float l = (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) + Mathf.Min(c.r, Mathf.Min(c.g, c.b))) * 0.5f;
            return l < 0.4f ? ToLight(c) : c;
        }

        static bool SameBlock(ColorBlock a, ColorBlock b) =>
            a.normalColor == b.normalColor && a.highlightedColor == b.highlightedColor && a.pressedColor == b.pressedColor
            && a.selectedColor == b.selectedColor && a.disabledColor == b.disabledColor;

        /// <summary>Photos, logos and pure-white glyphs (on coloured buttons) keep their colours.</summary>
        static bool Keep(Graphic g, Color c)
        {
            if (c.r > 0.985f && c.g > 0.985f && c.b > 0.985f) return true;
            return false;
        }

        /// <summary>Dark-theme colour -> light-theme colour.</summary>
        public static Color ToLight(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sv, out float v);
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float l = (max + min) * 0.5f;
            float s = max - min < 1e-4f ? 0f : (max - min) / (1f - Mathf.Abs(2f * l - 1f));

            if (l < 0.02f && c.a < 0.7f) return new Color(0f, 0f, 0f, c.a * 0.3f);   // drop shadows: soft grey

            float nl, ns;
            if (l < 0.4f)                       // surfaces: panels, rows, borders, fills -> white / light grey
            {
                nl = 0.985f - 0.4f * l;
                ns = s * 0.45f;
            }
            else if (sv < 0.3f)                 // neutral text and icons -> dark grey
            {
                nl = Mathf.Lerp(0.6f, 0.12f, Mathf.InverseLerp(0.4f, 0.93f, l));
                ns = Mathf.Min(s, 0.25f);
            }
            else                                // accents and status colours: same hue, darker for white backgrounds
            {
                nl = Mathf.Min(l, 0.56f);
                ns = s * 0.8f;
            }
            var o = HslToRgb(h, Mathf.Clamp01(ns), Mathf.Clamp01(nl));
            o.a = c.a;
            return o;
        }

        static Color HslToRgb(float h, float s, float l)
        {
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s, p = 2f * l - q;
            return new Color(Hue(p, q, h + 1f / 3f), Hue(p, q, h), Hue(p, q, h - 1f / 3f));
        }
        static float Hue(float p, float q, float t)
        {
            if (t < 0f) t += 1f; if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 0.5f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        /// <summary>The blue backdrop images become a plain light grey in Light mode.</summary>
        void ApplyBackgrounds()
        {
            if (blueBackground != null && blueBackground.activeSelf == IsLight) blueBackground.SetActive(!IsLight);
            if (vignette != null && vignette.activeSelf == IsLight) vignette.SetActive(!IsLight);
            if (heliosBackCam != null) heliosBackCam.backgroundColor = IsLight ? lightViewBackground : heliosBackCamDark;

            if (ietmViewBg == null && ietmRoot != null)
            {
                var t = ietmRoot.Find("IETM Viewport Background/Canvas/Background");
                if (t == null) foreach (var tr in ietmRoot.GetComponentsInChildren<Transform>(true)) if (tr.name == "IETM Viewport Background") { t = tr.Find("Canvas/Background"); break; }
                if (t != null && t.TryGetComponent(out ietmViewBg)) { ietmViewBgTex = ietmViewBg.texture; ietmViewBgColor = ietmViewBg.color; }
            }
            if (ietmViewBg != null)
            {
                var tex = IsLight ? null : ietmViewBgTex;
                var col = IsLight ? lightViewBackground : ietmViewBgColor;
                if (ietmViewBg.texture != tex) ietmViewBg.texture = tex;
                if (ietmViewBg.color != col) ietmViewBg.color = col;
            }
        }

        // ------------------------------------------------------------------ notifications

        void ShowNoNotifications()
        {
            var bell = topBar != null ? topBar.Find("Notifications") as RectTransform : null;
            if (bell == null) return;
            if (popup == null) BindPopup();
            if (popup == null) BuildPopup();
            if (popup == null) return;

            // under the bell, on top of everything in the HELIOS UI
            var canvas = bell.GetComponentInParent<Canvas>().rootCanvas;
            popup.SetParent(canvas.transform, false);
            popup.SetAsLastSibling();
            var corners = new Vector3[4];
            bell.GetWorldCorners(corners);
            var bottomCentre = (corners[0] + corners[3]) * 0.5f;
            popup.position = bottomCentre;
            popup.anchoredPosition += new Vector2(0f, -8f);
            // keep it inside the screen on the right
            var cr = ((RectTransform)canvas.transform).rect;
            float right = popup.anchoredPosition.x + popup.rect.width * (1f - popup.pivot.x);
            float maxRight = cr.width * (1f - ((RectTransform)canvas.transform).pivot.x) - 8f;
            if (right > maxRight) popup.anchoredPosition -= new Vector2(right - maxRight, 0f);

            popupUntil = Time.unscaledTime + notificationSeconds;
            popupGroup.alpha = 1f;
            popup.gameObject.SetActive(true);
        }

        /// <summary>The popup is in the HELIOS UI prefab ("UI > Notifications Popup"): only found here.</summary>
        void BindPopup()
        {
            foreach (var t in heliosRoot.GetComponentsInChildren<Transform>(true))
                if (t.name == "Notifications Popup" && t is RectTransform rt)
                {
                    popup = rt;
                    popupGroup = rt.GetComponent<CanvasGroup>();
                    if (popupGroup == null) popupGroup = rt.gameObject.AddComponent<CanvasGroup>();
                    return;
                }
        }

        /// <summary>Fallback when the prefab has no popup.</summary>
        void BuildPopup()
        {
            var theme = Helios.HeliosTheme.Current;
            var go = new GameObject("Notifications Popup", typeof(RectTransform)) { layer = 5 };
            popup = (RectTransform)go.transform;
            popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.pivot = new Vector2(0.5f, 1f);
            popup.sizeDelta = new Vector2(210f, 54f);
            popupGroup = go.AddComponent<CanvasGroup>();
            popupGroup.blocksRaycasts = false; popupGroup.interactable = false;

            // same rounded sprite as the top bar's Light button
            Sprite rounded = null;
            var lightFill = topBar.Find("Light/Fill");
            if (lightFill != null && lightFill.TryGetComponent<Image>(out var lf)) rounded = lf.sprite;

            var bg = go.AddComponent<Image>();
            bg.sprite = rounded; bg.type = rounded != null ? Image.Type.Sliced : Image.Type.Simple;
            var pc = theme.panel; pc.a = 0.98f; bg.color = pc;
            bg.raycastTarget = false;
            var ol = go.AddComponent<Outline>(); ol.effectColor = theme.panelLine; ol.effectDistance = new Vector2(1f, -1f);

            TMP_FontAsset font = theme.font;
            var nameT = topBar.Find("User/Name");
            if (font == null && nameT != null && nameT.TryGetComponent<TMP_Text>(out var nt)) font = nt.font;

            Label(popup, "Title", "No new notifications", 12, FontStyles.Bold, theme.text, new Vector2(0f, -10f), font);
            Label(popup, "Sub", "You're all caught up", 10, FontStyles.Normal, theme.textMuted, new Vector2(0f, -29f), font);
            go.SetActive(false);
        }

        static void Label(RectTransform parent, string name, string text, float size, FontStyles style, Color color, Vector2 pos, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(-16f, 18f);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void UpdatePopup()
        {
            float left = popupUntil - Time.unscaledTime;
            if (left <= 0f) { popup.gameObject.SetActive(false); return; }
            popupGroup.alpha = Mathf.Clamp01(left / 0.3f);   // fade out over the last 0.3 s
        }
    }
}
