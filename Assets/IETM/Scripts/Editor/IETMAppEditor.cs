// Inspector for IETM App: quick actions for the pre-built UI and model.
using UnityEditor;
using UnityEngine;

namespace IETM.EditorTools
{
    [CustomEditor(typeof(IETMApp))]
    public class IETMAppEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var app = (IETMApp)target;

            EditorGUILayout.HelpBox(
                app.view != null
                    ? "UI is pre-built under '" + app.view.name + "'. Edit it in the Hierarchy / Inspector; press Play to run."
                    : "No pre-built UI: it will be generated at runtime. Click 'Build UI In Scene' to make it editable.",
                app.view != null ? MessageType.Info : MessageType.Warning);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(app.view != null ? "Rebuild UI" : "Build UI In Scene", GUILayout.Height(26))) IETMSceneBuilder.RebuildUI(app);
                using (new EditorGUI.DisabledScope(app.view == null))
                {
                    if (GUILayout.Button("Refresh Preview Content", GUILayout.Height(26))) IETMSceneBuilder.RefreshPreview(app);
                    if (GUILayout.Button("Select UI", GUILayout.Height(26))) Selection.activeGameObject = app.view.gameObject;
                }
                EditorGUILayout.EndHorizontal();

                using (new EditorGUI.DisabledScope(app.view == null))
                    if (GUILayout.Button("Apply HELIOS Theme (colours, font, icons, logo)", GUILayout.Height(24))) IETMHeliosTheme.Apply(app);

                if (GUILayout.Button("Use Animated GLB Model (Landing_Gear 1.glb)", GUILayout.Height(24))) IETMSceneBuilder.UseAnimatedModel(app);

                using (new EditorGUI.DisabledScope(app.view == null))
                {
                    EditorGUILayout.BeginHorizontal();
                    bool hasShell = Object.FindAnyObjectByType<HeliosShell>() != null;
                    if (GUILayout.Button(hasShell ? "Rebuild HELIOS Shell" : "Build HELIOS Shell (IETM inside HELIOS)", GUILayout.Height(24))) HeliosShellEditor.BuildShell(app, true);
                    using (new EditorGUI.DisabledScope(!hasShell))
                        if (GUILayout.Button("Remove Shell", GUILayout.Height(24), GUILayout.Width(110))) HeliosShellEditor.RemoveShell(app);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Re-tag Model Parts From Data")) IETMSceneBuilder.RetagParts(app);
                using (new EditorGUI.DisabledScope(app.dataFile == null))
                    if (GUILayout.Button("Open Data (JSON)")) AssetDatabase.OpenAsset(app.dataFile);
                using (new EditorGUI.DisabledScope(app.view == null || app.view.templates == null))
                    if (GUILayout.Button("Show Row Templates"))
                    {
                        var t = app.view.templates.gameObject;
                        Undo.RecordObject(t, "Toggle IETM templates");
                        t.SetActive(!t.activeSelf);
                        Selection.activeGameObject = t;
                    }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }
    }
}
