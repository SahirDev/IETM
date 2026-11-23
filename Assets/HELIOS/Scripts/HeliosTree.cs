// HELIOS Aircraft Hierarchy (3D View page). The rows are ordinary objects in HELIOS UI.prefab: edit them in the Editor
// (rename the Label text, reorder, duplicate a row, change a row's Part Names). Nothing is built at run time.
// At Play the rows are bound to the aircraft's parts by name (HeliosTreeNode.partNames = name prefixes):
//   row click     -> the row's parts are outlined and the camera frames them (again = back to the whole aircraft)
//   chevron       -> expand / collapse
//   part clicked in the 3D view -> its row is selected in the tree
//   part under the mouse in the 3D view -> tooltip with its row names (HeliosApp.DescribePart)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    public class HeliosTree : MonoBehaviour
    {
        [Tooltip("Chevron of a collapsed row.")] public Sprite chevronRight;
        [Tooltip("Chevron of an expanded row.")] public Sprite chevronDown;
        [Tooltip("Eye of a shown row.")] public Sprite eyeOpen;
        [Tooltip("Eye of a hidden row.")] public Sprite eyeClosed;
        [Tooltip("Row selected (highlighted) - none at start.")] public HeliosTreeNode selected;
        [Tooltip("Update each row's count badge with the number of parts it found at Play.")] public bool liveCounts = true;

        HeliosApp app;
        readonly List<HeliosTreeNode> nodes = new List<HeliosTreeNode>();
        public bool Bound { get; private set; }

        void Awake()
        {
            GetComponentsInChildren(true, nodes);
            foreach (var n in nodes) n.Init(this);
            if (selected != null) selected.SetSelected(true);
        }

        void OnEnable()
        {
            if (app == null) app = FindAnyObjectByType<HeliosApp>();
            if (app != null) { app.PartSelected -= OnPartPicked; app.PartSelected += OnPartPicked; }
            if (!Bound) StartCoroutine(BindWhenReady());
        }

        void OnDisable() { if (app != null) app.PartSelected -= OnPartPicked; }

        IEnumerator BindWhenReady()
        {
            while (app == null || app.Model == null || app.Model.Parts == null || app.Model.Parts.Count == 0)
            {
                if (app == null) app = FindAnyObjectByType<HeliosApp>();
                yield return null;
            }
            if (app != null) { app.PartSelected -= OnPartPicked; app.PartSelected += OnPartPicked; app.DescribePart = Describe; }
            Bind();
        }

        /// <summary>Finds each row's parts by name (rows without Part Names take their children's parts).</summary>
        public void Bind()
        {
            foreach (var n in nodes) n.parts.Clear();
            var parts = app.Model.Parts;
            foreach (var r in parts)
            {
                if (r == null) continue;
                string nm = r.gameObject.name;
                foreach (var n in nodes) if (n.Matches(nm)) n.parts.Add(r);
            }
            // groups: the union of their descendant rows (deepest first)
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var n = nodes[i];
                if (n.HasOwnNames || n.children == null) continue;
                var set = new HashSet<Renderer>();
                foreach (var c in n.children.GetComponentsInChildren<HeliosTreeNode>(true)) if (c.HasOwnNames) set.UnionWith(c.parts);
                n.parts.AddRange(set);
            }
            foreach (var n in nodes) { n.visible = true; n.RefreshEye(); if (liveCounts) n.SetCount(n.parts.Count); }
            Bound = true;
        }

        // ---------------------------------------------------------------- actions

        public void Select(HeliosTreeNode node)
        {
            if (selected == node)                      // the selected row again: back to the whole aircraft
            {
                node.SetSelected(false); selected = null;
                if (app != null) app.SelectParts(null);
                return;
            }
            MarkSelected(node);
            if (app == null || !Bound) return;
            if (node.ParentNode == null) app.SelectParts(null);          // the whole aircraft: no outline, camera home
            else app.SelectParts(node.parts);
        }

        void MarkSelected(HeliosTreeNode node)
        {
            if (selected != null) selected.SetSelected(false);
            selected = node;
            if (selected != null) selected.SetSelected(true);
        }

        public void ToggleVisible(HeliosTreeNode node)
        {
            if (!Bound || app == null) return;
            bool show = !node.visible;
            app.SetPartsVisible(node.parts, show);
            foreach (var n in nodes) n.visible = n.parts.Count == 0 || n.parts.Exists(r => r != null && r.gameObject.activeSelf);
            foreach (var n in nodes) n.RefreshEye();
        }

        /// <summary>Shows every part again.</summary>
        public void ShowAll()
        {
            if (!Bound || app == null) return;
            foreach (var n in nodes) { app.SetPartsVisible(n.parts, true); n.visible = true; n.RefreshEye(); }
        }

        /// <summary>Tooltip text: part number, its row (e.g. "Nose · Left"), and the group above (e.g. Fuselage).</summary>
        public string Describe(Renderer part)
        {
            if (part == null) return null;
            HeliosTreeNode best = null;
            foreach (var n in nodes) if (n.parts.Contains(part) && (best == null || n.parts.Count < best.parts.Count)) best = n;
            string nm = part.gameObject.name;
            int pi = nm.LastIndexOf("_Part_");
            string number = pi >= 0 ? "Part " + nm.Substring(pi + 6) : nm;
            if (best == null || best.ParentNode == null) return HeliosPartTooltip.FromName(nm);
            string where = best.Label, group = null;
            var up = best.ParentNode;
            bool side = where == "Left" || where == "Right" || where == "Center";
            if (side && up != null && up.ParentNode != null) { where = up.Label + " \u00b7 " + where; up = up.ParentNode; }
            if (up != null && up.ParentNode != null) group = up.Label;
            if (pi < 0) { number = where; where = ""; }
            return HeliosPartTooltip.Format(number, where, group);
        }

        void OnPartPicked(Renderer part)
        {
            if (part == null) { if (selected != null) { selected.SetSelected(false); selected = null; } return; }
            HeliosTreeNode best = null;
            foreach (var n in nodes) if (n.parts.Contains(part) && (best == null || n.parts.Count < best.parts.Count)) best = n;
            if (best == null) return;
            for (var p = best.ParentNode; p != null; p = p.ParentNode) if (!p.expanded) p.SetExpanded(true);
            MarkSelected(best);
        }
    }
}
