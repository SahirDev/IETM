// Drives a spinning lead screw (the "Gear Actuator Piston Rod") from the travel of its nut.
//
// Why: in Landing_Gear 1.glb the screw spins ~49 turns during retraction - up to ~180 deg or more between
// two 24 fps keys. Blender evaluates its own curves per frame and looks right, but in glTF/Unity the
// rotation between keys is interpolated the short way round, so the screw jitters and spins backwards.
// A lead screw's angle is simply proportional to the nut's travel, so we compute it from that instead.
//
// The ratio is 514.3 deg per mm of nut travel (fitted to the GLB keys: within 0.5 deg over the whole cycle).
// The spin direction is measured from the clip's first frames, where the key-to-key steps are still
// unambiguous (Unity mirrors one axis on glTF import, so the sign is not fixed).
using UnityEngine;

namespace IETM
{
    public class IETMScrewDriver
    {
        readonly Transform body, screw, nut;
        Matrix4x4 screwInBody;     // screw pose relative to the body at rest (extended)
        Vector3 pivotInBody;       // a point on the screw axis (body space)
        Vector3 axisInBody;        // screw axis, towards the nut (body space)
        Vector3 nutLocal;          // nut reference point in the nut's own space
        Vector3 nutInBodyRest;     // that point in body space at rest
        public float degreesPerMetre = 514300f; // magnitude from the GLB; sign measured in Calibrate()
        public bool IsValid => body != null && screw != null && nut != null;

        public IETMScrewDriver(Transform root, string bodyName, string screwName, string nutName)
        {
            body = IETMModelSetup.Find(root, bodyName);
            screw = IETMModelSetup.Find(root, screwName);
            nut = IETMModelSetup.Find(root, nutName);
            if (IsValid) CaptureRest();
        }

        /// <summary>Call with the model in its rest (extended) pose.</summary>
        public void CaptureRest()
        {
            screwInBody = body.worldToLocalMatrix * screw.localToWorldMatrix;
            var sr = screw.GetComponentInChildren<Renderer>();
            var nr = nut.GetComponentInChildren<Renderer>();
            Vector3 screwCenter = sr != null ? sr.bounds.center : screw.position;
            Vector3 nutCenter = nr != null ? nr.bounds.center : nut.position;
            pivotInBody = body.InverseTransformPoint(screwCenter);
            nutLocal = nut.InverseTransformPoint(nutCenter);
            nutInBodyRest = body.InverseTransformPoint(nutCenter);
            var dir = nutInBodyRest - pivotInBody;
            axisInBody = dir.sqrMagnitude > 1e-12f ? dir.normalized : Vector3.down;
        }

        float NutSlide()
        {
            Vector3 n = body.InverseTransformPoint(nut.TransformPoint(nutLocal));
            return Vector3.Dot(n - nutInBodyRest, axisInBody);
        }

        Quaternion RelativeRotation() => Quaternion.Inverse(body.rotation) * screw.rotation;

        /// <summary>
        /// Measures the spin ratio from the clip's first key frames (each step must be &lt; 150 deg).
        /// Leaves the model sampled at 0 s afterwards.
        /// </summary>
        public void Calibrate(AnimationClip clip, GameObject root, float maxTime, float degreesPerMm)
        {
            if (!IsValid || clip == null) return;
            float fr = clip.frameRate > 0f ? clip.frameRate : 24f;
            clip.SampleAnimation(root, 0f);
            Quaternion prev = RelativeRotation();
            float angle = 0f, slide = 0f;
            for (int k = 1; k <= Mathf.CeilToInt(maxTime * fr); k++)
            {
                clip.SampleAnimation(root, k / fr);
                Quaternion rel = RelativeRotation();
                Quaternion step = Quaternion.Inverse(prev) * rel;
                step.ToAngleAxis(out float a, out Vector3 ax);
                if (a > 180f) { a = 360f - a; ax = -ax; }
                if (a > 150f) break;                         // beyond this the keys become ambiguous
                // step axis is in the screw's rest frame; bring it to body space
                Vector3 axBody = prev * ax;
                angle += a * Mathf.Sign(Vector3.Dot(axBody, axisInBody));
                slide = NutSlide();
                prev = rel;
            }
            float magnitude = Mathf.Abs(degreesPerMm) * 1000f;
            if (Mathf.Abs(slide) > 0.0002f && Mathf.Abs(angle) > 30f)
                degreesPerMetre = Mathf.Sign(angle / slide) * (magnitude > 0f ? magnitude : Mathf.Abs(angle / slide));
            else degreesPerMetre = Mathf.Sign(degreesPerMetre) * magnitude;
            clip.SampleAnimation(root, 0f);
            Debug.Log($"[IETM] Screw ratio: {degreesPerMetre / 1000f:0.#} deg per mm of nut travel.");
        }

        /// <summary>Call after the animation has been sampled for this frame.</summary>
        public void Apply()
        {
            if (!IsValid) return;
            float angle = degreesPerMetre * NutSlide();
            var spin = Matrix4x4.Translate(pivotInBody) * Matrix4x4.Rotate(Quaternion.AngleAxis(angle, axisInBody)) * Matrix4x4.Translate(-pivotInBody);
            var m = body.localToWorldMatrix * spin * screwInBody;
            screw.SetPositionAndRotation(m.GetColumn(3), m.rotation);
        }
    }
}
