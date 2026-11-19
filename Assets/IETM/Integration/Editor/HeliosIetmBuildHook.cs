// IETM <-> HELIOS: makes sure LandingGear_IETM.unity is part of player builds (WebGL etc.).
// In Play mode in the Editor nothing is needed: the bridge loads the scene by path.
//
// When a build is started from the Build window, the IETM scene is appended to that build's scene list
// for that build only. The project's Build Settings file is not changed.
// If you prefer it in the Build Settings list permanently (or use a custom build script), run
// IETM > HELIOS Integration > Add IETM Scene To Build.
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using IETM.Integration;
using UnityEditor;
using UnityEngine;

namespace IETM.EditorTools
{
    [InitializeOnLoad]
    static class HeliosIetmBuildHook
    {
        static HeliosIetmBuildHook()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildWithIetm);
        }

        static void BuildWithIetm(BuildPlayerOptions options)
        {
            string path = HeliosIetmBridge.ScenePath;
            var scenes = options.scenes ?? new string[0];
            if (File.Exists(path) && !scenes.Contains(path) && scenes.Any(IsHeliosScene))
            {
                options.scenes = scenes.Concat(new[] { path }).ToArray();
                Debug.Log("[IETM] Added " + path + " to this build (loaded by the HELIOS IETM page).");
            }
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }

        static bool IsHeliosScene(string scenePath)
        {
            var text = File.Exists(scenePath) ? File.ReadAllText(scenePath) : "";
            return Path.GetFileNameWithoutExtension(scenePath) == "HELIOS" || text.Contains("Helios.HeliosApp") || text.Contains("HELIOS UI");
        }

        [MenuItem("IETM/HELIOS Integration/Add IETM Scene To Build")]
        static void AddToBuildSettings()
        {
            string path = HeliosIetmBridge.ScenePath;
            if (!File.Exists(path)) { EditorUtility.DisplayDialog("IETM", "Scene not found:\n" + path, "OK"); return; }
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path)) { Debug.Log("[IETM] " + path + " is already in Build Settings."); return; }
            list.Add(new EditorBuildSettingsScene(path, true));           // appended: the HELIOS scene stays first
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[IETM] Added " + path + " to Build Settings.");
        }

        [MenuItem("IETM/HELIOS Integration/Open HELIOS Scene")]
        static void OpenHelios()
        {
            var guid = AssetDatabase.FindAssets("HELIOS t:Scene").FirstOrDefault();
            if (guid == null) { EditorUtility.DisplayDialog("IETM", "HELIOS scene not found.", "OK"); return; }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
#endif
