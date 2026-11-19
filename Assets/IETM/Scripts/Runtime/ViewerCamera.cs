// Orbit / pan / zoom camera for the IETM 3D viewport, with smooth focus transitions.
using UnityEngine;

namespace IETM
{
    [RequireComponent(typeof(Camera))]
    public class ViewerCamera : MonoBehaviour
    {
        public float rotateSpeed = 0.3f;
        public float smoothing = 12f;
        public float minPitch = -85f, maxPitch = 85f;
        public bool autoRotate;
        public float autoRotateSpeed = 12f;

        public Camera Cam { get; private set; }

        // current
        Vector3 pivot; float yaw = 35f, pitch = 20f, distance = 1f;
        // target
        Vector3 tPivot; float tYaw = 35f, tPitch = 20f, tDistance = 1f;

        float minDist = 0.01f, maxDist = 50f;
        Bounds homeBounds;
        bool dragging;
        int dragButton;
        Vector2 lastMouse;

        /// <summary>Set by the app: true while the pointer is over the 3D viewport (not UI).</summary>
        public bool PointerInViewport { get; set; }
        /// <summary>Set by the app: true while a text field has focus.</summary>
        public bool InputBlocked { get; set; }
        public bool IsDragging => dragging;

        void Awake()
        {
            Cam = GetComponent<Camera>();
        }

        public void SetHome(Bounds b)
        {
            homeBounds = b;
            float r = Mathf.Max(b.extents.magnitude, 0.001f);
            minDist = r * 0.05f;
            maxDist = r * 20f;
            Cam.nearClipPlane = Mathf.Max(0.0005f, r * 0.01f);
            Cam.farClipPlane = r * 100f;
        }

        public void Home(bool instant = false)
        {
            SetView(35f, 20f);
            Frame(homeBounds, instant);
        }

        public void SetView(float yawDeg, float pitchDeg)
        {
            // keep continuity: pick the equivalent yaw closest to current
            float d = Mathf.DeltaAngle(tYaw, yawDeg);
            tYaw = tYaw + d;
            tPitch = Mathf.Clamp(pitchDeg, minPitch, maxPitch);
        }

        /// <summary>Turns the view so the given motion direction runs across the screen (keeps the current side, slight elevation).</summary>
        public void LookAcross(Vector3 motion, float minPitchDeg = 12f, float maxPitchDeg = 40f)
        {
            if (motion.sqrMagnitude < 1e-12f) return;
            var dn = motion.normalized;
            var f = Quaternion.Euler(tPitch, tYaw, 0f) * Vector3.forward;
            var c = f - dn * Vector3.Dot(f, dn);
            if (c.sqrMagnitude < 1e-6f) c = Vector3.Cross(dn, Vector3.up);
            if (c.sqrMagnitude < 1e-6f) c = Vector3.forward;
            c.Normalize();
            float pitchDeg = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(-c.y, -1f, 1f)) * Mathf.Rad2Deg, minPitchDeg, maxPitchDeg);
            float yawDeg = Mathf.Atan2(c.x, c.z) * Mathf.Rad2Deg;
            SetView(yawDeg, pitchDeg);
        }

        /// <summary>True when the camera has (nearly) reached its target view.</summary>
        public bool IsSettled =>
            Mathf.Abs(Mathf.DeltaAngle(yaw, tYaw)) < 1.5f && Mathf.Abs(pitch - tPitch) < 1.5f &&
            Mathf.Abs(distance - Mathf.Max(tDistance, orbitHold > 0f ? orbitHold : 0f)) < Mathf.Max(0.002f, tDistance * 0.03f) &&
            (pivot - tPivot).magnitude < Mathf.Max(0.001f, tDistance * 0.02f) && orbitHold <= 0f;

        public float TargetYaw => tYaw;
        public float TargetPitch => tPitch;
        public Vector3 TargetForward => Quaternion.Euler(tPitch, tYaw, 0f) * Vector3.forward;

        /// <summary>Distance at which the bounds fill the view (same rule as Frame).</summary>
        public float DistanceFor(Bounds b, float padding)
        {
            float r = Mathf.Max(b.extents.magnitude, minDist);
            float fov = Cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float fovH = Mathf.Atan(Mathf.Tan(fov) * Mathf.Max(0.3f, Cam.aspect));
            return Mathf.Clamp(r * padding / Mathf.Sin(Mathf.Min(fov, fovH)), minDist, maxDist);
        }

        /// <summary>Orbits to a new pivot / direction / distance (smoothly).</summary>
        [Tooltip("Speed of the automatic orbit when the camera follows a part (lower = slower, smoother).")]
        public float orbitSmoothing = 3.5f;
        float orbitHold;

        public void OrbitTo(Vector3 newPivot, Vector3 forward, float newDistance, float safeRadius = 0f)
        {
            orbitHold = Mathf.Max(distance, newDistance, safeRadius);
            forward.Normalize();
            float p = Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float y = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            SetView(y, p);
            tPivot = newPivot;
            tDistance = Mathf.Clamp(newDistance, minDist, maxDist);
        }

        public void SetPreset(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "front": case "forward": SetView(90f, 0f); break;
                case "side": case "starboard": SetView(0f, 0f); break;
                case "back": case "aft": SetView(-90f, 0f); break;
                case "port": SetView(180f, 0f); break;
                case "top": SetView(90f, 85f); break;
                case "bottom": SetView(90f, -85f); break;
                case "iso": SetView(35f, 20f); break;
            }
        }

        public void Frame(Bounds b, bool instant = false, float padding = 1.15f)
        {
            float r = Mathf.Max(b.extents.magnitude, minDist);
            float fov = Cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float aspect = Mathf.Max(0.3f, Cam.aspect);
            float fovH = Mathf.Atan(Mathf.Tan(fov) * aspect);
            float f = Mathf.Min(fov, fovH);
            tPivot = b.center;
            tDistance = Mathf.Clamp(r * padding / Mathf.Sin(f), minDist, maxDist);
            if (instant) Snap();
        }

        public void Snap()
        {
            pivot = tPivot; yaw = tYaw; pitch = tPitch; distance = tDistance;
            Apply();
        }

        void LateUpdate()
        {
            HandleInput();

            if (autoRotate && !dragging) tYaw += autoRotateSpeed * Time.unscaledDeltaTime;

            float k = 1f - Mathf.Exp(-(orbitHold > 0f ? orbitSmoothing : smoothing) * Time.unscaledDeltaTime);
            pivot = Vector3.Lerp(pivot, tPivot, k);
            yaw = Mathf.Lerp(yaw, tYaw, k);
            pitch = Mathf.Lerp(pitch, tPitch, k);
            // orbit transitions: stay out at the safe radius while turning, close in only when the turn is done
            float turnLeft = Mathf.Abs(Mathf.DeltaAngle(yaw, tYaw)) + Mathf.Abs(pitch - tPitch);
            float dTarget = orbitHold > 0f && turnLeft > 6f ? Mathf.Max(orbitHold, tDistance) : tDistance;
            if (orbitHold > 0f && turnLeft <= 6f) orbitHold = 0f;
            if (dragging) orbitHold = 0f;
            distance = Mathf.Lerp(distance, dTarget, k);
            Apply();
        }

        void Apply()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = pivot - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }

        void HandleInput()
        {
            Vector2 m = InputCompat.MousePosition;

            if (!dragging && PointerInViewport)
            {
                if (InputCompat.LeftDown) { dragging = true; dragButton = 0; lastMouse = m; }
                else if (InputCompat.RightDown) { dragging = true; dragButton = 1; lastMouse = m; }
                else if (InputCompat.MiddleDown) { dragging = true; dragButton = 2; lastMouse = m; }
            }

            if (dragging)
            {
                bool held = dragButton == 0 ? InputCompat.LeftHeld : dragButton == 1 ? InputCompat.RightHeld : InputCompat.MiddleHeld;
                if (!held) { dragging = false; }
                else
                {
                    Vector2 d = m - lastMouse;
                    lastMouse = m;
                    bool pan = dragButton != 0 || InputCompat.Shift;
                    if (pan)
                    {
                        float scale = tDistance * 2f * Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1f, Cam.pixelHeight);
                        tPivot -= (transform.right * d.x + transform.up * d.y) * scale;
                    }
                    else
                    {
                        tYaw += d.x * rotateSpeed;
                        tPitch = Mathf.Clamp(tPitch - d.y * rotateSpeed, minPitch, maxPitch);
                    }
                }
            }

            if (PointerInViewport && !InputBlocked)
            {
                float s = InputCompat.Scroll;
                if (Mathf.Abs(s) > 0.001f) tDistance = Mathf.Clamp(tDistance * Mathf.Pow(0.85f, s), minDist, maxDist);
            }
        }

        public void Zoom(float factor)
        {
            tDistance = Mathf.Clamp(tDistance * factor, minDist, maxDist);
        }
    }
}
