// Editor tools for the HELIOS application shell around the IETM.
//   IETM > HELIOS Shell > Build Shell (IETM inside HELIOS)   - pre-builds the top bar, side menu and pages
//   IETM > HELIOS Shell > Remove Shell                         - back to the stand-alone IETM
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IETM.EditorTools
{
    public static class HeliosShellEditor
    {
        public const string ShellDir = "Assets/IETM/UI/Shell";

        [MenuItem("IETM/HELIOS Shell/Build Shell (IETM inside HELIOS)", priority = 40)]
        public static void BuildMenu()
        {
            var app = Object.FindAnyObjectByType<IETMApp>();
            if (app == null || app.view == null)
            {
                EditorUtility.DisplayDialog("IETM", "No pre-built IETM UI in the open scene.\nOpen the Landing Gear IETM scene first.", "OK");
                return;
            }
            BuildShell(app, true);
        }

        [MenuItem("IETM/HELIOS Shell/Remove Shell", priority = 41)]
        public static void RemoveMenu()
        {
            var app = Object.FindAnyObjectByType<IETMApp>();
            if (app == null || app.view == null) return;
            RemoveShell(app);
        }

        /// <summary>Builds (or rebuilds) the HELIOS shell on the IETM Canvas and adds the HeliosShell controller.</summary>
        public static HeliosShellView BuildShell(IETMApp app, bool select)
        {
            var v = app.view;
            Undo.RegisterFullObjectHierarchyUndo(v.gameObject, "Build HELIOS Shell");
            Undo.RecordObject(v, "Build HELIOS Shell");

            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/IETM/Resources/IETM/helios_shell.json");
            var data = HeliosShellData.Load(dataAsset);
            var db = IETMDatabase.Load(app.dataFile != null ? app.dataFile : Resources.Load<TextAsset>("IETM/landing_gear_ietm"));

            IETMHeliosTheme.ApplyToView(v);   // theme + latest IETM layout (header, view bar) first

            HeliosShellView sv = null;
            var prevFont = UIKit.FontOverride;
            IETMSceneBuilder.WithSprites(() =>
            {
                if (v.font != null) UIKit.FontOverride = v.font;
                HeliosShellBuilder.SpriteLoader = LoadShellSprite;
                try { sv = HeliosShellBuilder.Build(v, data, db); }
                finally { HeliosShellBuilder.SpriteLoader = null; UIKit.FontOverride = prevFont; }
            });

            // the HELIOS top bar carries the logo: hide the IETM's own one
            var logo = v.root.Find("Top Bar/Row/Logo");
            if (logo != null) logo.gameObject.SetActive(false);

            var ctl = Object.FindAnyObjectByType<HeliosShell>();
            if (ctl == null) ctl = Undo.AddComponent<HeliosShell>(app.gameObject);
            Undo.RecordObject(ctl, "Build HELIOS Shell");
            ctl.app = app; ctl.shell = sv; ctl.shellData = dataAsset;

            EditorUtility.SetDirty(v); EditorUtility.SetDirty(ctl);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            if (select) Selection.activeGameObject = sv.gameObject;
            Debug.Log("[IETM] HELIOS shell built: 3D View · IETM (aircraft > system > landing gear IETM).");
            return sv;
        }

        public static void RemoveShell(IETMApp app)
        {
            var v = app.view;
            var canvas = v.canvas.transform;
            Undo.RegisterFullObjectHierarchyUndo(v.gameObject, "Remove HELIOS Shell");
            Undo.RecordObject(v, "Remove HELIOS Shell");
            var frame = canvas.Find("IETM Frame");
            if (frame != null)
            {
                var kids = new System.Collections.Generic.List<Transform>();
                foreach (Transform t in frame) kids.Add(t);
                foreach (var t in kids) t.SetParent(canvas, false);
                Object.DestroyImmediate(frame.gameObject);
            }
            foreach (var n in new[] { "HELIOS Shell", "HELIOS Backdrop" })
            {
                var t = canvas.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            v.root = (RectTransform)canvas;
            var logo = v.root.Find("Top Bar/Row/Logo");
            if (logo != null) logo.gameObject.SetActive(true);
            var ctl = Object.FindAnyObjectByType<HeliosShell>();
            if (ctl != null) Undo.DestroyObjectImmediate(ctl);
            app.inputSuspended = false;
            EditorUtility.SetDirty(v);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
        }

        static Sprite LoadShellSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name == "logo") return IETMHeliosTheme.LoadSprite(IETMHeliosTheme.LogoPath);
            string png = $"{ShellDir}/{name}.png", jpg = $"{ShellDir}/{name}.jpg";
            if (AssetImporter.GetAtPath(png) != null) return IETMHeliosTheme.LoadSprite(png);
            if (AssetImporter.GetAtPath(jpg) != null) return IETMHeliosTheme.LoadSprite(jpg);
            if (name.StartsWith("icon_")) return IETMHeliosTheme.Icon(name.Substring(5));
            Debug.LogWarning("[IETM] Missing shell image: " + name);
            return null;
        }
    }
}
