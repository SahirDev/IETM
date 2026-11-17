// Keeps a GridLayoutGroup at a fixed number of columns that fill the available width.
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    [ExecuteAlways, RequireComponent(typeof(GridLayoutGroup))]
    public class UIGridFit : MonoBehaviour
    {
        [Min(1)] public int columns = 4;
        [Tooltip("Cell height in UI units.")] public float cellHeight = 300f;
        [Tooltip("Narrow screens: fewer columns when a cell would get narrower than this.")] public float minCellWidth = 260f;

        GridLayoutGroup grid;

        void OnEnable() { Fit(); }
        void OnRectTransformDimensionsChange() { Fit(); }
#if UNITY_EDITOR
        void OnValidate() { Fit(); }
#endif

        public void Fit()
        {
            if (grid == null) grid = GetComponent<GridLayoutGroup>();
            var rt = (RectTransform)transform;
            float w = rt.rect.width - grid.padding.left - grid.padding.right;
            if (w <= 1f) return;
            int cols = columns;
            while (cols > 1 && (w - grid.spacing.x * (cols - 1)) / cols < minCellWidth) cols--;
            float cw = Mathf.Floor((w - grid.spacing.x * (cols - 1)) / cols);
            var size = new Vector2(cw, cellHeight);
            if (grid.cellSize != size) grid.cellSize = size;
            if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount || grid.constraintCount != cols)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = cols;
            }
        }
    }
}
