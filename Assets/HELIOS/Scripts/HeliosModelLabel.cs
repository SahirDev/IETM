// HELIOS - a label that points at a place on the model (Environment, Strain Gauging, Vibration).
// While playing, the dot follows the point on the model and the box stays at "Box Offset" from it.
// Edit the box (texts, colours, size) like any other UI; move the point with "Custom Target" or "Point".
using UnityEngine;

namespace Helios
{
    public enum HeliosLabelPoint { Humidity = 0, Strain = 1, Vibration = 2, EngineRpm = 3, OilPressure = 4, FuelFlow = 5, Hydraulic = 6 }

    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Model Label")]
    public class HeliosModelLabel : MonoBehaviour
    {
        [Tooltip("The place on the model (the HeliosApp's Humidity / Strain / Vibration point).")]
        public HeliosLabelPoint point = HeliosLabelPoint.Humidity;
        [Tooltip("Optional: any object to point at instead.")]
        public Transform customTarget;
        [Tooltip("Where the box sits from the dot, in UI units (x right, y up).")]
        public Vector2 boxOffset = new Vector2(-64f, 96f);
        [Tooltip("Keep the box inside the 3D view area.")]
        public bool keepInView = true;
        public RectTransform box, line, dot, glow;

        static readonly Vector3[] corners = new Vector3[4];

        // set by HeliosSensorFocus: the label shows only for the sensor that was clicked, once the camera has moved
        [System.NonSerialized] public bool hiddenByFocus;
        float showAt;

        /// <summary>Shows or hides the label for Sensor Focus (shown after a delay, while the camera glides there).</summary>
        public void SetFocusVisible(bool on, float delay) { hiddenByFocus = !on; showAt = Time.unscaledTime + delay; }

        void OnValidate() { if (!System.Enum.IsDefined(typeof(HeliosLabelPoint), point)) point = HeliosLabelPoint.Humidity; }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            var app = HeliosApp.Instance;
            var cam = app != null ? app.ViewCamera : null;
            bool on = cam != null && cam.enabled && !hiddenByFocus && Time.unscaledTime >= showAt;
            Vector3 sp = Vector3.zero;
            if (on) { sp = cam.WorldToScreenPoint(customTarget != null ? customTarget.position : app.LabelPoint((int)point)); on = sp.z > 0f; }
            Show(on);
            if (!on) return;
            var self = (RectTransform)transform;
            Vector3 p3 = self.InverseTransformPoint(new Vector3(sp.x, sp.y, 0f));
            var p = new Vector2(p3.x, p3.y);
            var c = p + boxOffset;
            if (keepInView && box != null && HeliosViewArea.Current != null)
            {
                ((RectTransform)HeliosViewArea.Current.transform).GetWorldCorners(corners);
                Vector3 a = self.InverseTransformPoint(corners[0]), b = self.InverseTransformPoint(corners[2]);
                var half = new Vector2(box.rect.width, box.rect.height) * 0.5f;
                c.x = Mathf.Clamp(c.x, Mathf.Min(a.x, b.x) + half.x, Mathf.Max(Mathf.Min(a.x, b.x) + half.x, Mathf.Max(a.x, b.x) - half.x));
                c.y = Mathf.Clamp(c.y, Mathf.Min(a.y, b.y) + half.y + 50f, Mathf.Max(Mathf.Min(a.y, b.y) + half.y + 50f, Mathf.Max(a.y, b.y) - half.y - 60f));
            }
            if (box != null) box.localPosition = new Vector3(c.x, c.y, 0f);
            if (dot != null) dot.localPosition = new Vector3(p.x, p.y, 0f);
            if (glow != null) glow.localPosition = new Vector3(p.x, p.y, 0f);
            if (line != null)
            {
                var d = c - p;
                line.localPosition = new Vector3(p.x, p.y, 0f);
                line.sizeDelta = new Vector2(d.magnitude, line.sizeDelta.y);
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        void Show(bool on)
        {
            foreach (var r in new[] { box, line, dot, glow })
                if (r != null && r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
        }
    }
}
