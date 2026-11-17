// Applies the HELIOS 3D Application look (colours, font, icons, logo) to the IETM UI in the open scene.
// Only the look changes: layout, buttons, commands and features stay exactly as they are.
//   IETM > Apply HELIOS Theme   (also a button on the IETM App inspector)
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IETM.EditorTools
{
    public static class IETMHeliosTheme
    {
        public const string IconDir = "Assets/IETM/UI/Icons";
        public const string LogoPath = "Assets/IETM/UI/Logo/HELIOS_logo.png";
        public const string FontPath = "Assets/IETM/UI/Fonts/Inter-Regular.otf";
        public static readonly Color CameraBackground = new Color(0.043f, 0.063f, 0.2f); // #0B1033

        // Button command -> (icon name, icon only?)
        static readonly Dictionary<IETMCommand, (string icon, bool only)> CommandIcons = new Dictionary<IETMCommand, (string, bool)>
        {
            { IETMCommand.HistoryBack, ("arrow_left", true) },
            { IETMCommand.HistoryForward, ("arrow_right", true) },
            { IETMCommand.OpenTitlePage, ("home", true) },
            { IETMCommand.TabPublication, ("book", false) },
            { IETMCommand.TabStructure, ("folder", false) },
            { IETMCommand.ToggleOrderList, ("report", false) },
            { IETMCommand.HomeView, ("cube", true) },
            { IETMCommand.FitSelection, ("zoom", false) },
            { IETMCommand.AutoRotate, ("rotate", false) },
            { IETMCommand.Snapshot, ("image", true) },
            { IETMCommand.IpdReset, ("rotate", false) },
            { IETMCommand.IpdCopyList, ("document", false) },
            { IETMCommand.OrderCopy, ("document", false) },
            { IETMCommand.StepPrevious, ("arrow_left", false) },
            { IETMCommand.StepNext, ("arrow_right", false) },
            { IETMCommand.StepRestart, ("rotate", false) },
            { IETMCommand.ToggleLeftPanel, ("chevron_left", true) },
            { IETMCommand.ToggleRightPanel, ("chevron_left", true) },
        };

        [MenuItem("IETM/Apply HELIOS Theme", priority = 30)]
        public static void ApplyMenu()
        {
            var app = Object.FindAnyObjectByType<IETMApp>();
            if (app == null || app.view == null)
            {
                EditorUtility.DisplayDialog("IETM", "No pre-built IETM UI in the open scene.\nOpen the Landing Gear IETM scene first.", "OK");
                return;
            }
            Apply(app);
        }

        /// <summary>Restyles the app's pre-built UI, camera background and preview content.</summary>
        public static void Apply(IETMApp app)
        {
            Undo.RegisterFullObjectHierarchyUndo(app.view.gameObject, "Apply HELIOS Theme");
            Undo.RecordObject(app, "Apply HELIOS Theme");
            ApplyToView(app.view);
            app.backgroundColor = CameraBackground;
            if (app.viewerCamera != null)
            {
                Undo.RecordObject(app.viewerCamera, "Apply HELIOS Theme");
                app.viewerCamera.backgroundColor = CameraBackground;
            }
            EditorUtility.SetDirty(app);
            IETMSceneBuilder.RefreshPreview(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            Debug.Log("[IETM] HELIOS theme applied.");
        }

        /// <summary>Colours, font, icons and logo on an IETMView. Safe to run more than once.</summary>
        public static void ApplyToView(IETMView v)
        {
            if (v == null) return;
            Theme.ResetToDefaults();
            var from = Theme.LegacyPalette;
            var to = Theme.CurrentPalette();
            Color legacyBg = from[0];

            var font = v.font != null ? v.font : AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            v.font = font;
            v.logo = LoadSprite(LogoPath);
            v.iconFolder = Icon("folder");
            v.iconFolderOpen = Icon("folder_open");
            v.iconFile = Icon("file");
            v.theme.Capture();
            IETMUIBuilder.TreeFolder = v.iconFolder; IETMUIBuilder.TreeFolderOpen = v.iconFolderOpen; IETMUIBuilder.TreeFile = v.iconFile;

            // ---- colours + font
            foreach (var g in v.GetComponentsInChildren<Graphic>(true))
            {
                if (g is Text t)
                {
                    t.color = Near(t.color, legacyBg) ? new Color(1, 1, 1, t.color.a) : Remap(t.color, from, to);
                    if (font != null) t.font = font;
                }
                else g.color = Remap(g.color, from, to);
                EditorUtility.SetDirty(g);
            }
            foreach (var o in v.GetComponentsInChildren<Shadow>(true)) { o.effectColor = Remap(o.effectColor, from, to); EditorUtility.SetDirty(o); }
            foreach (var b in v.GetComponentsInChildren<IETMButton>(true))
            {
                b.normal = Remap(b.normal, from, to);
                b.hover = Remap(b.hover, from, to);
                b.active = Remap(b.active, from, to);
                b.labelColor = Near(b.labelColor, legacyBg) ? Color.white : Remap(b.labelColor, from, to);
                b.labelOnColor = Color.white;
                b.Apply();
                EditorUtility.SetDirty(b);
            }
            foreach (var f in v.GetComponentsInChildren<InputField>(true))
            {
                f.caretColor = Theme.Accent; f.selectionColor = Theme.Accent.A(0.3f);
                EditorUtility.SetDirty(f);
            }

            // ---- bordered see-through panels -> opaque (keeps text in them readable)
            UIKit.FlattenBorderedPanels(v.root, Theme.Bg);

            // ---- layout updates (edge collapse tabs, camera view buttons, header subtitle)
            IETMSceneBuilder.WithSprites(() => UpdateLayout(v));

            // ---- icons on buttons (by command)
            foreach (var b in v.GetComponentsInChildren<IETMButton>(true))
            {
                if (!CommandIcons.TryGetValue(b.command, out var map)) continue;
                var sp = Icon(map.icon);
                if (sp == null) continue;
                if (b.label != null && !map.only) b.label.text = StripArrows(b.label.text);
                bool handle = b.command == IETMCommand.ToggleLeftPanel || b.command == IETMCommand.ToggleRightPanel;
                UIKit.AttachIcon(b, sp, map.only, handle ? 12f : 16f);
                if (b == v.rightHandle && b.icon != null) b.icon.rectTransform.localEulerAngles = new Vector3(0, 0, 180);
                EditorUtility.SetDirty(b);
            }

            // ---- logo (top bar)
            var logo = v.root != null ? v.root.Find("Top Bar/Row/Logo") : null;
            if (logo != null && v.logo != null)
            {
                var img = logo.GetComponent<Image>();
                img.sprite = v.logo; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
                var le = logo.GetComponent<LayoutElement>();
                if (le != null) { le.preferredWidth = 150; le.minWidth = 150; le.preferredHeight = 30; le.minHeight = 30; }
                var lt = logo.Find("Logo Text");
                if (lt != null) lt.gameObject.SetActive(false);
                EditorUtility.SetDirty(img);
            }

            // ---- breadcrumb home icon
            if (v.breadcrumb != null) AddLeadingIcon(v.breadcrumb, Icon("breadcrumb_home"), Theme.Muted, 13f, 14f);

            // ---- procedure warning / caution icons
            if (v.procWarnText != null) AddLeadingIcon(v.procWarnText, Icon("warning"), Theme.Danger, 16f, 12f);
            if (v.procCautionText != null) AddLeadingIcon(v.procCautionText, Icon("caution"), Theme.Warn, 16f, 12f);

            // ---- structure tree template: folder / file icon
            if (v.tplTreeRow != null && v.tplTreeRow.Get<Image>("icon") == null)
            {
                var exp = v.tplTreeRow.Get<IETMButton>("expand");
                var ico = UIKit.Rect("Icon", v.tplTreeRow.transform).gameObject.AddComponent<Image>();
                ico.raycastTarget = false; ico.preserveAspect = true; ico.color = Theme.Muted;
                ico.LE(14, 14, -1, -1, 14, 14);
                if (exp != null) ico.transform.SetSiblingIndex(exp.transform.GetSiblingIndex() + 1);
                v.tplTreeRow.Set("icon", ico);
                EditorUtility.SetDirty(v.tplTreeRow);
            }

            EditorUtility.SetDirty(v);
        }

        /// <summary>Brings a UI built with an earlier version up to date (idempotent).</summary>
        static void UpdateLayout(IETMView v)
        {
            // panel toggles move from the top bar to the panel edges
            foreach (var b in v.GetComponentsInChildren<IETMButton>(true))
            {
                if (b.command != IETMCommand.ToggleLeftPanel && b.command != IETMCommand.ToggleRightPanel) continue;
                if (b == v.leftHandle || b == v.rightHandle) continue;
                if (v.leftPanel != null && b.transform.IsChildOf(v.leftPanel)) continue;
                if (v.rightPanel != null && b.transform.IsChildOf(v.rightPanel)) continue;
                b.gameObject.SetActive(false); // old "◄ Panel" / "Panel ►" buttons in the top bar
            }
            if (v.leftHandle == null && v.leftPanel != null) v.leftHandle = IETMUIBuilder.BuildPanelHandle(v.leftPanel, true);
            if (v.rightHandle == null && v.rightPanel != null) v.rightHandle = IETMUIBuilder.BuildPanelHandle(v.rightPanel, false);

            // header subtitle no longer runs into the buttons
            IETMUIBuilder.FitSubtitle(v.publicationSubtitle);
            var titles = v.publicationTitle != null ? v.publicationTitle.transform.parent : null;
            var tle = titles != null ? titles.GetComponent<LayoutElement>() : null;
            if (tle != null && tle.preferredWidth < 400) tle.preferredWidth = 400;

            // camera views: Isometric, Top, Bottom, Aft, Forward, Starboard, Port (icon + tooltip)
            Transform panel = null;
            foreach (var b in v.GetComponentsInChildren<IETMButton>(true))
                if (b.command == IETMCommand.ViewIso || b.command == IETMCommand.ViewTop) { panel = b.transform.parent; break; }
            if (panel == null) return;
            int insertAt = -1;
            for (int i = 0; i < panel.childCount; i++)
                if (panel.GetChild(i).GetComponent<IETMButton>() != null) { insertAt = i; break; }
            if (insertAt < 0) insertAt = panel.childCount;
            foreach (var vb in IETMUIBuilder.ViewButtons)
            {
                IETMButton btn = null;
                foreach (Transform c in panel)
                {
                    var cb = c.GetComponent<IETMButton>();
                    if (cb != null && cb.command == vb.command) { btn = cb; break; }
                }
                if (btn == null) btn = UIKit.Button(panel, vb.label, vb.command, BtnStyle.Ghost, 11, -1, 30, vb.tooltip);
                if (!btn.TryGetComponent<UITooltip>(out var tip)) tip = btn.gameObject.AddComponent<UITooltip>();
                tip.text = vb.tooltip;
                var sp = Icon(vb.icon);
                if (sp != null) UIKit.AttachIcon(btn, sp, true, 18f);
                btn.transform.SetSiblingIndex(insertAt++);
                EditorUtility.SetDirty(btn);
            }
        }

        // ================================================================== helpers

        static string StripArrows(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("←", "").Replace("→", "").Trim();
        }

        /// <summary>Puts an icon in front of a text that is stretched inside its parent box.</summary>
        static void AddLeadingIcon(Text text, Sprite sprite, Color color, float size, float left)
        {
            if (sprite == null) return;
            var parent = text.transform.parent;
            var existing = parent.Find(text.name + " Icon");
            Image icon = existing != null ? existing.GetComponent<Image>() : null;
            if (icon == null)
            {
                icon = UIKit.Rect(text.name + " Icon", parent).gameObject.AddComponent<Image>();
                icon.raycastTarget = false;
                var trt = text.rectTransform;
                trt.offsetMin = new Vector2(trt.offsetMin.x + size + 8, trt.offsetMin.y);
            }
            icon.sprite = sprite; icon.preserveAspect = true; icon.color = color;
            icon.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(left, 0), new Vector2(size, size));
            EditorUtility.SetDirty(icon);
        }

        static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        static Color Remap(Color c, Color[] from, Color[] to)
        {
            for (int i = 0; i < from.Length && i < to.Length; i++)
                if (Near(c, from[i])) { var n = to[i]; n.a = c.a; return n; }
            return c;
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Icon(string name) => LoadSprite($"{IconDir}/icon_{name}.png");

        public static Sprite LoadSprite(string path)
        {
            if (cache.TryGetValue(path, out var s) && s != null) return s;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[IETM] Missing UI image: " + path); return null; }
            if (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled || imp.spriteImportMode != SpriteImportMode.Single)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            cache[path] = s;
            return s;
        }
    }
}
