// HELIOS: everything in the scene before Play (WebGL: nothing is loaded or built when the app starts).
// Runs by itself when HELIOS.unity is open and something below is not done yet (and from Tools > HELIOS > Set Up Scene):
//   1. UI: the project's UI is Assets/HELIOS/Prefabs/HELIOS UI.prefab (Figma UI: Aircraft Hierarchy, View Tools,
//      cards...). Any other HELIOS UI in the scene (e.g. the default UI from Resources/HELIOS UI.prefab, which
//      Tools > HELIOS > Build UI regenerates) is replaced by it; HELIOS App > UI points to it.
//   2. Aircraft: Assets/3DModels/F16_V01.glb is placed in the scene under HELIOS > "HELIOS Model", centred and scaled
//      as the app would do at Play (Aircraft Model points to it, so Play uses it as it is). Any other placed model is removed.
//   3. Background camera (backdrop) and Sensor Focus are scene objects; the Main Camera is at the home view (the
//      Game view shows what Play shows); Unity's default Directional Light (doubles the HELIOS light rig) is removed.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Helios.EditorTools
{
    [InitializeOnLoad]
    public static class HeliosSceneSetup
    {
        const string ScenePath = "Assets/HELIOS/Scenes/HELIOS.unity";
        public const string UiPrefabPath = "Assets/HELIOS/Prefabs/HELIOS UI.prefab";
        public const string ModelPath = "Assets/3DModels/F16_V01.glb";

        static HeliosSceneSetup()
        {
            EditorApplication.delayCall += AutoRun;
            EditorSceneManager.sceneOpened += (s, m) => EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoRun; return; }
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            // the Edit Page drives what is shown while editing (the IETM only on the IETM page); shown first so the
            // aircraft is visible (not left hidden by an IETM preview) before the camera is checked and framed
            var nav = app != null && app.ui != null ? app.ui : Object.FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);
            if (nav != null) nav.ShowEditPage();
            if (app != null)
            {
                MigrateToIsometric(app);
                if (NeedsSetup(app)) Run(app, true);
            }
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public const float IsoYaw = 135f, IsoPitch = 30f;

        /// <summary>The old start angle (120 / 18) becomes the isometric one once; a value you set yourself is kept.</summary>
        static void MigrateToIsometric(HeliosApp app)
        {
            if (!Mathf.Approximately(app.startYaw, 120f) || !Mathf.Approximately(app.startPitch, 18f)) return;
            Undo.RecordObject(app, "HELIOS isometric view");
            app.startYaw = IsoYaw; app.startPitch = IsoPitch;
            EditorUtility.SetDirty(app);
        }

        [MenuItem("Tools/HELIOS/Isometric Camera View (Editor + Play)", false, 7)]
        static void IsometricMenu()
        {
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app == null) { EditorUtility.DisplayDialog("HELIOS", "No HELIOS App in the scene.", "OK"); return; }
            var nav = app.ui != null ? app.ui : Object.FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);
            if (nav != null) nav.ShowEditPage();
            Undo.RecordObject(app, "HELIOS isometric view");
            app.startYaw = IsoYaw; app.startPitch = IsoPitch;
            EditorUtility.SetDirty(app);
            PoseMainCamera(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            EditorSceneManager.SaveScene(app.gameObject.scene);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log("[HELIOS] Isometric view: Main Camera framed on the F-16 (yaw " + IsoYaw + ", pitch " + IsoPitch + "); Play starts from the same view.");
        }

        [MenuItem("Tools/HELIOS/Set Up Scene (UI, F-16, nothing loaded at Play)", false, 6)]
        static void Menu()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app == null) { EditorUtility.DisplayDialog("HELIOS", "No HELIOS App in the scene.", "OK"); return; }
            Run(app, true);
        }

        [MenuItem("Tools/HELIOS/Frame Aircraft In Game View", false, 8)]
        static void FrameMenu()
        {
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app != null) { PoseMainCamera(app); EditorSceneManager.MarkSceneDirty(app.gameObject.scene); }
        }

        // ================================================================ checks

        static string SourcePath(GameObject go)
        {
            if (go == null) return null;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            return src != null ? AssetDatabase.GetAssetPath(src) : null;
        }

        static HeliosNavigator[] Navigators() =>
            Object.FindObjectsByType<HeliosNavigator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        static bool UiOk()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath) == null) return true;      // nothing to put in
            var navs = Navigators();
            return navs.Length == 1 && SourcePath(navs[0].transform.root.gameObject) == UiPrefabPath;
        }

        static bool ModelOk(HeliosApp app)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null) return app.aircraftModel == null || HeliosModel.IsPlaced(app.aircraftModel);
            return HeliosModel.IsPlaced(app.aircraftModel) && SourcePath(app.aircraftModel) == ModelPath;
        }

        static bool NeedsSetup(HeliosApp app)
        {
            var nav = app.ui != null ? app.ui : Object.FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);
            return !UiOk() || !ModelOk(app) || !DoubleSidedOk(app) || app.backgroundCamera == null
                   || (nav != null && nav.GetComponent<HeliosSensorFocus>() == null) || !CameraAtHome(app) || DuplicateLight() != null || !BackdropOk(app) || app.sceneLookVersion < 2 || !EngineShared(app) || (Camera.main != null && Camera.main.GetComponent<HeliosLensShift>() == null);
        }

        // ================================================================ set up

        public static void Run(HeliosApp app, bool save)
        {
            Undo.RecordObject(app, "HELIOS scene setup");
            SwapUi(app);
            SwapModel(app);
            PlaceModel(app);
            ShareEngine(app);
            MakeDoubleSided(app);
            EnsureBackgroundCamera(app);
            AttachBackdrop(app);
            Brighten(app);
            PoseMainCamera(app);
            RemoveDuplicateLight();
            var nav = app.ui != null ? app.ui : Object.FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);
            if (nav != null && nav.GetComponent<HeliosSensorFocus>() == null) Undo.AddComponent<HeliosSensorFocus>(nav.gameObject);
            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            if (save) EditorSceneManager.SaveScene(app.gameObject.scene);
        }

        /// <summary>The scene's HELIOS UI = an instance of the project's UI prefab (any other HELIOS UI is removed).</summary>
        static void SwapUi(HeliosApp app)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (prefab == null) { Debug.LogWarning("[HELIOS] " + UiPrefabPath + " not found: UI left as it is."); return; }
            var navs = Navigators();
            HeliosNavigator keep = null;
            foreach (var n in navs) if (SourcePath(n.transform.root.gameObject) == UiPrefabPath) { keep = n; break; }
            if (keep == null)
            {
                int index = navs.Length > 0 ? navs[0].transform.root.GetSiblingIndex() : -1;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, app.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(inst, "HELIOS scene setup");
                inst.name = "HELIOS UI";
                if (index >= 0) inst.transform.SetSiblingIndex(index);
                keep = inst.GetComponentInChildren<HeliosNavigator>(true);
                Debug.Log("[HELIOS] UI set to " + UiPrefabPath + ".");
            }
            foreach (var n in navs)
            {
                if (n == null || n == keep) continue;
                var root = n.transform.root.gameObject;
                if (keep != null && root == keep.transform.root.gameObject) continue;
                Debug.Log("[HELIOS] Removed the old UI '" + root.name + "' (" + (SourcePath(root) ?? "scene object") + ").");
                Undo.DestroyObjectImmediate(root);
            }
            app.ui = keep;
        }

        /// <summary>Aircraft Model = F16_V01.glb: any other placed model is removed (the new one is placed below).</summary>
        static void SwapModel(HeliosApp app)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (asset == null || ModelOk(app)) return;
            var rootT = app.transform.Find(HeliosModel.PlacedRootName);
            if (rootT != null)
                for (int i = rootT.childCount - 1; i >= 0; i--)
                {
                    var c = rootT.GetChild(i).gameObject;
                    Debug.Log("[HELIOS] Removed the old aircraft '" + c.name + "' (" + (SourcePath(c) ?? "scene object") + ").");
                    Undo.DestroyObjectImmediate(c);
                }
            app.aircraftModel = asset;
        }

        /// <summary>Places the Aircraft Model like HeliosModel.Build does at Play (centred, longest side = Size).</summary>
        static void PlaceModel(HeliosApp app)
        {
            var src = app.aircraftModel;
            if (src == null || HeliosModel.IsPlaced(src)) return;
            var rootT = app.transform.Find(HeliosModel.PlacedRootName);
            if (rootT == null)
            {
                var rg = new GameObject(HeliosModel.PlacedRootName);
                Undo.RegisterCreatedObjectUndo(rg, "HELIOS scene setup");
                rootT = rg.transform;
                rootT.SetParent(app.transform, false);
            }
            rootT.localPosition = Vector3.zero; rootT.localRotation = Quaternion.identity; rootT.localScale = Vector3.one;
            GameObject inst;
            if (src.scene.IsValid()) { inst = src; Undo.SetTransformParent(inst.transform, rootT, "HELIOS scene setup"); }
            else
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(src, rootT);       // stays linked to the model file
                Undo.RegisterCreatedObjectUndo(inst, "HELIOS scene setup");
            }
            if (!inst.activeSelf) inst.SetActive(true);
            var b = ModelBounds(inst);
            if (b.HasValue && b.Value.size.sqrMagnitude > 1e-8f)
            {
                inst.transform.position -= b.Value.center;
                float longest = Mathf.Max(b.Value.size.x, Mathf.Max(b.Value.size.y, b.Value.size.z));
                rootT.localScale = Vector3.one * (longest > 1e-5f ? HeliosModel.Size / longest : 1f);
            }
            app.aircraftModel = inst;
            Debug.Log("[HELIOS] Aircraft placed in the scene: " + inst.name + " (HELIOS > " + HeliosModel.PlacedRootName + "). Nothing is loaded at Play.");
        }

        // ================================================================ double-sided materials

        public const string DoubleSidedShader = "HELIOS/Double Sided Lit (Built-in)";
        const string MaterialRoot = "Assets/HELIOS/Materials";

        static IEnumerable<Renderer> ModelRenderers(HeliosApp app)
        {
            if (app.aircraftModel == null || !HeliosModel.IsPlaced(app.aircraftModel)) yield break;
            foreach (var r in app.aircraftModel.GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer || r is SkinnedMeshRenderer) yield return r;
        }

        static bool DoubleSidedOk(HeliosApp app)
        {
            if (Shader.Find(DoubleSidedShader) == null) return true;
            foreach (var r in ModelRenderers(app))
                foreach (var m in r.sharedMaterials) if (m != null && m.shader.name != DoubleSidedShader && !KeepMaterial(m)) return false;
            return true;
        }

        /// <summary>Every part of the aircraft gets a double-sided copy of its material (saved under
        /// Assets/HELIOS/Materials/&lt;model&gt;): both sides drawn and lit from the camera side, in every mode.</summary>
        static void MakeDoubleSided(HeliosApp app)
        {
            var shader = Shader.Find(DoubleSidedShader);
            if (shader == null) { Debug.LogWarning("[HELIOS] Shader " + DoubleSidedShader + " not found: materials left as they are."); return; }
            string model = SourcePath(app.aircraftModel);
            string defFolder = MaterialRoot + "/" + (string.IsNullOrEmpty(model) ? app.aircraftModel.name : Path.GetFileNameWithoutExtension(model));
            if (!AssetDatabase.IsValidFolder(MaterialRoot)) AssetDatabase.CreateFolder("Assets/HELIOS", "Materials");
            string folder = defFolder;
            var map = new Dictionary<Material, Material>();
            int parts = 0;
            foreach (var r in ModelRenderers(app))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null || src.shader == shader || KeepMaterial(src)) continue;
                    if (!map.TryGetValue(src, out var m))
                    {
                        // one folder per source model (F16_V01, engine, ...): material names repeat between models
                        string srcPath = AssetDatabase.GetAssetPath(src);
                        folder = string.IsNullOrEmpty(srcPath) || !srcPath.StartsWith("Assets/") ? defFolder : MaterialRoot + "/" + Safe(Path.GetFileNameWithoutExtension(srcPath));
                        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(MaterialRoot, Path.GetFileName(folder));
                        string file = folder + "/" + Safe(src.name) + ".mat";
                        m = AssetDatabase.LoadAssetAtPath<Material>(file);
                        if (m == null) { m = DoubleSidedOf(src, shader); AssetDatabase.CreateAsset(m, file); }
                        map[src] = m;
                    }
                    mats[i] = m; changed = true;
                }
                if (!changed) continue;
                Undo.RecordObject(r, "HELIOS double-sided materials");
                r.sharedMaterials = mats;
                parts++;
            }
            AssetDatabase.SaveAssets();
            if (parts > 0) Debug.Log("[HELIOS] Double-sided materials: " + map.Count + " materials (" + MaterialRoot + ") on " + parts + " parts.");
        }

        /// <summary>A see-through material you assigned yourself (e.g. the canopy glass, Assets/3DModels/Glas.mat) is kept
        /// as it is: transparent queue or RenderType Transparent. Only opaque / cut-out materials get double-sided copies.</summary>
        static bool KeepMaterial(Material m)
        {
            return m != null && (m.renderQueue >= 2500 || m.GetTag("RenderType", false) == "Transparent");
        }

        static string Safe(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return string.IsNullOrEmpty(s) ? "Material" : s;
        }

        static Material DoubleSidedOf(Material src, Shader shader)
        {
            var m = new Material(shader) { name = src.name };
            string tex = FirstTexture(src, "baseColorTexture", "_BaseMap", "_MainTex");
            if (tex != null)
            {
                m.SetTexture("_MainTex", src.GetTexture(tex));
                m.SetTextureScale("_MainTex", src.GetTextureScale(tex));
                m.SetTextureOffset("_MainTex", src.GetTextureOffset(tex));
            }
            string nrm = FirstTexture(src, "normalTexture", "_BumpMap");
            if (nrm != null) m.SetTexture("_BumpMap", src.GetTexture(nrm));
            string col = First(src, "baseColorFactor", "_BaseColor", "_Color");
            if (col != null) m.SetColor("_Color", src.GetColor(col));
            string met = First(src, "metallicFactor", "_Metallic");
            if (met != null) m.SetFloat("_Metallic", src.GetFloat(met));
            if (src.HasProperty("roughnessFactor")) m.SetFloat("_Glossiness", 1f - src.GetFloat("roughnessFactor"));
            else { string sm = First(src, "_Smoothness", "_Glossiness"); if (sm != null) m.SetFloat("_Glossiness", src.GetFloat(sm)); }
            string emc = First(src, "emissiveFactor", "_EmissionColor");
            if (emc != null) m.SetColor("_EmissionColor", src.GetColor(emc));
            string emt = FirstTexture(src, "emissiveTexture", "_EmissionMap");
            if (emt != null) m.SetTexture("_EmissionMap", src.GetTexture(emt));
            bool cutout = src.IsKeywordEnabled("_ALPHATEST_ON") || src.GetTag("RenderType", false) == "TransparentCutout";
            string cut = First(src, "alphaCutoff", "_Cutoff");
            m.SetFloat("_Cutoff", cutout && cut != null ? src.GetFloat(cut) : 0f);
            return m;
        }

        static string First(Material m, params string[] names) { foreach (var n in names) if (m.HasProperty(n)) return n; return null; }
        static string FirstTexture(Material m, params string[] names) { foreach (var n in names) if (m.HasProperty(n) && m.GetTexture(n) != null) return n; return null; }

        static Bounds? ModelBounds(GameObject go)
        {
            bool any = false; var b = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b : (Bounds?)null;
        }

        static void EnsureBackgroundCamera(HeliosApp app)
        {
            if (app.backgroundCamera != null) return;
            var t = app.transform.Find("HELIOS Background Camera");
            GameObject go;
            if (t != null) go = t.gameObject;
            else
            {
                go = new GameObject("HELIOS Background Camera");
                Undo.RegisterCreatedObjectUndo(go, "HELIOS scene setup");
                go.transform.SetParent(app.transform, false);
            }
            var c = go.GetComponent<Camera>();
            if (c == null) c = Undo.AddComponent<Camera>(go);
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = HeliosTheme.Current != null ? HeliosTheme.Current.background : new Color32(8, 17, 46, 255);
            c.cullingMask = 1 << 5;
            var main = Camera.main;
            c.depth = (main != null ? main.depth : -1f) - 10f;
            app.backgroundCamera = c;
        }

        // ================================================================ the engine inside the F-16 (one model, also the IETM's)

        static IETM.IETMPublication EnginePublication(out IETM.IETMApp ietm)
        {
            ietm = Object.FindAnyObjectByType<IETM.IETMApp>(FindObjectsInactive.Include);
            if (ietm == null || ietm.publications == null) return null;
            foreach (var p in ietm.publications) if (p != null && p.id == "engine" && p.model != null) return p;
            return null;
        }

        static bool EngineShared(HeliosApp app)
        {
            var pub = EnginePublication(out _);
            if (pub == null || app.aircraftModel == null || !HeliosModel.IsPlaced(app.aircraftModel)) return true;
            return pub.Shared && pub.model.transform.IsChildOf(app.aircraftModel.transform) && pub.model.activeSelf;
        }

        /// <summary>The IETM engine, placed by hand where the F-16's engine is, becomes part of the aircraft (child of the
        /// F-16, same place in the world): shown in the 3D view, in the Aircraft Hierarchy (Engine), selectable. Its pose in
        /// the IETM is kept on the publication; opening the engine IETM moves this same object there and back.</summary>
        static void ShareEngine(HeliosApp app)
        {
            var pub = EnginePublication(out var ietm);
            if (pub == null || app.aircraftModel == null || !HeliosModel.IsPlaced(app.aircraftModel)) return;
            var t = pub.model.transform;
            if (!t.IsChildOf(app.aircraftModel.transform))
            {
                Undo.RecordObject(ietm, "HELIOS engine in the F-16");
                pub.ietmParent = t.parent;
                pub.ietmLocalPosition = t.localPosition; pub.ietmLocalRotation = t.localRotation; pub.ietmLocalScale = t.localScale;
                Undo.SetTransformParent(t, app.aircraftModel.transform, "HELIOS engine in the F-16");     // keeps its place in the world
                EditorUtility.SetDirty(ietm);
                Debug.Log("[HELIOS] Engine moved into the F-16 (" + app.aircraftModel.name + "); the engine IETM uses the same object (pose under '" + pub.ietmParent.name + "' kept).");
            }
            else if (pub.ietmParent == null)
                Debug.LogWarning("[HELIOS] The engine is already inside the F-16 but its IETM place is not set: set IETM App > Publications > engine > Ietm Parent (Animated Model Pivot) and the pose.");
            if (!pub.model.activeSelf) { Undo.RecordObject(pub.model, "HELIOS engine in the F-16"); pub.model.SetActive(true); }
        }

        // ================================================================ brightness

        [MenuItem("Tools/HELIOS/Scene Brightness", false, 9)]
        static void BrightnessMenu()
        {
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app == null) return;
            var hb = Object.FindAnyObjectByType<HeliosBrightness>(FindObjectsInactive.Include);
            if (hb == null) { app.sceneLookVersion = 0; Brighten(app); EditorSceneManager.MarkSceneDirty(app.gameObject.scene); hb = Object.FindAnyObjectByType<HeliosBrightness>(FindObjectsInactive.Include); }
            if (hb != null) { Selection.activeObject = hb.gameObject; EditorGUIUtility.PingObject(hb.gameObject); }
        }

        const float OldLightBoost = 1.35f, OldAmbientBoost = 1.4f;

        /// <summary>A HeliosBrightness on HELIOS Aircraft Lights: Brightness / Ambient sliders over the lights' designed
        /// intensities and the scene's ambient colours (the earlier one-off x1.35 / x1.4 is taken out of the base).</summary>
        static void Brighten(HeliosApp app)
        {
            if (app.sceneLookVersion >= 2 && Object.FindAnyObjectByType<HeliosBrightness>(FindObjectsInactive.Include) != null) return;
            bool boosted = app.sceneLookVersion == 1;
            var host = GameObject.Find("HELIOS Aircraft Lights");
            if (host == null) host = app.gameObject;
            var hb = host.GetComponent<HeliosBrightness>();
            if (hb == null) hb = Undo.AddComponent<HeliosBrightness>(host);
            Undo.RecordObject(hb, "HELIOS brightness");
            hb.lights.Clear();
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.gameObject.scene == app.gameObject.scene)
                    hb.lights.Add(new HeliosBrightness.LightBase { light = l, intensity = boosted ? l.intensity / OldLightBoost : l.intensity });
            float k = boosted ? 1f / OldAmbientBoost : 1f;
            hb.baseSky = RenderSettings.ambientSkyColor * k; hb.baseEquator = RenderSettings.ambientEquatorColor * k;
            hb.baseGround = RenderSettings.ambientGroundColor * k; hb.baseFlat = RenderSettings.ambientLight * k;
            hb.baseSky.a = hb.baseEquator.a = hb.baseGround.a = hb.baseFlat.a = 1f;
            hb.brightness = 1.6f; hb.ambient = 1.6f;
            hb.Apply();
            EditorUtility.SetDirty(hb);
            Undo.RecordObject(app, "HELIOS brightness");
            app.sceneLookVersion = 2;
            EditorUtility.SetDirty(app);
            Debug.Log("[HELIOS] Scene Brightness on '" + host.name + "': " + hb.lights.Count + " lights, Brightness 1.6, Ambient 1.6 (change the sliders there).");
        }

        // ================================================================ backdrop

        static HeliosBackdrop FindBackdrop(HeliosApp app)
        {
            var bd = app.ui != null ? app.ui.transform.root.GetComponentInChildren<HeliosBackdrop>(true) : null;
            return bd != null ? bd : Object.FindAnyObjectByType<HeliosBackdrop>(FindObjectsInactive.Include);
        }

        /// <summary>A Screen Space - Overlay backdrop is drawn over every camera, so it hid the aircraft in the Editor;
        /// drawn by the background camera (as at Play) it sits behind the aircraft.</summary>
        static bool BackdropOk(HeliosApp app)
        {
            var bd = FindBackdrop(app);
            if (bd == null || app.backgroundCamera == null) return true;
            var c = bd.GetComponent<Canvas>();
            return c == null || (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == app.backgroundCamera);
        }

        static void AttachBackdrop(HeliosApp app)
        {
            var bd = FindBackdrop(app);
            if (bd == null || app.backgroundCamera == null) return;
            var c = bd.GetComponent<Canvas>();
            if (c == null) return;
            Undo.RecordObject(c, "HELIOS scene setup"); Undo.RecordObject(bd.gameObject, "HELIOS scene setup");
            bd.Attach(app.backgroundCamera);
            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bd.gameObject);
            Debug.Log("[HELIOS] Backdrop drawn by the HELIOS Background Camera (behind the aircraft) in the Editor too.");
        }

        // ================================================================ camera, lights

        static bool CameraAtHome(HeliosApp app)
        {
            var cam = Camera.main;
            if (cam == null || app.aircraftModel == null || !HeliosModel.IsPlaced(app.aircraftModel) || !app.aircraftModel.activeInHierarchy) return true;   // hidden (IETM page preview)
            var b = ModelBounds(app.aircraftModel);
            if (!b.HasValue) return true;
            app.HomePose(b.Value, cam.aspect > 0.1f ? cam.aspect : 16f / 9f, out var pos, out var rot);   // same aspect as the Game view (HeliosLensShift)
            return (cam.transform.position - pos).sqrMagnitude < 0.25f && Quaternion.Angle(cam.transform.rotation, rot) < 2f
                   && Mathf.Approximately(cam.fieldOfView, app.fieldOfView) && cam.clearFlags == CameraClearFlags.Depth;
        }

        static void PoseMainCamera(HeliosApp app)
        {
            var cam = Camera.main;
            if (cam == null || app.aircraftModel == null || !app.aircraftModel.activeInHierarchy) return;
            var b = ModelBounds(app.aircraftModel);
            if (!b.HasValue) return;
            Undo.RecordObject(cam.transform, "HELIOS scene setup"); Undo.RecordObject(cam, "HELIOS scene setup");
            app.HomePose(b.Value, cam.aspect > 0.1f ? cam.aspect : 16f / 9f, out var pos, out var rot);   // same aspect as the Game view (HeliosLensShift)
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = app.fieldOfView;
            cam.clearFlags = CameraClearFlags.Depth;            // the background camera draws the backdrop (as at Play)
            cam.cullingMask &= ~(1 << 5);                       // not the UI layer
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 1000f;
            cam.orthographic = false;
            cam.allowHDR = false;                               // as at Play (no post effects; saves an HDR buffer in WebGL)
            if (cam.GetComponent<HeliosLensShift>() == null) Undo.AddComponent<HeliosLensShift>(cam.gameObject);   // pages' Model Area (Editor + Play)
            Debug.Log("[HELIOS] Main Camera framed on the aircraft (yaw " + app.startYaw + ", pitch " + app.startPitch + ").");
        }

        static GameObject DuplicateLight()
        {
            var main = Camera.main;
            if (main == null || main.GetComponentInChildren<Light>(true) == null) return null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.gameObject.name == "Directional Light" && l.transform.parent == null && l.transform.childCount == 0)
                    return l.gameObject;
            return null;
        }

        static void RemoveDuplicateLight()
        {
            var go = DuplicateLight();
            if (go == null) return;
            Undo.DestroyObjectImmediate(go);
            Debug.Log("[HELIOS] Removed the default Directional Light (the HELIOS light rig lights the aircraft).");
        }
    }
}
