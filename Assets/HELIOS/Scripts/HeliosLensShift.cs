// HELIOS - centres the 3D model in a part of the screen (a page's Model Area, e.g. the space between the Aircraft
// Hierarchy and the view tools on the 3D View page) instead of the middle of the whole 3D view. The camera keeps its
// place; its picture is shifted (off-centre projection), so picking, labels and orbiting stay exact.
// It sits on the Main Camera (added by the scene setup) and works in the Editor Game view and at Play.
using UnityEngine;

namespace Helios
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("HELIOS/Lens Shift (Model Area)")]
    public class HeliosLensShift : MonoBehaviour
    {
        Camera cam;
        static HeliosNavigator nav;
        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>The open page's Model Area inside a camera's picture: its centre and size as 0..1 of the picture.</summary>
        public static bool TryGet(Camera c, out Vector2 centre, out Vector2 size)
        {
            centre = new Vector2(0.5f, 0.5f); size = Vector2.one;
            if (c == null) return false;
            if (nav == null) nav = FindAnyObjectByType<HeliosNavigator>(FindObjectsInactive.Include);
            if (nav == null) return false;
            HeliosPage page = Application.isPlaying ? nav.Current : null;
            if (page == null && nav.pages != null)
                foreach (var p in nav.pages) if (p != null && p.gameObject.activeInHierarchy) { page = p; break; }
            if (page == null || page.modelArea == null || !page.modelArea.gameObject.activeInHierarchy) return false;
            page.modelArea.GetWorldCorners(corners);          // overlay canvas: world = screen pixels
            var view = c.pixelRect;
            if (view.width < 10f || view.height < 10f) return false;
            float x0 = Mathf.Min(corners[0].x, corners[2].x), x1 = Mathf.Max(corners[0].x, corners[2].x);
            float y0 = Mathf.Min(corners[0].y, corners[2].y), y1 = Mathf.Max(corners[0].y, corners[2].y);
            centre = new Vector2(((x0 + x1) * 0.5f - view.x) / view.width, ((y0 + y1) * 0.5f - view.y) / view.height);
            size = new Vector2(Mathf.Clamp((x1 - x0) / view.width, 0.1f, 1f), Mathf.Clamp((y1 - y0) / view.height, 0.1f, 1f));
            return true;
        }

        /// <summary>Distance that fits a sphere of this radius in the Model Area (the whole view without one).</summary>
        public static float FitDistance(Camera c, float radius, float fovDeg, float aspect)
        {
            Vector2 centre, size;
            if (!TryGet(c, out centre, out size)) size = Vector2.one;
            float t = Mathf.Tan(fovDeg * Mathf.Deg2Rad * 0.5f);
            float half = Mathf.Atan(Mathf.Min(t * size.y, t * aspect * size.x));
            return radius / Mathf.Max(1e-4f, Mathf.Sin(half));
        }

        void OnEnable() { cam = GetComponent<Camera>(); }

        void OnPreCull()
        {
            if (cam == null) cam = GetComponent<Camera>();
            cam.ResetProjectionMatrix();
            Vector2 centre, size;
            if (!TryGet(cam, out centre, out size)) return;
            var m = cam.projectionMatrix;
            m[0, 2] = -(centre.x * 2f - 1f);
            m[1, 2] = -(centre.y * 2f - 1f);
            cam.projectionMatrix = m;
        }

        void OnDisable() { if (cam != null) cam.ResetProjectionMatrix(); }
    }
}
