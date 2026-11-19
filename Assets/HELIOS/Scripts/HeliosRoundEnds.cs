// HELIOS - keeps the ends of a capsule, pill or cylinder shape perfectly round at any size.
// A sliced Image keeps its corners at the sprite's size, so a capsule drawn shorter than its texture gets oval ends.
// This sets the Image's "Pixels Per Unit Multiplier" from the rectangle's short side, so the ends stay half-circles.
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("HELIOS/Round Ends")]
    public class HeliosRoundEnds : MonoBehaviour
    {
        Image img;

        void OnEnable() { Apply(); }
        void OnRectTransformDimensionsChange() { Apply(); }
#if UNITY_EDITOR
        void OnValidate() { Apply(); }
#endif

        public void Apply()
        {
            if (img == null) img = GetComponent<Image>();
            if (img == null || img.sprite == null) return;
            var b = img.sprite.border;
            float across = Mathf.Max(b.x + b.z, b.y + b.w);                  // the sprite's round end, in pixels
            var r = ((RectTransform)transform).rect;
            float shortSide = Mathf.Min(r.width, r.height);
            if (across <= 0f || shortSide <= 0.5f) return;
            float m = across / shortSide;
            if (Mathf.Abs(img.pixelsPerUnitMultiplier - m) > 0.001f) img.pixelsPerUnitMultiplier = m;
        }

        /// <summary>True for the sprites that need round ends (pill, capsule, cylinder shapes).</summary>
        public static bool Wants(Sprite s)
        {
            if (s == null) return false;
            string n = s.name.ToLowerInvariant();
            return n.StartsWith("pill") || n.StartsWith("capsule") || n.StartsWith("cylinder");
        }
    }
}
