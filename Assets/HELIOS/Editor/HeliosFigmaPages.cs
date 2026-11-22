// HELIOS - Tools > HELIOS > Apply Figma Pages (Dashboard, Sensors, Health, Asset Integrity).
// Rebuilds only those four pages inside Assets/HELIOS/Resources/HELIOS UI.prefab from HeliosPagesFigma.cs, so they
// are in the prefab (visible in the Editor before Play). The top bar, the menu and its order, the IETM page, the
// Maintenance page and every other edit in the prefab are kept.
// It runs by itself once, the first time Unity opens the project after this file arrives (when the Dashboard page
// has no "Telemetry Strip" yet); afterwards only from the menu.
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Helios.EditorTools
{
    [InitializeOnLoad]
    public static class HeliosFigmaPages
    {
        const string PrefabPath = "Assets/HELIOS/Resources/HELIOS UI.prefab";
        const string ArtDir = "Assets/HELIOS/Art", IconDir = ArtDir + "/Icons";
        const string SessionKey = "HELIOS.FigmaPages.Checked";

        static HeliosFigmaPages() { EditorApplication.delayCall += AutoApply; }

        static void AutoApply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += AutoApply; return; }
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || HasFigmaPages(prefab)) return;
            if (Apply()) Debug.Log("[HELIOS] The Dashboard, Sensors, Health and Asset Integrity pages were updated to the new Figma design in " + PrefabPath + ".");
        }

        static bool HasFigmaPages(GameObject prefab)
        {
            foreach (var p in prefab.GetComponentsInChildren<HeliosPage>(true))
                if (p.pageId == HeliosData.Menu[0]) return p.transform.Find(HeliosUiFactory.FigmaMarker) != null;
            return false;
        }

        [MenuItem("Tools/HELIOS/Apply Figma Pages (Dashboard, Sensors, Health, Asset Integrity)", false, 7)]
        static void ApplyMenu()
        {
            if (!EditorUtility.DisplayDialog("Apply the Figma pages?",
                    "Rebuilds the Dashboard, Sensors, Health and Asset Integrity pages in " + PrefabPath +
                    " from the updated Figma design.\n\nEverything else in the prefab is kept (top bar, menu, IETM, Maintenance). Changes you made by hand inside these four pages are replaced.",
                    "Apply", "Cancel"))
                return;
            if (Apply()) Debug.Log("[HELIOS] Figma pages applied to " + PrefabPath + ".");
        }

        static bool Apply()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null || TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("[HELIOS] Import TextMeshPro's Essential Resources first, then Tools > HELIOS > Apply Figma Pages.");
                return false;
            }
            var theme = AssetDatabase.LoadAssetAtPath<HeliosTheme>("Assets/HELIOS/Resources/HELIOS Theme.asset");
            if (theme != null) HeliosTheme.Use(theme);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (!HeliosUiFactory.RebuildFigmaPages(root, new Binder()))
                {
                    Debug.LogError("[HELIOS] " + PrefabPath + " has no HELIOS navigator / pages: nothing changed.");
                    return false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[HELIOS] Could not apply the Figma pages: " + ex);
                return false;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Sprites from the art files; listeners saved in the prefab (they show in each button's OnClick list).</summary>
        class Binder : HeliosUiFactory.IBinder, HeliosUiFactory.IVoidBinder
        {
            static string ArtPath(string name) { return name.StartsWith("icon-") ? IconDir + "/" + name.Substring(5) + ".png" : ArtDir + "/" + name + ".png"; }
            public Sprite Sprite(string name)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(name));
                return s != null ? s : HeliosStyle.Sprite(name);
            }
            public TMP_FontAsset Font { get { return HeliosTheme.Current.font; } }
            public void OnClick(Button b, UnityAction<string> action, string arg) { UnityEventTools.AddStringPersistentListener(b.onClick, action, arg); }
            public void OnClick(Button b, UnityAction<int> action, int arg) { UnityEventTools.AddIntPersistentListener(b.onClick, action, arg); }
            public void OnClick(Button b, UnityAction<GameObject> action, GameObject arg) { UnityEventTools.AddObjectPersistentListener<GameObject>(b.onClick, action, arg); }
            public void OnClick(Button b, UnityAction action) { UnityEventTools.AddVoidPersistentListener(b.onClick, action); }
        }
    }
}
