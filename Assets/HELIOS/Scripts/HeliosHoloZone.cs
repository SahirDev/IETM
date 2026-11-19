// HELIOS - a highlight zone for the hologram: everything of the model inside this box glows in its colour
// (like the orange avionics bay, green hydraulic lines or blue engine of the HELIOS design).
// Add it to an empty object (HELIOS > Hologram Zone), then move, rotate and scale the object to fit the part:
// the box is the object's scale (a 1 x 1 x 1 cube). Up to 12 zones show at once, the model's own zones included.
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    [ExecuteAlways]
    [AddComponentMenu("HELIOS/Hologram Zone")]
    public class HeliosHoloZone : MonoBehaviour
    {
        public static readonly List<HeliosHoloZone> All = new List<HeliosHoloZone>();

        public Color color = new Color32(251, 146, 60, 255);
        [Tooltip("How strongly the zone glows (0 = only its lines change colour).")]
        [Range(0f, 2f)] public float glow = 1f;
        [Tooltip("The pages (looks) it shows on.")]
        public bool onHealth = true, onIntegrity = true, onMaintenance = true;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void OnValidate() { glow = Mathf.Clamp(glow, 0f, 2f); if (float.IsNaN(glow)) glow = 1f; }

        public bool ShowsOn(ModelLook l)
        {
            return l == ModelLook.HoloHealth ? onHealth : l == ModelLook.HoloIntegrity ? onIntegrity : l == ModelLook.HoloMaintenance && onMaintenance;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.12f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
