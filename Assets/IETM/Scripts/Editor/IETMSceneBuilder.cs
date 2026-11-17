// One-click scene setup: menu  IETM > Build Landing Gear IETM Scene
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IETM.EditorTools
{
    public static class IETMSceneBuilder
    {
        const string ScenePath = "Assets/IETM/Scenes/LandingGear_IETM.unity";
        const string ExtendedName = "F-16 Front Landing Gear Assembly Extended";
        const string CollapsedName = "F-16 Front Landing Gear Assembly Collapsed";

        [MenuItem("IETM/Build Landing Gear IETM Scene", priority = 0)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var extended = FindPrefab(ExtendedName);
            var collapsed = FindPrefab(CollapsedName);
            if (extended == null)
            {
                EditorUtility.DisplayDialog("IETM", $"Could not find the prefab '{ExtendedName}'.\nImport the landing gear model first (Assets/3DModels).", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // camera
            var camGO = new GameObject("IETM Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.067f, 0.09f, 0.15f);
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.001f;
            cam.farClipPlane = 100f;
            camGO.transform.position = new Vector3(0.5f, 0.3f, -0.6f);
            camGO.transform.LookAt(Vector3.zero);

            // key light
            var lightGO = new GameObject("Key Light", typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // models
            var root = new GameObject("Landing Gear");
            var ext = (GameObject)PrefabUtility.InstantiatePrefab(extended, scene);
            ext.transform.SetParent(root.transform, false);
            GameObject col = null;
            if (collapsed != null)
            {
                col = (GameObject)PrefabUtility.InstantiatePrefab(collapsed, scene);
                col.transform.SetParent(root.transform, false);
            }
            AddColliders(ext);
            if (col != null) AddColliders(col);

            // app
            var appGO = new GameObject("IETM App");
            var app = appGO.AddComponent<IETMApp>();
            app.extendedModel = ext;
            app.retractedModel = col;
            app.viewerCamera = cam;
            app.highlightShader = Shader.Find("IETM/Highlight");
            app.ghostShader = Shader.Find("IETM/Ghost");
            app.dataFile = FindAsset<TextAsset>("landing_gear_ietm");
            if (app.highlightShader == null || app.ghostShader == null)
                Debug.LogWarning("[IETM] IETM shaders not found. Check that IETM/Shaders imported without errors.");

            // save
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = appGO;
            Debug.Log("[IETM] Scene created: " + ScenePath + "  -  press Play.");
            EditorUtility.DisplayDialog("IETM", "Landing Gear IETM scene created:\n" + ScenePath + "\n\nPress Play to run the IETM.", "OK");
        }

        [MenuItem("IETM/Open Landing Gear IETM Scene", priority = 1)]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) { Build(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        static void AddColliders(GameObject go)
        {
            // Mesh colliders are cooked at build time, so picking works even for non-readable meshes.
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        static GameObject FindPrefab(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        static T FindAsset<T>(string name) where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets(name))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<T>(path);
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
}
