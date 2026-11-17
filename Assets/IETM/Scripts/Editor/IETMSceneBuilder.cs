// IETM Editor tools.
//   IETM > Build Landing Gear IETM Scene    creates a scene with the model, camera and the full UI pre-built
//   IETM > Rebuild UI In Open Scene         replaces the UI in the open scene (your UI edits are lost)
//   IETM > Refresh UI Preview Content       refills the lists (tree, parts table...) after editing the JSON
//   IETM > Re-tag Model Parts From Data     re-applies IETMPart item numbers from the JSON node names
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace IETM.EditorTools
{
    public static class IETMSceneBuilder
    {
        public const string ScenePath = "Assets/IETM/Scenes/LandingGear_IETM.unity";
        const string SpriteFolder = "Assets/IETM/UI/Sprites";
        const string ExtendedName = "F-16 Front Landing Gear Assembly Extended";
        const string CollapsedName = "F-16 Front Landing Gear Assembly Collapsed";
        const string DataName = "landing_gear_ietm";

        // ================================================================== menu

        [MenuItem("IETM/Build Landing Gear IETM Scene", priority = 0)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var extended = FindPrefab(ExtendedName);
            var collapsed = FindPrefab(CollapsedName);
            var dataAsset = FindAsset<TextAsset>(DataName);
            if (extended == null)
            {
                EditorUtility.DisplayDialog("IETM", $"Could not find the prefab '{ExtendedName}'.\nImport the landing gear model first (Assets/3DModels).", "OK");
                return;
            }
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("IETM", "The IETM scene already exists:\n" + ScenePath + "\n\nRebuild it from scratch? Changes you made in that scene will be lost.", "Rebuild", "Cancel"))
                return;

            var db = IETMDatabase.Load(dataAsset);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- camera + light + event system
            var camGO = new GameObject("IETM Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.067f, 0.09f, 0.15f);
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.001f;
            cam.farClipPlane = 100f;
            camGO.AddComponent<ViewerCamera>();

            var lightGO = new GameObject("Key Light", typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var fill = new GameObject("Camera Fill Light", typeof(Light));
            var fl = fill.GetComponent<Light>();
            fl.type = LightType.Directional; fl.intensity = 0.45f; fl.shadows = LightShadows.None;
            fill.transform.SetParent(camGO.transform, false);
            fill.transform.localRotation = Quaternion.Euler(10f, 15f, 0f);

            CreateEventSystem();

            // ---- models (pre-configured: oriented, aligned, colliders, part tags)
            var root = new GameObject("Landing Gear");
            var ext = (GameObject)PrefabUtility.InstantiatePrefab(extended, scene);
            ext.transform.SetParent(root.transform, false);
            var pivot = IETMModelSetup.OrientUpright(ext, "F-16 Front Landing Gear", "Wheel", Vector3.zero, "Model Pivot");
            GameObject col = null;
            if (collapsed != null)
            {
                col = (GameObject)PrefabUtility.InstantiatePrefab(collapsed, scene);
                col.transform.SetParent(pivot != null ? pivot : root.transform, false);
                IETMModelSetup.AlignToReference(col, ext, "F-16 Front Landing Gear");
            }
            PrepareModel(ext, db);
            if (col != null) { PrepareModel(col, db); col.SetActive(false); }

            FrameCamera(cam, ext);

            // ---- UI
            var view = BuildView(db);

            // ---- app
            var appGO = new GameObject("IETM App");
            var app = appGO.AddComponent<IETMApp>();
            app.view = view;
            app.extendedModel = ext;
            app.retractedModel = col;
            app.modelPreconfigured = true;
            app.viewerCamera = cam;
            app.highlightShader = Shader.Find("IETM/Highlight");
            app.ghostShader = Shader.Find("IETM/Ghost");
            app.dataFile = dataAsset;
            if (app.highlightShader == null || app.ghostShader == null)
                Debug.LogWarning("[IETM] IETM shaders not found. Check that IETM/Shaders imported without errors.");

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = appGO;
            Debug.Log("[IETM] Scene created: " + ScenePath);
            EditorUtility.DisplayDialog("IETM",
                "Landing Gear IETM scene created:\n" + ScenePath +
                "\n\nThe UI is in the Hierarchy under 'IETM Canvas'. Select objects to edit them in the Inspector; look at the Game view to see the result.\n\nPress Play to run the IETM.", "OK");
        }

        [MenuItem("IETM/Open Landing Gear IETM Scene", priority = 1)]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildScene(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("IETM/Rebuild UI In Open Scene", priority = 20)]
        public static void RebuildUIMenu()
        {
            var app = Object.FindAnyObjectByType<IETMApp>();
            if (app == null) { EditorUtility.DisplayDialog("IETM", "No 'IETM App' in the open scene. Use IETM > Build Landing Gear IETM Scene.", "OK"); return; }
            RebuildUI(app);
        }

        [MenuItem("IETM/Refresh UI Preview Content", priority = 21)]
        public static void RefreshPreviewMenu()
        {
            var app = Object.FindAnyObjectByType<IETMApp>();
            if (app == null || app.view == null) { EditorUtility.DisplayDialog("IETM", "No pre-built IETM UI in the open scene.", "OK"); return; }
            RefreshPreview(app);
        }

        [MenuItem("IETM/Re-tag Model Parts From Data", priority = 22)]
        public static void RetagMenu()
        {
            var app = Object.FindAnyObjectByType<IETMApp>();
            if (app == null) { EditorUtility.DisplayDialog("IETM", "No 'IETM App' in the open scene.", "OK"); return; }
            RetagParts(app);
        }

        // ================================================================== operations (also used by the IETMApp inspector)

        public static void RebuildUI(IETMApp app)
        {
            if (app.view != null &&
                !EditorUtility.DisplayDialog("IETM", "Replace the current IETM UI with a freshly generated one?\nAny edits you made to the UI objects will be lost.", "Rebuild", "Cancel"))
                return;
            var db = IETMDatabase.Load(app.dataFile != null ? app.dataFile : FindAsset<TextAsset>(DataName));
            if (app.view != null) Undo.DestroyObjectImmediate(app.view.gameObject);
            var view = BuildView(db);
            Undo.RegisterCreatedObjectUndo(view.gameObject, "Rebuild IETM UI");
            Undo.RecordObject(app, "Rebuild IETM UI");
            app.view = view;
            if (Object.FindAnyObjectByType<EventSystem>() == null) CreateEventSystem();
            EditorUtility.SetDirty(app);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            Selection.activeGameObject = view.gameObject;
        }

        public static void RefreshPreview(IETMApp app)
        {
            var db = IETMDatabase.Load(app.dataFile != null ? app.dataFile : FindAsset<TextAsset>(DataName));
            WithSprites(() => IETMUIBuilder.PopulatePreview(app.view, db));
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
        }

        public static void RetagParts(IETMApp app)
        {
            var db = IETMDatabase.Load(app.dataFile != null ? app.dataFile : FindAsset<TextAsset>(DataName));
            int n = 0;
            foreach (var go in new[] { app.extendedModel, app.retractedModel })
            {
                if (go == null || !go.scene.IsValid()) continue;
                n += IETMModelSetup.TagParts(go, db);
                EditorUtility.SetDirty(go);
            }
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            Debug.Log($"[IETM] Tagged {n} model nodes with IETMPart.");
        }

        static IETMView BuildView(IETMDatabase db)
        {
            IETMView view = null;
            WithSprites(() =>
            {
                view = IETMUIBuilder.Build(db);
                IETMUIBuilder.PopulatePreview(view, db);
            });
            return view;
        }

        static void PrepareModel(GameObject go, IETMDatabase db)
        {
            IETMModelSetup.AddColliders(go);
            IETMModelSetup.TagParts(go, db);
        }

        static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        static void FrameCamera(Camera cam, GameObject model)
        {
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float radius = Mathf.Max(0.01f, b.extents.magnitude);
            var rot = Quaternion.Euler(20f, 35f, 0f);
            float dist = radius * 1.15f / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.rotation = rot;
            cam.transform.position = b.center - rot * Vector3.forward * dist;
            cam.nearClipPlane = Mathf.Max(0.0005f, radius * 0.01f);
            cam.farClipPlane = radius * 100f;
        }

        // ================================================================== sprite assets

        static readonly Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();
        static Sprite circleCache;

        /// <summary>Runs 'build' with UIKit using sprite ASSETS (so the UI survives saving the scene).</summary>
        static void WithSprites(System.Action build)
        {
            UIKit.RoundedOverride = RoundedAsset;
            UIKit.CircleOverride = CircleAsset;
            try { build(); }
            finally { UIKit.RoundedOverride = null; UIKit.CircleOverride = null; }
        }

        static Sprite RoundedAsset(int radius)
        {
            if (spriteCache.TryGetValue(radius, out var s) && s != null) return s;
            string path = $"{SpriteFolder}/IETM_Round{radius}.png";
            s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null)
            {
                int b = radius + 1;
                s = WriteSprite(path, UIKit.RoundedTexture(radius), new Vector4(b, b, b, b));
            }
            spriteCache[radius] = s;
            return s;
        }

        static Sprite CircleAsset()
        {
            if (circleCache != null) return circleCache;
            string path = $"{SpriteFolder}/IETM_Circle.png";
            circleCache = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (circleCache == null) circleCache = WriteSprite(path, UIKit.CircleTexture(), Vector4.zero);
            return circleCache;
        }

        static Sprite WriteSprite(string path, Texture2D tex, Vector4 border)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 100;
            imp.spriteBorder = border;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================== asset lookup

        public static GameObject FindPrefab(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        public static T FindAsset<T>(string name) where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets(name))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) return a;
            }
            return null;
        }

        static void AddToBuildSettings(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            list.RemoveAll(s => s.path == path);
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }

    /// <summary>Offers to build the IETM scene the first time the project is opened.</summary>
    [InitializeOnLoad]
    static class IETMFirstRun
    {
        static IETMFirstRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(IETMSceneBuilder.ScenePath)) return;
                if (SessionState.GetBool("IETM_FirstRunAsked", false) || EditorPrefs.GetBool("IETM_FirstRunNever", false)) return;
                SessionState.SetBool("IETM_FirstRunAsked", true);
                int r = EditorUtility.DisplayDialogComplex("IETM",
                    "Build the Landing Gear IETM scene now?\nThe UI and the model will be created in the scene so you can edit them before pressing Play.",
                    "Build scene", "Not now", "Don't ask again");
                if (r == 0) IETMSceneBuilder.BuildScene();
                else if (r == 2) EditorPrefs.SetBool("IETM_FirstRunNever", true);
            };
        }
    }
}
