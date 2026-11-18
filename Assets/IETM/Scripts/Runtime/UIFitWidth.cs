// Scales a bar down (never up) so it always fits inside its parent's width - e.g. the viewer toolbar
// when the 3D viewport gets narrow (side panels open, HELIOS shell, small screens).
using UnityEngine;

namespace IETM
{
    [ExecuteAlways]
    public class UIFitWidth : MonoBehaviour
    {
        [Tooltip("Space kept free on the left and right (UI units).")] public float margin = 16f;
        [Range(0.3f, 1f)] public float minScale = 0.55f;

        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            float w = rt.rect.width, avail = parent.rect.width - 2f * margin;
            if (w <= 1f || avail <= 1f) return;
            float s = Mathf.Clamp(avail / w, minScale, 1f);
            if (Mathf.Abs(rt.localScale.x - s) > 0.001f) rt.localScale = new Vector3(s, s, 1f);
        }
    }
}
