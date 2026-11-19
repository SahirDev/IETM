// HELIOS - marks the part of the screen where the 3D model is drawn (between the menu and the cards).
// Resize this rectangle in the Scene view to change where the model sits.
using UnityEngine;

namespace Helios
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("HELIOS/3D View Area")]
    public class HeliosViewArea : MonoBehaviour
    {
        public static HeliosViewArea Current { get; private set; }
        static readonly Vector3[] corners = new Vector3[4];

        void OnEnable() { Current = this; }
        void OnDisable() { if (Current == this) Current = null; }

        /// <summary>The area in screen pixels (the UI canvas is a screen overlay, so its world space is the screen).</summary>
        public Rect ScreenRect()
        {
            ((RectTransform)transform).GetWorldCorners(corners);
            float x0 = Mathf.Min(corners[0].x, corners[2].x), x1 = Mathf.Max(corners[0].x, corners[2].x);
            float y0 = Mathf.Min(corners[0].y, corners[2].y), y1 = Mathf.Max(corners[0].y, corners[2].y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
    }
}
