// One row of the HELIOS Aircraft Hierarchy (see HeliosTree). Edit it in the Editor: the Label child's text, the count
// badge, Part Names (which parts of the aircraft the row stands for), Expanded.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    public class HeliosTreeNode : MonoBehaviour
    {
        [Tooltip("The aircraft parts of this row: part (GameObject) names starting with any of these, e.g. F16_Wing_Region_Left_. " +
                 "Empty = the parts of its child rows.")]
        public string[] partNames = new string[0];
        [Tooltip("Container of the child rows (none for a leaf).")] public GameObject children;
        [Tooltip("Expand / collapse chevron.")] public Image chevron;
        [Tooltip("Selection highlight of the row.")] public GameObject selectedMark;
        [Tooltip("Eye button icon (hide / show).")] public Image eye;
        [Tooltip("Count badge text.")] public TMP_Text count;
        [Tooltip("Children shown when the page opens (at edit time, switch the Children object on / off to see them).")] public bool expanded;

        [System.NonSerialized] public readonly List<Renderer> parts = new List<Renderer>();
        [System.NonSerialized] public bool visible = true;
        HeliosTree tree;

        public bool HasOwnNames { get { if (partNames == null) return false; foreach (var s in partNames) if (!string.IsNullOrEmpty(s)) return true; return false; } }

        public bool Matches(string partName)
        {
            if (partNames == null) return false;
            foreach (var s in partNames) if (!string.IsNullOrEmpty(s) && partName.StartsWith(s, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>The row whose Children container holds this row (null for the top row).</summary>
        public HeliosTreeNode ParentNode
        {
            get
            {
                for (var t = transform.parent; t != null; t = t.parent)
                {
                    var n = t.GetComponentInChildren<HeliosTreeNode>(true);
                    if (n != null && n != this && n.children != null && transform.IsChildOf(n.children.transform)) return n;
                    if (t.GetComponent<HeliosTree>() != null) break;
                }
                return null;
            }
        }

        public void Init(HeliosTree t) { tree = t; SetExpanded(expanded); SetSelected(false); }

        // ---------------------------------------------------------------- buttons (saved in the prefab)
        public void OnClick() { if (tree != null) tree.Select(this); }
        public void OnChevron() { if (children != null) SetExpanded(!expanded); }
        public void OnEye() { if (tree != null) tree.ToggleVisible(this); }

        public void SetExpanded(bool on)
        {
            expanded = on && children != null;
            if (children != null) children.SetActive(expanded);
            if (chevron != null && tree != null && children != null) chevron.sprite = expanded ? tree.chevronDown : tree.chevronRight;
            var content = transform.parent != null ? transform.parent.parent as RectTransform : null;
            if (content != null) LayoutRebuilder.MarkLayoutForRebuild(content);
        }

        public void SetSelected(bool on) { if (selectedMark != null) selectedMark.SetActive(on); }

        public void RefreshEye()
        {
            if (eye == null || tree == null) return;
            eye.sprite = visible ? tree.eyeOpen : tree.eyeClosed;
            var c = eye.color; c.a = visible ? 1f : 0.45f; eye.color = c;
        }

        public void SetCount(int n) { if (count != null) count.text = n.ToString(); }
    }
}
