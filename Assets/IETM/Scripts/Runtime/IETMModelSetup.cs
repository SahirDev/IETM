// Model preparation helpers, used by the Editor scene builder (pre-configured model)
// and by IETMApp at runtime when the model was not prepared in the Editor.
using System.Collections.Generic;
using UnityEngine;

namespace IETM
{
    public static class IETMModelSetup
    {
        /// <summary>Parents the model under a pivot and rotates it so topNode -> bottomNode points down.</summary>
        public static Transform OrientUpright(GameObject model, string topNode, string bottomNode, Vector3 extraEuler, string pivotName = "IETM Model Pivot")
        {
            if (model == null) return null;
            var pivot = new GameObject(pivotName).transform;
            pivot.SetParent(model.transform.parent, false);
            model.transform.SetParent(pivot, true);

            var top = Find(model.transform, topNode);
            var bottom = string.IsNullOrEmpty(bottomNode) ? null : Find(model.transform, bottomNode);
            if (top != null && bottom != null)
            {
                Vector3 a = top.position, b = bottom.position;
                var rb = bottom.GetComponentInChildren<Renderer>();
                if (rb != null) b = rb.bounds.center;
                Vector3 dir = b - a;
                if (dir.sqrMagnitude > 1e-10f) pivot.rotation = Quaternion.FromToRotation(dir.normalized, Vector3.down) * pivot.rotation;
            }
            pivot.rotation = Quaternion.Euler(extraEuler) * pivot.rotation;
            return pivot;
        }

        /// <summary>Angle (deg) between "align node -> bottom node" and world down; 0 = upright.</summary>
        public static float UprightError(GameObject model, string topNode, string bottomNode)
        {
            if (model == null) return 0f;
            var top = Find(model.transform, topNode);
            var bottom = Find(model.transform, bottomNode);
            if (top == null || bottom == null) return 0f;
            Vector3 b = bottom.position;
            var rb = bottom.GetComponentInChildren<Renderer>();
            if (rb != null) b = rb.bounds.center;
            var dir = b - top.position;
            return dir.sqrMagnitude < 1e-10f ? 0f : Vector3.Angle(dir, Vector3.down);
        }

        /// <summary>
        /// Aligns 'model' with 'reference' using the CAD assembly frame (the parent of alignNode),
        /// which both CAD exports share even though the parts move between gear states.
        /// </summary>
        public static bool AlignToReference(GameObject model, GameObject reference, string alignNode)
        {
            if (model == null || reference == null || string.IsNullOrEmpty(alignNode)) return false;
            var a = Find(reference.transform, alignNode);
            var b = Find(model.transform, alignNode);
            if (a == null || b == null) return false;
            if (a.parent != null && b.parent != null) { a = a.parent; b = b.parent; }
            Quaternion dq = a.rotation * Quaternion.Inverse(b.rotation);
            model.transform.rotation = dq * model.transform.rotation;
            model.transform.position += a.position - b.position;
            return true;
        }

        /// <summary>Adds mesh colliders for picking (cooked at build time when done in the Editor).</summary>
        public static int AddColliders(GameObject go)
        {
            int n = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                n++;
            }
            return n;
        }

        /// <summary>Adds / updates IETMPart components on the nodes named in the IPD data.</summary>
        public static int TagParts(GameObject root, IETMDatabase db)
        {
            int n = 0;
            if (root == null || db == null) return 0;
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var item in db.Leaves())
            {
                foreach (var nodeName in item.nodes)
                    foreach (var t in all)
                    {
                        if (t.name != nodeName) continue;
                        if (!t.TryGetComponent<IETMPart>(out var p)) p = t.gameObject.AddComponent<IETMPart>();
                        p.itemNo = item.itemNo;
                        p.ipdDescription = item.description;
                        p.partNumber = item.partNumber;
                        n++;
                    }
            }
            return n;
        }

        public static Transform Find(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>itemNo -> transforms, from IETMPart components (empty if the model is not tagged).</summary>
        public static Dictionary<string, List<Transform>> PartMap(GameObject root)
        {
            var map = new Dictionary<string, List<Transform>>();
            if (root == null) return map;
            foreach (var p in root.GetComponentsInChildren<IETMPart>(true))
            {
                if (string.IsNullOrEmpty(p.itemNo)) continue;
                if (!map.TryGetValue(p.itemNo, out var l)) map[p.itemNo] = l = new List<Transform>();
                l.Add(p.transform);
            }
            return map;
        }
    }
}
