// HELIOS: everything in the scene before Play (WebGL: nothing is loaded or built when the app starts).
//   1. The aircraft (HELIOS App > Aircraft Model, e.g. F_16_Updated_Metadata 4.glb) is placed in the scene under
//      HELIOS > "HELIOS Model", centred and scaled exactly as the app would do at Play; Aircraft Model then points to
//      that scene object, so Play uses it as it is (no instantiate, no move, no scale).
//   2. The background camera (draws the backdrop behind the model) is a scene object, assigned to the app.
//   3. Sensor Focus is on the UI (instead of being added at Play).
// Runs by itself once when HELIOS.unity is open and something is missing; again from Tools > HELIOS > Place Aircraft In Scene.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            var app = Object.FindAnyObjectByType<HeliosApp>(FindObjectsInactive.Include);
            if (app != null && NeedsSetup(app)) Run(app, true);
        }

        static HeliosNavigator Nav() => Object.FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);

        static bool NeedsSetup(HeliosApp app)
        {
            var nav = Nav();
            return (app.aircraftModel != null && !HeliosModel.IsPlaced(app.aircraftModel)) || app.backgroundCamera == null
                   || (nav != null && nav.GetComponent<HeliosSensorFocus>() == null);
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

            // 3. sensor focus on the UI
            var nav = Nav();
            if (nav != null && nav.GetComponent<HeliosSensorFocus>() == null) Undo.AddComponent<HeliosSensorFocus>(nav.gameObject);

            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            if (save) EditorSceneManager.SaveScene(app.gameObject.scene);
        }
    }
}
