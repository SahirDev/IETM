// IETM <-> HELIOS editor tools (menu IETM > HELIOS Integration).
// The IETM lives inside the HELIOS scene under "IETM Module" (inactive = hidden). The bridge shows it on the
// HELIOS IETM page at Play, so builds need nothing extra: HELIOS.unity is the only scene.
//
// "Merge IETM Into HELIOS Scene" copies the objects of LandingGear_IETM.unity (where you can keep editing
// the IETM on its own) into HELIOS.unity again, replacing the previous "IETM Module".
#if UNITY_EDITOR
using System.Linq;
using IETM.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace IETM.EditorTools
{
    static class HeliosIetmIntegrationMenu
    {
        const string HeliosScenePath = "Assets/HELIOS/Scenes/HELIOS.unity";

        [MenuItem("IETM/HELIOS Integration/Open HELIOS Scene")]
        static void OpenHelios()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(HeliosScenePath);
        }

        [MenuItem("IETM/HELIOS Integration/Select IETM Module")]
        static void SelectModule()
        {
            var go = FindModule(SceneManager.GetActiveScene());
            if (go == null) { EditorUtility.DisplayDialog("IETM", "No '" + HeliosIetmBridge.ModuleName + "' in the open scene.\nOpen the HELIOS scene first.", "OK"); return; }
            Selection.activeGameObject = go; EditorGUIUtility.PingObject(go);
        }

        [MenuItem("IETM/HELIOS Integration/Merge IETM Into HELIOS Scene")]
        static void Merge()
        {
            if (!EditorUtility.DisplayDialog("IETM",
                "Copy the IETM (LandingGear_IETM.unity) into the HELIOS scene as '" + HeliosIetmBridge.ModuleName + "'?\n\n" +
                "The current '" + HeliosIetmBridge.ModuleName + "' in HELIOS is replaced. Nothing else in the HELIOS scene changes.",
                "Merge", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var helios = EditorSceneManager.OpenScene(HeliosScenePath, OpenSceneMode.Single);
            var old = FindModule(helios);
            if (old != null) Object.DestroyImmediate(old);

            var ietm = EditorSceneManager.OpenScene(HeliosIetmBridge.ScenePath, OpenSceneMode.Additive);
            var module = new GameObject(HeliosIetmBridge.ModuleName);
            SceneManager.MoveGameObjectToScene(module, helios);

            foreach (var root in ietm.GetRootGameObjects())
            {
                if (root.GetComponent<EventSystem>() != null) continue;          // HELIOS has its own
                SceneManager.MoveGameObjectToScene(root, helios);
                root.transform.SetParent(module.transform, true);
            }
            foreach (var cam in module.GetComponentsInChildren<Camera>(true))
            {
                if (cam.CompareTag("MainCamera")) cam.tag = "Untagged";        // Camera.main stays the HELIOS camera
                var al = cam.GetComponent<AudioListener>(); if (al != null) al.enabled = false;
            }
            module.SetActive(false);                                             // shown by the bridge on the IETM page

            EditorSceneManager.CloseScene(ietm, true);                           // not saved: the IETM scene keeps its objects
            EditorSceneManager.MarkSceneDirty(helios);
            EditorSceneManager.SaveScene(helios);
            Selection.activeGameObject = module;
            Debug.Log("[IETM] Merged into " + HeliosScenePath + " as '" + HeliosIetmBridge.ModuleName + "'.");
        }

        static GameObject FindModule(Scene s)
        {
            return s.IsValid() ? s.GetRootGameObjects().FirstOrDefault(g => g.name == HeliosIetmBridge.ModuleName) : null;
        }
    }
}
#endif
