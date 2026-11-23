// Aircraft Hierarchy panel (3D View page): a tree built in the Editor (HELIOS UI.prefab), nothing is created at run
// time. Each row has a HeliosTreeNode: clicking it selects the row and, if it has children, expands / collapses them.
using UnityEngine;

namespace Helios
{
    public class HeliosTree : MonoBehaviour
    {
        [Tooltip("Chevron of a collapsed node.")] public Sprite chevronRight;
        [Tooltip("Chevron of an expanded node.")] public Sprite chevronDown;
        [Tooltip("Row selected when the page opens.")] public HeliosTreeNode selected;

        public void Select(HeliosTreeNode node)
        {
            if (selected == node) return;
            if (selected != null) selected.SetSelected(false);
            selected = node;
            if (selected != null) selected.SetSelected(true);
        }
    }
}
