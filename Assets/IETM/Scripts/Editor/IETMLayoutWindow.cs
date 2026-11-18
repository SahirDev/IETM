// IETM > Layout Editor: place every part's EXPLODED and DISASSEMBLY (laid-out) position by hand, in the Scene
// view, before Play. Choose a layout, press Edit: the gear is posed EXTENDED and every part moves to its saved
// position. Move parts with the normal Move tool, then Save: the offsets are written back into
// landing_gear_ietm.json ("explode" / "removal" blocks). Cancel (or entering Play) puts the model back unchanged.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace IETM.EditorTools
{
    public class IETMLayoutWindow : EditorWindow
    {
        enum Layout { Disassembly = 0, Explode = 1 }

        Layout layout = Layout.Disassembly;
        bool editing;
        IETMApp app;
        TextAsset json;
        Vector2 scroll;
        // node -> (pose before editing, base pose of the extended gear)
        readonly Dictionary<Transform, (Vector3 pos, Quaternion rot, Vector3 scale)> original = new Dictionary<Transform, (Vector3, Quaternion, Vector3)>();
        readonly Dictionary<Transform, Vector3> basePos = new Dictionary<Transform, Vector3>();
        readonly List<Transform> parts = new List<Transform>();

        [MenuItem("IETM/Layout Editor (exploded and disassembly positions)", priority = 60)]
        public static void Open() => GetWindow<IETMLayoutWindow>("IETM Layout");

        void OnEnable() { EditorApplication.playModeStateChanged += OnPlayMode; }
        void OnDisable() { EditorApplication.playModeStateChanged -= OnPlayMode; if (editing) EndEdit(); }
        void OnPlayMode(PlayModeStateChange s) { if (s == PlayModeStateChange.ExitingEditMode && editing) EndEdit(); }

        void OnGUI()
        {
            if (Application.isPlaying) { EditorGUILayout.HelpBox("Stop Play mode to edit the layouts.", MessageType.Info); return; }
            if (app == null) app = FindAnyObjectByType<IETMApp>();
            if (app == null || app.extendedModel == null) { EditorGUILayout.HelpBox("Open the IETM scene (IETM App with a model).", MessageType.Warning); return; }

            EditorGUILayout.HelpBox(
                "1. Pick a layout and press Edit: the gear is shown EXTENDED with every part at its saved position.\n" +
                "2. Click a part (list below or Scene view) and move it with the Move tool (W).\n" +
                "3. Save writes the positions into landing_gear_ietm.json. Cancel restores the model.\n\n" +
                "Disassembly = where a part is laid out when a procedure removes it.\nExplode = the Explode slider at 100 %.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(editing))
                layout = (Layout)GUILayout.Toolbar((int)layout, new[] { "Disassembly layout", "Explode layout" });

            EditorGUILayout.Space(4);
            if (!editing)
            {
                if (GUILayout.Button("Edit " + layout + " layout", GUILayout.Height(28))) BeginEdit();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.6f);
            if (GUILayout.Button("Save", GUILayout.Height(28))) { Save(); EndEdit(); }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", GUILayout.Height(28))) EndEdit();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Move all parts back to the assembled position")) foreach (var t in parts) { Undo.RecordObject(t, "Reset layout"); t.localPosition = basePos[t]; }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Parts", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var t in parts)
            {
                if (t == null) continue;
                EditorGUILayout.BeginHorizontal();
                var d = t.localPosition - basePos[t];
                bool sel = Selection.activeTransform == t;
                if (GUILayout.Toggle(sel, t.name, "Button", GUILayout.MinWidth(170)) && !sel) { Selection.activeTransform = t; SceneView.lastActiveSceneView?.FrameSelected(); }
                GUILayout.Label(d.sqrMagnitude < 1e-10f ? "in place" : (d.magnitude * 1000f).ToString("0") + " mm", GUILayout.Width(70));
                if (GUILayout.Button("Reset", GUILayout.Width(50))) { Undo.RecordObject(t, "Reset part"); t.localPosition = basePos[t]; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ edit session

        void BeginEdit()
        {
            json = app.dataFile != null ? app.dataFile : Resources.Load<TextAsset>("IETM/landing_gear_ietm");
            if (json == null) { EditorUtility.DisplayDialog("IETM", "landing_gear_ietm.json not found.", "OK"); return; }
            var db = IETMDatabase.Load(json);
            var root = app.extendedModel.transform;

            original.Clear(); basePos.Clear(); parts.Clear();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) original[t] = (t.localPosition, t.localRotation, t.localScale);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "IETM layout");

            // pose the gear EXTENDED (the layouts are designed for it)
            if (app.gearAnimation != null)
            {
                var animRoot = root.gameObject;
                var anim = root.GetComponentInChildren<Animation>(true);
                var animator = root.GetComponentInChildren<Animator>(true);
                if (anim != null) animRoot = anim.gameObject; else if (animator != null) animRoot = animator.gameObject;
                float t = app.clipStartsRetracted ? app.midTime : 0f;
                app.gearAnimation.SampleAnimation(animRoot, t);
            }

            // every node named in the parts list
            var names = new HashSet<string>();
            foreach (var it in db.items) if (it.nodes != null) foreach (var n in it.nodes) names.Add(n);
            foreach (var n in names)
            {
                var t = IETMModelSetup.Find(root, n);
                if (t == null || parts.Contains(t)) continue;
                parts.Add(t);
                basePos[t] = t.localPosition;
            }
            parts.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            var list = layout == Layout.Disassembly ? db.removal : db.explode;
            if (list != null)
                foreach (var e in list)
                {
                    if (e == null || e.offset == null || e.offset.Length < 3) continue;
                    var t = parts.Find(p => p.name == e.node);
                    if (t != null) t.localPosition = basePos[t] + new Vector3(-e.offset[0], e.offset[1], e.offset[2]);
                }
            editing = true;
            Tools.current = Tool.Move;
            SceneView.lastActiveSceneView?.Frame(BoundsOf(root), false);
        }

        void EndEdit()
        {
            foreach (var kv in original)
                if (kv.Key != null) { kv.Key.localPosition = kv.Value.pos; kv.Key.localRotation = kv.Value.rot; kv.Key.localScale = kv.Value.scale; }
            original.Clear(); basePos.Clear(); parts.Clear();
            editing = false;
            Repaint();
        }

        void Save()
        {
            var sb = new StringBuilder("[\n");
            bool first = true;
            foreach (var t in parts)
            {
                var d = t.localPosition - basePos[t];
                if (d.sqrMagnitude < 1e-12f) continue;
                if (!first) sb.Append(",\n");
                first = false;
                // Unity local -> glTF (x mirrored, as glTFast imports it)
                sb.Append("    { \"node\": \"").Append(t.name.Replace("\"", "\\\"")).Append("\", \"offset\": [")
                  .Append(F(-d.x)).Append(", ").Append(F(d.y)).Append(", ").Append(F(d.z)).Append("] }");
            }
            sb.Append("\n  ]");

            string path = AssetDatabase.GetAssetPath(json);
            string text = File.ReadAllText(path);
            string key = layout == Layout.Disassembly ? "removal" : "explode";
            text = ReplaceArray(text, key, sb.ToString());
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[IETM] Saved the {layout} layout to {path}.");
        }

        static string F(float v) => v.ToString("0.#####", CultureInfo.InvariantCulture);

        /// <summary>Replaces the JSON array of a top-level key (adds the key when it is missing).</summary>
        static string ReplaceArray(string text, string key, string array)
        {
            int k = text.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (k < 0)
            {
                int end = text.LastIndexOf('}');
                return text.Substring(0, end).TrimEnd() + ",\n  \"" + key + "\": " + array + "\n}\n";
            }
            int open = text.IndexOf('[', k);
            int depth = 0, close = -1; bool str = false;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (str) { if (c == '\\') i++; else if (c == '"') str = false; continue; }
                if (c == '"') str = true;
                else if (c == '[') depth++;
                else if (c == ']' && --depth == 0) { close = i; break; }
            }
            if (open < 0 || close < 0) return text;
            return text.Substring(0, open) + array + text.Substring(close + 1);
        }

        static Bounds BoundsOf(Transform root)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(root.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            b.Expand(b.size.magnitude * 0.6f);
            return b;
        }
    }
}
