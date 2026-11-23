// One row of the HELIOS Aircraft Hierarchy tree (see HeliosTree). The row's Button calls OnClick (saved in the prefab).
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    public class HeliosTreeNode : MonoBehaviour
    {
        [Tooltip("Container of the child rows (empty for a leaf).")] public GameObject children;
        [Tooltip("Expand / collapse chevron.")] public Image chevron;
        [Tooltip("Selection highlight of the row.")] public GameObject selectedMark;
        public bool expanded;

        HeliosTree tree;
        HeliosTree Tree => tree != null ? tree : (tree = GetComponentInParent<HeliosTree>(true));

        public void OnClick()
        {
            if (children != null) SetExpanded(!expanded);
            if (Tree != null) Tree.Select(this);
        }

        public void SetExpanded(bool on)
        {
            expanded = on;
            if (children != null) children.SetActive(on);
            if (chevron != null && Tree != null) chevron.sprite = on ? Tree.chevronDown : Tree.chevronRight;
            var content = Tree != null ? Tree.transform as RectTransform : transform.parent as RectTransform;
            if (content != null) LayoutRebuilder.MarkLayoutForRebuild(content);
        }

        public void SetSelected(bool on)
        {
            if (selectedMark != null) selectedMark.SetActive(on);
        }
    }
}
