// HELIOS - editor tools (Tools > HELIOS):
//   Build UI            saves the art as PNG files, the Theme asset, the "HELIOS UI" prefab and the HELIOS scene
//                       (runs by itself the first time the package is imported; later it asks before replacing)
//   Open HELIOS Scene, Open UI Prefab, Select Theme, Add HELIOS To Current Scene, Select HELIOS
// After the build you edit the prefab: every element is a normal GameObject; buttons are wired in their OnClick lists.
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Helios.EditorTools
{
    [InitializeOnLoad]
    public static class HeliosMenu
    {
        const string Root = "Assets/HELIOS";
        const string ArtDir = Root + "/Art", IconDir = ArtDir + "/Icons", ShapeDir = ArtDir + "/Shapes", ResDir = Root + "/Resources", SceneDir = Root + "/Scenes";
        const string PrefabPath = ResDir + "/HELIOS UI.prefab", ThemePath = ResDir + "/HELIOS Theme.asset", ScenePath = SceneDir + "/HELIOS.unity";
        const string AutoKey = "HELIOS.v2.AutoBuild";

        // No automatic UI build: the project has its HELIOS UI.prefab (Figma UI). Building it on editor start replaced the
        // project's prefab with the default UI whenever the prefab was not imported yet (right after a pull).
        // Tools > HELIOS > Build UI still makes a default UI on purpose.
        static HeliosMenu() { EditorApplication.delayCall += AutoShapes; }

        static T Find<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // never replace a prefab that is there: right after a pull the file may simply not be imported yet
            // (LoadAssetAtPath = null) and building here would overwrite the project's UI with the default one
            if (File.Exists(PrefabPath) || EditorApplication.isUpdating) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            if (SessionState.GetBool(AutoKey, false)) return;
            SessionState.SetBool(AutoKey, true);
            if (!TmpReady(false)) return;
            if (Build(false) && EditorUtility.DisplayDialog("HELIOS is ready",
                    "The editable UI prefab, the theme, the art and the HELIOS scene have been created in " + Root + ".\n\nOpen the HELIOS scene now?", "Open scene", "Later"))
                OpenScene();
        }

        /// <summary>Adds the shape library once per session if files are missing (never touches the prefab).</summary>
        static void AutoShapes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (SessionState.GetBool(AutoKey + ".Shapes", false)) return;
            SessionState.SetBool(AutoKey + ".Shapes", true);
            bool missing = false;
            foreach (var sp in HeliosShapes.All) if (!File.Exists(ShapeDir + "/" + sp.name + ".png")) { missing = true; break; }
            if (missing) WriteShapes(false);
        }

        // =====================================================================
        // menu
        // =====================================================================

        [MenuItem("Tools/HELIOS/Make Shape Library (Art/Shapes)", false, 5)]
        static void ShapesMenu()
        {
            int n = WriteShapes(true);
            var folder = AssetDatabase.LoadAssetAtPath<Object>(ShapeDir);
            if (folder != null) { Selection.activeObject = folder; EditorGUIUtility.PingObject(folder); }
            Debug.Log("[HELIOS] Shape library: " + (n > 0 ? n + " shapes added to " : "all shapes are already in ") + ShapeDir +
                      ". Drag one onto an Image's Source Image (Image Type: Sliced for stretchable shapes) and tint it with the Image colour or a Themed Colour role.");
        }

        [MenuItem("Tools/HELIOS/Build UI (prefab, theme, art, scene)", false, 1)]
        static void BuildMenu()
        {
            if (!TmpReady(true)) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog("Rebuild the HELIOS UI prefab?",
                    "This replaces " + PrefabPath + " with a fresh copy, so changes you made to the prefab are lost.\n\nYour theme, your art files and the scenes are kept (art files you deleted are drawn again).",
                    "Rebuild", "Cancel"))
                return;
            if (Build(true)) Debug.Log("[HELIOS] Built " + PrefabPath + ". Open it with Tools > HELIOS > Open UI Prefab.");
        }

        [MenuItem("Tools/HELIOS/Open HELIOS Scene", false, 2)]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { if (!TmpReady(true) || !Build(false)) return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/HELIOS/Open UI Prefab", false, 3)]
        static void OpenPrefab()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (p == null) { if (!TmpReady(true) || !Build(false)) return; p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); }
            if (p != null) AssetDatabase.OpenAsset(p);
        }

        [MenuItem("Tools/HELIOS/Select Theme", false, 4)]
        static void SelectTheme()
        {
            var t = EnsureTheme();
            Selection.activeObject = t; EditorGUIUtility.PingObject(t);
        }

        [MenuItem("Tools/HELIOS/Add HELIOS To Current Scene", false, 20)]
        static void AddToScene()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null && (!TmpReady(true) || !Build(false))) return;
            var app = SetUpScene();
            Selection.activeObject = app.gameObject;
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            Debug.Log("[HELIOS] Added. Put your aircraft in the HELIOS component's \"Aircraft Model\" field, then press Play.");
        }

        [MenuItem("Tools/HELIOS/Select HELIOS", false, 21)]
        static void Select()
        {
            var app = Find<HeliosApp>();
            if (app != null) { Selection.activeObject = app.gameObject; EditorGUIUtility.PingObject(app.gameObject); }
            else Debug.Log("[HELIOS] There is no HELIOS in this scene: use Tools > HELIOS > Add HELIOS To Current Scene, or open the HELIOS scene.");
        }

        // =====================================================================
        // building
        // =====================================================================

        /// <summary>TextMeshPro needs its Essential Resources (its default font) before texts can be made.</summary>
        static bool TmpReady(bool tell)
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null && TMP_Settings.defaultFontAsset != null) return true;
            if (tell || EditorUtility.DisplayDialog("HELIOS needs TextMeshPro's Essential Resources",
                    "Click \"Import TMP Essentials\" in the window that opens next, then choose Tools > HELIOS > Build UI.", "OK"))
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            if (tell) Debug.Log("[HELIOS] Import TMP Essential Resources (Window > TextMeshPro), then choose Tools > HELIOS > Build UI again.");
            return false;
        }

        static bool Build(bool replacePrefab)
        {
            try
            {
                Folder(Root, "Art"); Folder(ArtDir, "Icons"); Folder(Root, "Resources"); Folder(Root, "Scenes");
                HeliosTheme.Use(EnsureTheme());
                WriteArt();
                WriteShapes(false);
                if (replacePrefab || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                {
                    var go = HeliosUiFactory.Build(new EditorBinder());
                    PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                    Object.DestroyImmediate(go);
                }
                if (!File.Exists(ScenePath)) MakeScene();
                AssetDatabase.SaveAssets();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[HELIOS] Could not build the UI: " + ex);
                return false;
            }
        }

        static void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }

        static HeliosTheme EnsureTheme()
        {
            Folder(Root, "Resources");
            var t = AssetDatabase.LoadAssetAtPath<HeliosTheme>(ThemePath);
            if (t == null) { t = ScriptableObject.CreateInstance<HeliosTheme>(); AssetDatabase.CreateAsset(t, ThemePath); AssetDatabase.SaveAssets(); }
            return t;
        }

        static string ArtPath(string name) { return name.StartsWith("icon-") ? IconDir + "/" + name.Substring(5) + ".png" : ArtDir + "/" + name + ".png"; }

        /// <summary>Saves every shape and icon as a PNG (files that are already there are kept: they may be yours).</summary>
        static void WriteArt()
        {
            var names = new System.Collections.Generic.List<string>(HeliosStyle.Shapes);
            foreach (var i in HeliosStyle.Icons) names.Add("icon-" + i);
            var fresh = new System.Collections.Generic.List<string>();
            foreach (var n in names)
            {
                string path = ArtPath(n);
                if (File.Exists(path)) continue;
                var tex = HeliosStyle.Draw(n);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                fresh.Add(n);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var n in fresh)
            {
                var ti = AssetImporter.GetAtPath(ArtPath(n)) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spriteBorder = HeliosStyle.Border(n);
                var st = new TextureImporterSettings();
                ti.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(st);
                ti.SaveAndReimport();
            }
        }

        [MenuItem("Tools/HELIOS/Fix Round Ends (pills, capsules, cylinders)", false, 6)]
        static void FixRoundEnds()
        {
            int n = 0;
            if (File.Exists(PrefabPath))
            {
                var root = PrefabUtility.LoadPrefabContents(PrefabPath);
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                    if (HeliosRoundEnds.Wants(img.sprite) && img.GetComponent<HeliosRoundEnds>() == null) { img.gameObject.AddComponent<HeliosRoundEnds>(); n++; }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (img == null || EditorUtility.IsPersistent(img) || !img.gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabInstance(img)) continue;
                if (HeliosRoundEnds.Wants(img.sprite) && img.GetComponent<HeliosRoundEnds>() == null) { Undo.AddComponent<HeliosRoundEnds>(img.gameObject); n++; }
            }
            Debug.Log("[HELIOS] Round Ends added to " + n + " pill, capsule and cylinder images (in " + PrefabPath + " and the open scene). Their ends now stay round at any size.");
        }

        /// <summary>Saves every library shape that is not there yet as a PNG sprite with its 9-slice border.</summary>
        static int WriteShapes(bool listInConsole)
        {
            Folder(Root, "Art"); Folder(ArtDir, "Shapes");
            var fresh = new System.Collections.Generic.List<HeliosShapes.Spec>();
            foreach (var sp in HeliosShapes.All)
            {
                string path = ShapeDir + "/" + sp.name + ".png";
                if (File.Exists(path)) continue;          // yours, or already made: kept
                var tex = HeliosShapes.Draw(sp);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                fresh.Add(sp);
            }
            if (fresh.Count == 0) return 0;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sb = new System.Text.StringBuilder("[HELIOS] Shapes added to " + ShapeDir + ":\n");
            foreach (var sp in fresh)
            {
                string path = ShapeDir + "/" + sp.name + ".png";
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = 100f;
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.filterMode = FilterMode.Bilinear;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.spriteBorder = sp.border;
                    var st = new TextureImporterSettings();
                    ti.ReadTextureSettings(st);
                    st.spriteMeshType = SpriteMeshType.FullRect;
                    ti.SetTextureSettings(st);
                    ti.SaveAndReimport();
                }
                sb.Append("  ").Append(sp.name).Append(sp.border.sqrMagnitude > 0f ? "  (sliced)" : "  (keep square)").Append(": ").Append(sp.use).Append('\n');
            }
            if (listInConsole) Debug.Log(sb.ToString());
            return fresh.Count;
        }

        /// <summary>Sprites from the PNG files and listeners saved in the prefab (they show in each button's OnClick list).</summary>
        class EditorBinder : HeliosUiFactory.IBinder
        {
            public Sprite Sprite(string name)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(name));
                return s != null ? s : HeliosStyle.Sprite(name);
            }
            public TMP_FontAsset Font { get { return HeliosTheme.Current.font; } }
            public void OnClick(Button b, UnityAction<string> action, string arg) { UnityEventTools.AddStringPersistentListener(b.onClick, action, arg); }
            public void OnClick(Button b, UnityAction<int> action, int arg) { UnityEventTools.AddIntPersistentListener(b.onClick, action, arg); }
            public void OnClick(Button b, UnityAction<GameObject> action, GameObject arg) { UnityEventTools.AddObjectPersistentListener<GameObject>(b.onClick, action, arg); }
        }

        // =====================================================================
        // the scene
        // =====================================================================

        static void MakeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath)) { list.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = list.ToArray(); }
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous)) EditorSceneManager.OpenScene(previous);
        }

        /// <summary>Adds what the app needs to the open scene: camera, light, event system, HELIOS and the UI prefab.</summary>
        static HeliosApp SetUpScene()
        {
            if (Camera.main == null)
            {
                var c = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); c.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(c, "HELIOS camera");
            }
            if (Find<Light>() == null)
            {
                var l = new GameObject("Directional Light"); var li = l.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.1f;
                l.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
                Undo.RegisterCreatedObjectUndo(l, "HELIOS light");
            }
            if (Find<EventSystem>() == null)
            {
                var e = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (t != null) e.AddComponent(t); else e.AddComponent<StandaloneInputModule>();
#else
                e.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(e, "HELIOS event system");
            }
            var app = Find<HeliosApp>();
            if (app == null)
            {
                var go = new GameObject("HELIOS");
                app = go.AddComponent<HeliosApp>();
                Undo.RegisterCreatedObjectUndo(go, "HELIOS");
            }
            var nav = Find<HeliosNavigator>();
            if (nav == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    Undo.RegisterCreatedObjectUndo(inst, "HELIOS UI");
                    nav = inst.GetComponentInChildren<HeliosNavigator>(true);
                }
            }
            app.ui = nav;
            return app;
        }
    }
}
