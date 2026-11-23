// HELIOS: everything in the scene before Play (WebGL: nothing is loaded or built when the app starts).
// Runs by itself when HELIOS.unity is open and something below is not done yet (and from Tools > HELIOS > Set Up Scene):
//   1. UI: the project's UI is Assets/HELIOS/Prefabs/HELIOS UI.prefab (Figma UI: Aircraft Hierarchy, View Tools,
//      cards...). Any other HELIOS UI in the scene (e.g. the default UI from Resources/HELIOS UI.prefab, which
//      Tools > HELIOS > Build UI regenerates) is replaced by it; HELIOS App > UI points to it.
//   2. Aircraft: Assets/3DModels/F16_V01.glb is placed in the scene under HELIOS > "HELIOS Model", centred and scaled
//      as the app would do at Play (Aircraft Model points to it, so Play uses it as it is). Any other placed model is removed.
//   3. Background camera (backdrop) and Sensor Focus are scene objects; the Main Camera is at the home view (the
//      Game view shows what Play shows); Unity's default Directional Light (doubles the HELIOS light rig) is removed.
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
            if (app != null && NeedsSetup(app)) Run(app, true);
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
            return !UiOk() || !ModelOk(app) || app.backgroundCamera == null
                   || (nav != null && nav.GetComponent<HeliosSensorFocus>() == null) || !CameraAtHome(app) || DuplicateLight() != null;
        }

        // ================================================================ set up

        public static void Run(HeliosApp app, bool save)
        {
            Undo.RecordObject(app, "HELIOS scene setup");
            SwapUi(app);
            SwapModel(app);
            PlaceModel(app);
            EnsureBackgroundCamera(app);
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

        // ================================================================ camera, lights

        static bool CameraAtHome(HeliosApp app)
        {
            var cam = Camera.main;
            if (cam == null || app.aircraftModel == null || !HeliosModel.IsPlaced(app.aircraftModel)) return true;
            var b = ModelBounds(app.aircraftModel);
            if (!b.HasValue) return true;
            app.HomePose(b.Value, 16f / 9f, out var pos, out var rot);
            return (cam.transform.position - pos).sqrMagnitude < 0.01f && cam.clearFlags == CameraClearFlags.Depth;
        }

        static void PoseMainCamera(HeliosApp app)
        {
            var cam = Camera.main;
            if (cam == null || app.aircraftModel == null) return;
            var b = ModelBounds(app.aircraftModel);
            if (!b.HasValue) return;
            Undo.RecordObject(cam.transform, "HELIOS scene setup"); Undo.RecordObject(cam, "HELIOS scene setup");
            app.HomePose(b.Value, 16f / 9f, out var pos, out var rot);
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = app.fieldOfView;
            cam.clearFlags = CameraClearFlags.Depth;            // the background camera draws the backdrop (as at Play)
            cam.cullingMask &= ~(1 << 5);                       // not the UI layer
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 1000f;
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
