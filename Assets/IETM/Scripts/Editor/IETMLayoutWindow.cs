// IETM > Layout Editor: place every part's EXPLODED and DISASSEMBLY (laid-out) position by hand, in the Scene
// view, before Play. Choose the publication (landing gear, engine, ...) and a layout, press Edit: the model is shown
// (the gear posed EXTENDED) and every part moves to its saved position. Move parts with the normal Move tool, then
// Save: the offsets are written back into that publication's JSON ("explode" / "removal" / "removalVia" blocks).
// Cancel (or entering Play) puts the model back unchanged.
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
        enum Layout { Disassembly = 0, DisassemblyCorner = 1, Explode = 2 }

        Layout layout = Layout.Disassembly;
        int pubIndex;                     // 0 = landing gear (IETM App's own model), 1.. = IETM App > Publications
        bool editing;
        // objects shown only for the edit session (an inactive publication model, its inactive parents; the gear hidden)
        readonly List<(GameObject go, bool active)> shown = new List<(GameObject, bool)>();
        IETMApp app;
        TextAsset json;
        Vector2 scroll;
        // node -> (pose before editing, base pose of the extended gear)
        readonly Dictionary<Transform, (Vector3 pos, Quaternion rot, Vector3 scale)> original = new Dictionary<Transform, (Vector3, Quaternion, Vector3)>();
        readonly Dictionary<Transform, Vector3> basePos = new Dictionary<Transform, Vector3>();
        readonly List<Transform> parts = new List<Transform>();
        // corner tab: laid-out end position per part, and which parts use an L path
        readonly Dictionary<Transform, Vector3> endPos = new Dictionary<Transform, Vector3>();
        readonly HashSet<Transform> lPath = new HashSet<Transform>();

        [MenuItem("IETM/Layout Editor (exploded and disassembly positions)", priority = 60)]
        public static void Open() => GetWindow<IETMLayoutWindow>("IETM Layout");

        void OnEnable() { EditorApplication.playModeStateChanged += OnPlayMode; }
        void OnDisable() { EditorApplication.playModeStateChanged -= OnPlayMode; if (editing) EndEdit(); }
        void OnPlayMode(PlayModeStateChange s) { if (s == PlayModeStateChange.ExitingEditMode && editing) EndEdit(); }

        void OnGUI()
        {
            if (Application.isPlaying) { EditorGUILayout.HelpBox("Stop Play mode to edit the layouts.", MessageType.Info); return; }
            if (app == null) app = FindAnyObjectByType<IETMApp>(FindObjectsInactive.Include);   // HELIOS: under the hidden IETM Module
            if (app == null || app.extendedModel == null) { EditorGUILayout.HelpBox("Open the IETM scene (IETM App with a model).", MessageType.Warning); return; }

            EditorGUILayout.HelpBox(
                "1. Pick the publication and a layout and press Edit: the model is shown (gear EXTENDED) with every part at its saved position.\n" +
                "2. Click a part (list below or Scene view) and move it with the Move tool (W).\n" +
                "3. Save writes the positions into the publication's JSON. Cancel restores the model.\n\n" +
                "Disassembly = where a part is laid out when a procedure removes it.\n" +
                "L-path corner = tick 'L path' on the parts that should move in an L, then drag each to where its FIRST move ends (e.g. pulled straight off its pin). Unticked parts move straight.\n" +
                "Explode = the Explode slider at 100 %. Engine: saving the Disassembly layout also sets the exploded view to it.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(editing))
            {
                var names = new List<string> { "Landing gear" };
                foreach (var pb in app.publications) names.Add(pb == null ? "(empty)" : string.IsNullOrEmpty(pb.id) ? (pb.dataFile != null ? pb.dataFile.name : "(no data)") : pb.id);
                pubIndex = Mathf.Clamp(pubIndex, 0, names.Count - 1);
                pubIndex = EditorGUILayout.Popup("Publication", pubIndex, names.ToArray());
            }
            using (new EditorGUI.DisabledScope(editing))
                layout = (Layout)GUILayout.Toolbar((int)layout, new[] { "Disassembly layout", "L-path corner", "Explode layout" });

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
                if (layout == Layout.DisassemblyCorner && endPos.ContainsKey(t))
                {
                    bool on = lPath.Contains(t);
                    bool now = GUILayout.Toggle(on, "L path", GUILayout.Width(60));
                    if (now != on)
                    {
                        Undo.RecordObject(t, "L path");
                        if (now) { lPath.Add(t); t.localPosition = basePos[t] + IETMModel.AutoVia(endPos[t] - basePos[t]); }
                        else { lPath.Remove(t); t.localPosition = endPos[t]; }
                    }
                }
                else if (GUILayout.Button("Reset", GUILayout.Width(50))) { Undo.RecordObject(t, "Reset part"); t.localPosition = basePos[t]; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ edit session

        void BeginEdit()
        {
            IETMPublication pub = pubIndex > 0 && pubIndex - 1 < app.publications.Count ? app.publications[pubIndex - 1] : null;
            if (pubIndex > 0 && (pub == null || pub.dataFile == null || pub.model == null))
            { EditorUtility.DisplayDialog("IETM", "This publication needs a Data File and a Model (IETM App > Publications).", "OK"); return; }
            json = pub != null ? pub.dataFile : app.dataFile != null ? app.dataFile : Resources.Load<TextAsset>("IETM/landing_gear_ietm");
            if (json == null) { EditorUtility.DisplayDialog("IETM", "landing_gear_ietm.json not found.", "OK"); return; }
            var db = IETMDatabase.Load(json);
            var root = pub != null ? pub.model.transform : app.extendedModel.transform;

            // show the model for the session (it may be inactive, e.g. the engine under a hidden IETM Module);
            // the landing gear is hidden while another publication is edited (they share the same spot)
            shown.Clear();
            for (var a = root; a != null; a = a.parent)
                if (!a.gameObject.activeSelf) { shown.Add((a.gameObject, false)); a.gameObject.SetActive(true); }
            if (pub != null && app.extendedModel != null && app.extendedModel.activeSelf)
            { shown.Add((app.extendedModel, true)); app.extendedModel.SetActive(false); }

            original.Clear(); basePos.Clear(); parts.Clear(); endPos.Clear(); lPath.Clear();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) original[t] = (t.localPosition, t.localRotation, t.localScale);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "IETM layout");

            // pose the gear EXTENDED (the layouts are designed for it)
            if (pub == null && app.gearAnimation != null)
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

            if (layout == Layout.DisassemblyCorner)
            {
                // corner of each part's L-shaped removal path: saved corner, else the automatic one
                foreach (var e in db.removal)
                {
                    if (e == null || e.offset == null || e.offset.Length < 3) continue;
                    var t = parts.Find(p => p.name == e.node);
                    if (t == null) continue;
                    var end = new Vector3(-e.offset[0], e.offset[1], e.offset[2]);
                    var v = db.removalVia != null ? db.removalVia.Find(x => x != null && x.node == e.node && x.offset != null && x.offset.Length >= 3) : null;
                    endPos[t] = basePos[t] + end;
                    if (v != null || db.removalLPath)
                    {
                        lPath.Add(t);
                        t.localPosition = basePos[t] + (v != null ? new Vector3(-v.offset[0], v.offset[1], v.offset[2]) : IETMModel.AutoVia(end));
                    }
                    else t.localPosition = endPos[t];   // straight path: shown at its end, tick "L path" to give it a corner
                }
            }
            else
            {
                var list = layout == Layout.Disassembly ? db.removal : db.explode;
                if (list != null)
                    foreach (var e in list)
                    {
                        if (e == null || e.offset == null || e.offset.Length < 3) continue;
                        var t = parts.Find(p => p.name == e.node);
                        if (t != null) t.localPosition = basePos[t] + new Vector3(-e.offset[0], e.offset[1], e.offset[2]);
                    }
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
            for (int i = shown.Count - 1; i >= 0; i--) if (shown[i].go != null) shown[i].go.SetActive(shown[i].active);
            shown.Clear();
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
                if (layout == Layout.DisassemblyCorner && !lPath.Contains(t)) continue;   // straight path: no corner saved
                if (!first) sb.Append(",\n");
                first = false;
                // Unity local -> glTF (x mirrored, as glTFast imports it)
                sb.Append("    { \"node\": \"").Append(t.name.Replace("\"", "\\\"")).Append("\", \"offset\": [")
                  .Append(F(-d.x)).Append(", ").Append(F(d.y)).Append(", ").Append(F(d.z)).Append("] }");
            }
            sb.Append("\n  ]");

            string path = AssetDatabase.GetAssetPath(json);
            string text = File.ReadAllText(path);
            string key = layout == Layout.Disassembly ? "removal" : layout == Layout.DisassemblyCorner ? "removalVia" : "explode";
            text = ReplaceArray(text, key, sb.ToString());
            // engine (and other publications): the exploded view is the disassembly layout, saved together
            if (pubIndex > 0 && layout == Layout.Disassembly) text = ReplaceArray(text, "explode", sb.ToString());
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
