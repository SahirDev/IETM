// One-time Editor set-up of the Engine IETM in the HELIOS scene (everything is done at edit time and saved in the
// scene, nothing is built in Play / WebGL):
//   1. Assets/Model/engine.glb is placed under "IETM Module" next to the landing gear (inactive until opened),
//      sized like the landing gear, with mesh colliders baked for picking.
//   2. It is registered on IETM App > Publications with Resources/IETM/engine_ietm.json.
//   3. The HELIOS shell (Aircraft > System > IETM pages) is rebuilt from helios_shell.json, so the Engine card and
//      the "71-80 Engine" system page (section 72-00, Open IETM) are in the scene.
// Runs by itself once when HELIOS.unity is open and the engine is not registered yet; again from
// IETM > HELIOS Integration > Set Up Engine IETM.
using System.Linq;
using IETM;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IETM.EditorTools
{
    [InitializeOnLoad]
    public static class HeliosEngineSetup
    {
        const string ScenePath = "Assets/HELIOS/Scenes/HELIOS.unity";
        const string ModelPath = "Assets/Model/engine.glb";
        const string DataPath = "Assets/IETM/Resources/IETM/engine_ietm.json";
        const string ShellDataPath = "Assets/IETM/Resources/IETM/helios_shell.json";
        const string EngineName = "Engine";
        const string PubId = "engine";

        static HeliosEngineSetup()
        {
            EditorApplication.delayCall += AutoRun;
            EditorSceneManager.sceneOpened += (s, m) => EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;
            var app = Object.FindAnyObjectByType<IETMApp>(FindObjectsInactive.Include);
            if (app == null || app.publications.Any(p => p != null && p.id == PubId && p.model != null && p.dataFile != null)) return;
            Run(app, true);
        }

        [MenuItem("IETM/HELIOS Integration/Set Up Engine IETM")]
        static void Menu()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            var app = Object.FindAnyObjectByType<IETMApp>(FindObjectsInactive.Include);
            if (app == null) { EditorUtility.DisplayDialog("IETM", "No IETM App in HELIOS.unity.", "OK"); return; }
            Run(app, true);
        }

        static void Run(IETMApp app, bool save)
        {
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var data = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (glb == null || data == null) { Debug.LogWarning("[IETM] Engine set-up: missing " + (glb == null ? ModelPath : DataPath)); return; }

            var engine = PlaceEngine(app, glb);
            Register(app, engine, data);
            RebuildShell(app);

            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            if (save) EditorSceneManager.SaveScene(app.gameObject.scene);
            Debug.Log("[IETM] Engine IETM set up: model, publication and shell (System 71-80 > 72-00 Engine) saved in HELIOS.unity.");
        }

        /// <summary>engine.glb under the landing gear's parent, centred on it and of comparable size; inactive.</summary>
        static GameObject PlaceEngine(IETMApp app, GameObject glb)
        {
            var gear = app.extendedModel != null && app.extendedModel.scene.IsValid() ? app.extendedModel : null;
            Transform parent = gear != null ? gear.transform.parent : app.transform;

            var existing = parent != null ? parent.Find(EngineName) : null;
            if (existing != null) return existing.gameObject;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(glb, parent);
            Undo.RegisterCreatedObjectUndo(go, "Set Up Engine IETM");
            go.name = EngineName;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // size and position: like the landing gear (its bounds), engine shaft horizontal as authored
            if (gear != null)
            {
                bool was = gear.activeSelf;
                gear.SetActive(true);
                var gb = WorldBounds(gear);
                gear.SetActive(was);
                var eb = WorldBounds(go);
                float gs = Mathf.Max(gb.size.x, gb.size.y, gb.size.z), es = Mathf.Max(eb.size.x, eb.size.y, eb.size.z);
                if (gs > 0.0001f && es > 0.0001f) go.transform.localScale = Vector3.one * (gs * 1.1f / es);
                eb = WorldBounds(go);
                go.transform.position += gb.center - eb.center;
            }

            // picking colliders baked now (not added at run time)
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponent<Collider>() != null) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mc = Undo.AddComponent<MeshCollider>(r.gameObject);
                mc.sharedMesh = mf.sharedMesh;
            }
            go.SetActive(false);
            return go;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void Register(IETMApp app, GameObject engine, TextAsset data)
        {
            Undo.RecordObject(app, "Set Up Engine IETM");
            var pub = app.publications.FirstOrDefault(p => p != null && p.id == PubId);
            if (pub == null) { pub = new IETMPublication { id = PubId }; app.publications.Add(pub); }
            pub.dataFile = data;
            pub.model = engine;
            pub.startModule = "DM-ENG-TITLE";
            if (string.IsNullOrEmpty(pub.ipdPdfFile)) pub.ipdPdfFile = "IETM/72-00-00_Fig1_IPC.pdf";
            PrefabUtility.RecordPrefabInstancePropertyModifications(app);
        }

        /// <summary>Rebuilds the Aircraft / System / IETM shell pages from helios_shell.json (no theme / layout change).</summary>
        static void RebuildShell(IETMApp app)
        {
            var v = app.view;
            var ctl = Object.FindAnyObjectByType<HeliosShell>(FindObjectsInactive.Include);
            if (v == null || ctl == null) { Debug.LogWarning("[IETM] Engine set-up: no HELIOS shell in the scene, shell not rebuilt."); return; }
            var shellAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ShellDataPath);
            var shellData = HeliosShellData.Load(shellAsset);
            var db = IETMDatabase.Load(app.dataFile != null ? app.dataFile : Resources.Load<TextAsset>("IETM/landing_gear_ietm"));

            Undo.RegisterFullObjectHierarchyUndo(v.gameObject, "Set Up Engine IETM");
            Undo.RecordObject(v, "Set Up Engine IETM");
            Undo.RecordObject(ctl, "Set Up Engine IETM");

            HeliosShellView sv = null;
            var prevFont = UIKit.FontOverride;
            IETMSceneBuilder.WithSprites(() =>
            {
                if (v.font != null) UIKit.FontOverride = v.font;
                HeliosShellBuilder.SpriteLoader = HeliosShellEditor.LoadShellSprite;
                try { sv = HeliosShellBuilder.Build(v, shellData, db); }
                finally { HeliosShellBuilder.SpriteLoader = null; UIKit.FontOverride = prevFont; }
            });
            var logo = v.root != null ? v.root.Find("Top Bar/Row/Logo") : null;
            if (logo != null) logo.gameObject.SetActive(false);
            ctl.app = app; ctl.shell = sv; ctl.shellData = shellAsset;
            EditorUtility.SetDirty(v); EditorUtility.SetDirty(ctl);
        }
    }
}
