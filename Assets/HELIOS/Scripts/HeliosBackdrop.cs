// HELIOS - the background behind the 3D model (the blue glow of the prototype). It is a canvas of its own,
// drawn by the background camera, so you can edit it like the rest of the UI (images, colours, extra shapes).
using UnityEngine;

namespace Helios
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("HELIOS/Backdrop")]
    public class HeliosBackdrop : MonoBehaviour
    {
        [Tooltip("How far from the background camera the backdrop is drawn.")]
        public float distance = 50f;

        void OnValidate() { if (float.IsNaN(distance) || distance < 1f) distance = 50f; }

        public void Attach(Camera backCam)
        {
            var c = GetComponent<Canvas>();
            if (c == null || backCam == null) return;
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = backCam;
            c.planeDistance = Mathf.Min(distance, backCam.farClipPlane * 0.9f);
            gameObject.layer = 5;
        }
    }
}
