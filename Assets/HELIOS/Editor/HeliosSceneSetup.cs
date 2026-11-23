// HELIOS: everything in the scene before Play (WebGL: nothing is loaded or built when the app starts).
//   1. The aircraft (HELIOS App > Aircraft Model, e.g. F_16_Updated_Metadata 4.glb) is placed in the scene under
//      HELIOS > "HELIOS Model", centred and scaled exactly as the app would do at Play; Aircraft Model then points to
//      that scene object, so Play uses it as it is (no instantiate, no move, no scale).
//   2. The background camera (draws the backdrop behind the model) is a scene object, assigned to the app.
//   3. Sensor Focus is on the UI (instead of being added at Play).
// Runs by itself once when HELIOS.unity is open and something is missing; again from Tools > HELIOS > Place Aircraft In Scene.
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Helios.EditorTools
{
    [InitializeOnLoad]
    public static class HeliosSceneSetup
    {
        const string ScenePath = "Assets/HELIOS/Scenes/HELIOS.unity";

        static HeliosSceneSetup()
        {
            EditorApplication.delayCall += AutoRun;
            EditorSceneManager.sceneOpened += (s, m) => EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;
            CheckUiPrefab();
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app != null && NeedsSetup(app)) Run(app, true);
        }

        static void PoseMainCamera(HeliosApp app)
        {
            var cam = Camera.main;
            if (cam == null || app.aircraftModel == null) return;
            bool any = false; var b = new Bounds();
            foreach (var r in app.aircraftModel.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return;
            Undo.RecordObject(cam.transform, "HELIOS scene setup"); Undo.RecordObject(cam, "HELIOS scene setup");
            app.HomePose(b, 16f / 9f, out var pos, out var rot);
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = app.fieldOfView;
            cam.clearFlags = CameraClearFlags.Depth;            // the background camera draws the backdrop (as at Play)
            cam.cullingMask &= ~(1 << 5);                       // not the UI layer
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 1000f;
        }

        static GameObject DuplicateLight()
        {
            var main = Camera.main;
            bool rig = main != null && main.GetComponentInChildren<Light>(true) != null;
            if (!rig) return null;
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

        [MenuItem("Tools/HELIOS/Frame Aircraft In Game View", false, 8)]
        static void FrameMenu()
        {
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app != null) { PoseMainCamera(app); EditorSceneManager.MarkSceneDirty(app.gameObject.scene); }
        }

        // ---------------------------------------------------------------- the UI prefab is the project's one (Figma UI)

        const string PrefabPath = "Assets/HELIOS/Resources/HELIOS UI.prefab";
        const string Marker = "m_Name: Aircraft Hierarchy";      // only in the current (Figma) HELIOS UI

        /// <summary>The HELIOS UI prefab was replaced by the default generated UI (old menu, no Aircraft Hierarchy /
        /// View Tools): offers to restore the project's prefab from git.</summary>
        static void CheckUiPrefab()
        {
            if (!File.Exists(PrefabPath) || File.ReadAllText(PrefabPath).Contains(Marker)) return;
            if (SessionState.GetBool("HELIOS.PrefabAsked", false)) return;
            SessionState.SetBool("HELIOS.PrefabAsked", true);
            if (!EditorUtility.DisplayDialog("HELIOS UI",
                    "Assets/HELIOS/Resources/HELIOS UI.prefab is the old default UI (it was regenerated on this computer), not the " +
                    "project's UI (Figma: Aircraft Hierarchy, View Tools, cards).\n\nRestore it from git now? (git checkout of that one file)",
                    "Restore", "Not now")) return;
            RestoreUiPrefab();
        }

        [MenuItem("Tools/HELIOS/Restore UI Prefab From Git", false, 7)]
        static void RestoreUiPrefab()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            try
            {
                var psi = new ProcessStartInfo("git", "checkout HEAD -- \"" + PrefabPath + "\"")
                { WorkingDirectory = root, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
                using (var p = Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd(); p.WaitForExit();
                    if (p.ExitCode != 0) { Debug.LogError("[HELIOS] git checkout failed: " + err); return; }
                }
                AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
                Debug.Log("[HELIOS] HELIOS UI.prefab restored from git (the project's Figma UI).");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[HELIOS] Could not run git (" + e.Message + "). In a terminal at the project folder run:  git checkout HEAD -- \"" + PrefabPath + "\"");
            }
        }

        static HeliosNavigator Nav() => Object.FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);

        static bool NeedsSetup(HeliosApp app)
        {
            var nav = Nav();
            return (app.aircraftModel != null && !HeliosModel.IsPlaced(app.aircraftModel)) || app.backgroundCamera == null
                   || (nav != null && nav.GetComponent<HeliosSensorFocus>() == null) || !CameraAtHome(app) || DuplicateLight() != null;
        }

        static bool CameraAtHome(HeliosApp app)
        {
            var cam = Camera.main;
            if (cam == null || app.aircraftModel == null || !HeliosModel.IsPlaced(app.aircraftModel)) return true;
            bool any = false; var b = new Bounds();
            foreach (var r in app.aircraftModel.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return true;
            app.HomePose(b, 16f / 9f, out var pos, out var rot);
            return (cam.transform.position - pos).sqrMagnitude < 0.01f && cam.clearFlags == CameraClearFlags.Depth;
        }

        [MenuItem("Tools/HELIOS/Place Aircraft In Scene (no loading at Play)", false, 6)]
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

        public static void Run(HeliosApp app, bool save)
        {
            Undo.RecordObject(app, "HELIOS scene setup");

            // 1. the aircraft, placed like HeliosModel.Build does at Play
            var src = app.aircraftModel;
            if (src != null && !HeliosModel.IsPlaced(src))
            {
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

                // centre on the origin and scale the longest side to HeliosModel.Size (same as at Play)
                bool any = false; var b = new Bounds();
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (any && b.size.sqrMagnitude > 1e-8f)
                {
                    inst.transform.position -= b.center;
                    float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    rootT.localScale = Vector3.one * (longest > 1e-5f ? HeliosModel.Size / longest : 1f);
                }
                app.aircraftModel = inst;
                Debug.Log("[HELIOS] Aircraft placed in the scene: " + inst.name + " (under HELIOS > " + HeliosModel.PlacedRootName + "). Nothing is loaded at Play.");
            }

            // 2. the background camera
            if (app.backgroundCamera == null)
            {
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

            // 2b. the main camera at the home view (as the app places it at Play), so the Game view shows the aircraft
            PoseMainCamera(app);

            // 2c. Unity's default "Directional Light" doubles the HELIOS light rig (Camera Key Light on the Main Camera,
            //     Rim / Top lights): removed, the aircraft was over-lit
            RemoveDuplicateLight();

            // 3. sensor focus on the UI
            var nav = Nav();
            if (nav != null && nav.GetComponent<HeliosSensorFocus>() == null) Undo.AddComponent<HeliosSensorFocus>(nav.gameObject);

            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            if (save) EditorSceneManager.SaveScene(app.gameObject.scene);
        }
    }
}
