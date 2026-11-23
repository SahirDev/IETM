// Editor: the IETM follows the HELIOS navigator's Edit Page while you edit (no Play needed).
// Edit Page = IETM  -> the IETM Module is shown, fitted into the HELIOS content area, the HELIOS aircraft hidden.
// Any other page    -> the IETM Module is hidden (so its own shell does not draw behind the HELIOS UI).
// Pick the page on HELIOS UI > UI > HeliosNavigator > Edit Page.
using Helios;
using IETM.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IETM.EditorTools
{
    [InitializeOnLoad]
    static class HeliosIetmEditorPreview
    {
        static HeliosIetmEditorPreview() { HeliosNavigator.EditorPageShown += OnShown; }

        static void OnShown(HeliosNavigator nav, HeliosPage page)
        {
            if (Application.isPlaying || nav == null) return;
            if (PrefabStageUtility.GetPrefabStage(nav.gameObject) != null) return;   // editing the UI prefab on its own
            var bridge = Object.FindAnyObjectByType<HeliosIetmBridge>(FindObjectsInactive.Include);
            if (bridge != null) bridge.EditorPreview(nav, page);
        }
    }
}
