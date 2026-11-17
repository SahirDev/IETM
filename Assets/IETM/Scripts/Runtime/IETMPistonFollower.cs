// Keeps a piston rod inside its cylinder while an animation moves the cylinder and the rod-end.
// Used for Landing_Gear 1.glb, whose exported animation does not slide the "Gear Actuator Piston Rod"
// along the "Gear Actuator" (the rod stayed at a fixed distance and tilted off-axis by up to ~17 deg).
// Rule: rod keeps its rest pose relative to the cylinder, and slides along the cylinder axis by the
// change of the rod-end anchor (the "Actuator Nut") along that axis.
using UnityEngine;

namespace IETM
{
    public class IETMPistonFollower
    {
        readonly Transform cylinder, rod, anchor;
        readonly Matrix4x4 rodInCylinder;     // rod pose relative to cylinder at rest
        readonly Vector3 anchorLocal;         // anchor point in the anchor's own space
        readonly Vector3 anchorInCylRest;     // anchor point in cylinder space at rest
        readonly Vector3 axisInCyl;           // cylinder axis (towards the anchor), cylinder space
        public bool IsValid => cylinder != null && rod != null && anchor != null;

        public IETMPistonFollower(Transform root, string cylinderName, string rodName, string anchorName)
        {
            cylinder = IETMModelSetup.Find(root, cylinderName);
            rod = IETMModelSetup.Find(root, rodName);
            anchor = IETMModelSetup.Find(root, anchorName);
            if (!IsValid) return;

            rodInCylinder = cylinder.worldToLocalMatrix * rod.localToWorldMatrix;
            var ar = anchor.GetComponentInChildren<Renderer>();
            Vector3 anchorWorld = ar != null ? ar.bounds.center : anchor.position;
            anchorLocal = anchor.InverseTransformPoint(anchorWorld);
            anchorInCylRest = cylinder.InverseTransformPoint(anchorWorld);

            var cr = cylinder.GetComponentInChildren<Renderer>();
            Vector3 cylCenter = cylinder.InverseTransformPoint(cr != null ? cr.bounds.center : cylinder.position);
            var dir = anchorInCylRest - cylCenter;
            axisInCyl = dir.sqrMagnitude > 1e-12f ? dir.normalized : Vector3.down;
        }

        /// <summary>Call after the animation has been sampled for this frame.</summary>
        public void Apply()
        {
            if (!IsValid) return;
            Vector3 anchorInCyl = cylinder.InverseTransformPoint(anchor.TransformPoint(anchorLocal));
            float slide = Vector3.Dot(anchorInCyl - anchorInCylRest, axisInCyl);
            var m = cylinder.localToWorldMatrix * rodInCylinder;
            Vector3 pos = (Vector3)m.GetColumn(3) + cylinder.TransformDirection(axisInCyl) * slide;
            rod.SetPositionAndRotation(pos, m.rotation);
        }
    }
}
