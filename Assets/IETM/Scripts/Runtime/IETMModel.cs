// Binds IPD items to the 3D model, and handles highlight / ghost / hide / isolate,
// exploded view, part removal animation, picking and gear-state (model variant) switching.
using System.Collections.Generic;
using UnityEngine;

namespace IETM
{
    public class PartBinding
    {
        public IPDItem item;
        public readonly List<Transform> nodes = new List<Transform>();
        public readonly List<Vector3> baseLocal = new List<Vector3>();
        public readonly List<Renderer> renderers = new List<Renderer>();
        public readonly List<Material[]> originals = new List<Material[]>();
        public Vector3 explodeDir = Vector3.up;
        /// <summary>Designed explode travel per node, in the node's parent space (Unity axes). See ExplodeOffset.</summary>
        public readonly List<Vector3> designLocal = new List<Vector3>();
        public bool hasDesign;
        /// <summary>Designed travel when a procedure removes the part (see IETMDatabase.removal).</summary>
        public readonly List<Vector3> removalLocal = new List<Vector3>();
        /// <summary>Corner of the L-shaped removal path (node parent space, Unity axes).</summary>
        public readonly List<Vector3> removalViaLocal = new List<Vector3>();
        public bool hasRemoval;
        public Vector3 baseCenter;
        public bool IsBound => renderers.Count > 0;

        public Bounds CurrentBounds()
        {
            bool has = false;
            var b = new Bounds();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }

    public class ModelVariant
    {
        public string name;
        public GameObject root;
        public readonly Dictionary<IPDItem, PartBinding> parts = new Dictionary<IPDItem, PartBinding>();
        public readonly Dictionary<Collider, PartBinding> byCollider = new Dictionary<Collider, PartBinding>();
        public Bounds bounds;
        public float radius;
        public bool hasDesign;
    }

    public enum PartVisual { Normal, Hover, Selected, Ghost, Hidden }

    public class IETMModel
    {
        public readonly List<ModelVariant> variants = new List<ModelVariant>();
        public ModelVariant Active { get; private set; }
        public int ActiveIndex { get; private set; }

        // State shared across variants (keyed by IPD leaf item)
        public readonly HashSet<IPDItem> selected = new HashSet<IPDItem>();
        public readonly HashSet<IPDItem> hidden = new HashSet<IPDItem>();
        public HashSet<IPDItem> isolated; // null = none
        public IPDItem hovered;
        public bool xray;
        /// <summary>True while an animation (gear extend / retract) owns the part transforms.</summary>
        public bool poseLocked;
        public float explode;      // 0..1
        /// <summary>Use the designed (collision-free) explode offsets from the data when available. Off = radial.</summary>
        public bool designedExplode = true;
        public bool HasDesignedExplode => Active != null && Active.hasDesign;
        public readonly Dictionary<IPDItem, float> removeT = new Dictionary<IPDItem, float>();
        public readonly Dictionary<IPDItem, float> removeTarget = new Dictionary<IPDItem, float>();

        Material selMat, hoverMat, ghostMat;
        readonly IETMDatabase db;
        bool dirty = true;
        public Color selectColor = new Color(0.13f, 0.83f, 0.93f);
        public Color hoverColor = new Color(0.98f, 0.75f, 0.25f);

        public IETMModel(IETMDatabase db, Shader highlightShader, Shader ghostShader)
        {
            this.db = db;
            selMat = MakeMat(highlightShader, selectColor, "IETM_Selected");
            hoverMat = MakeMat(highlightShader, hoverColor, "IETM_Hover");
            ghostMat = MakeGhost(ghostShader);
        }

        static Material MakeMat(Shader s, Color c, string name)
        {
            if (s == null) s = Shader.Find("IETM/Highlight");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(s) { name = name };
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", Color.Lerp(c, Color.white, 0.6f));
            return m;
        }

        static Material MakeGhost(Shader s)
        {
            if (s == null) s = Shader.Find("IETM/Ghost");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(s) { name = "IETM_Ghost" };
            var c = new Color(0.45f, 0.65f, 1f, 0.06f);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.renderQueue = 3000;
            return m;
        }

        // ------------------------------------------------------------------ binding

        /// <summary>
        /// Registers a model variant (gear state). Parts are found through IETMPart components
        /// (set up in the Editor) or, if the model has none, through the node names in the IPD data.
        /// </summary>
        public ModelVariant AddVariant(string name, GameObject root, ModelVariant alignTo = null, string alignNode = null, bool align = true)
        {
            var v = new ModelVariant { name = name, root = root };
            if (root == null) return v;

            if (align && alignTo != null && alignTo.root != null)
                IETMModelSetup.AlignToReference(root, alignTo.root, alignNode);

            var partMap = IETMModelSetup.PartMap(root);
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var item in db.Leaves())
            {
                var pb = new PartBinding { item = item };
                var nodes = new List<Transform>();
                if (partMap.Count > 0)
                {
                    if (partMap.TryGetValue(item.itemNo, out var tagged)) nodes.AddRange(tagged);
                }
                else
                {
                    foreach (var nodeName in item.nodes)
                        foreach (var t in all) if (t.name == nodeName) nodes.Add(t);
                }

                foreach (var t in nodes)
                {
                    if (pb.nodes.Contains(t)) continue;
                    pb.nodes.Add(t);
                    pb.baseLocal.Add(t.localPosition);
                    var eo = FindExplode(t.name);
                    pb.designLocal.Add(eo != null ? new Vector3(-eo.offset[0], eo.offset[1], eo.offset[2]) : Vector3.zero);
                    if (eo != null) { pb.hasDesign = true; v.hasDesign = true; }
                    var ro = FindOffset(db.removal, t.name);
                    var remLocal = ro != null ? new Vector3(-ro.offset[0], ro.offset[1], ro.offset[2]) : Vector3.zero;
                    pb.removalLocal.Add(remLocal);
                    var vo = FindOffset(db.removalVia, t.name);
                    pb.removalViaLocal.Add(vo != null ? new Vector3(-vo.offset[0], vo.offset[1], vo.offset[2])
                        : db.removalLPath ? AutoVia(remLocal) : remLocal);   // via == end: straight move
                    if (ro != null) pb.hasRemoval = true;
                    foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    {
                        if (pb.renderers.Contains(r)) continue;
                        pb.renderers.Add(r);
                        pb.originals.Add(r.sharedMaterials);
                        var col = r.GetComponent<Collider>();
                        if (col == null)
                        {
                            var mf = r.GetComponent<MeshFilter>();
                            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable)
                            {
                                var mc = r.gameObject.AddComponent<MeshCollider>();
                                mc.sharedMesh = mf.sharedMesh;
                                col = mc;
                            }
                            else col = r.gameObject.AddComponent<BoxCollider>();
                        }
                        v.byCollider[col] = pb;
                    }
                }
                if (!pb.IsBound) Debug.LogWarning($"[IETM] Item {item.itemNo} '{item.description}' has no model part in '{name}'.");
                v.parts[item] = pb;
            }

            // assembly bounds + explode directions
            bool has = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!has) { v.bounds = r.bounds; has = true; } else v.bounds.Encapsulate(r.bounds);
            }
            v.radius = Mathf.Max(0.001f, v.bounds.extents.magnitude);
            foreach (var pb in v.parts.Values)
            {
                if (!pb.IsBound) continue;
                pb.baseCenter = pb.CurrentBounds().center;
                var d = pb.baseCenter - v.bounds.center;
                pb.explodeDir = d.sqrMagnitude < 1e-8f ? Vector3.up : d.normalized;
            }

            variants.Add(v);
            if (Active == null) { Active = v; ActiveIndex = variants.Count - 1; }
            else root.SetActive(false);
            dirty = true;
            return v;
        }

        public void SetVariant(int index)
        {
            if (index < 0 || index >= variants.Count || variants[index].root == null) return;
            for (int i = 0; i < variants.Count; i++) if (variants[i].root) variants[i].root.SetActive(i == index);
            Active = variants[index];
            ActiveIndex = index;
            dirty = true;
            ApplyOffsets();
        }

        // ------------------------------------------------------------------ state helpers

        public List<IPDItem> LeavesOf(IPDItem item)
        {
            var l = new List<IPDItem>();
            if (item != null) item.CollectLeaves(l);
            return l;
        }

        public void SetSelection(IEnumerable<IPDItem> leaves)
        {
            selected.Clear();
            if (leaves != null) foreach (var l in leaves) selected.Add(l);
            dirty = true;
        }

        public void SetHover(IPDItem leaf) { if (hovered != leaf) { hovered = leaf; dirty = true; } }

        public void SetHidden(IPDItem leaf, bool h)
        {
            if (h) hidden.Add(leaf); else hidden.Remove(leaf);
            dirty = true;
        }

        public void ShowAll()
        {
            hidden.Clear(); isolated = null; dirty = true;
        }

        public void Isolate(IEnumerable<IPDItem> leaves)
        {
            isolated = leaves == null ? null : new HashSet<IPDItem>(leaves);
            if (isolated != null && isolated.Count == 0) isolated = null;
            dirty = true;
        }

        public void SetXRay(bool on) { xray = on; dirty = true; }
        public void MarkDirty() { dirty = true; }

        public void SetRemoved(IPDItem leaf, bool removed)
        {
            removeTarget[leaf] = removed ? 1f : 0f;
            if (!removeT.ContainsKey(leaf)) removeT[leaf] = 0f;
        }

        public void RestoreAll()
        {
            var keys = new List<IPDItem>(removeTarget.Keys);
            foreach (var k in keys) removeTarget[k] = 0f;
        }

        /// <summary>Puts every removed part back instantly (no animation).</summary>
        public void ClearRemovalsImmediate()
        {
            var keys = new List<IPDItem>(removeTarget.Keys);
            foreach (var k in keys) { removeTarget[k] = 0f; removeT[k] = 0f; }
            ApplyOffsets();
            dirty = true;
        }

        /// <summary>
        /// Re-reads the parts' current local positions as their base pose (call after an animation
        /// such as gear extend / retract has moved them), and recomputes bounds and explode directions.
        /// </summary>
        public void RecaptureBase()
        {
            if (Active == null) return;
            var v = Active;
            foreach (var pb in v.parts.Values)
                for (int i = 0; i < pb.nodes.Count; i++)
                    if (pb.nodes[i] != null) pb.baseLocal[i] = pb.nodes[i].localPosition;
            bool has = false;
            foreach (var r in v.root.GetComponentsInChildren<Renderer>())
            {
                if (!has) { v.bounds = r.bounds; has = true; } else v.bounds.Encapsulate(r.bounds);
            }
            v.radius = Mathf.Max(0.001f, v.bounds.extents.magnitude);
            foreach (var pb in v.parts.Values)
            {
                if (!pb.IsBound) continue;
                pb.baseCenter = pb.CurrentBounds().center;
                var d = pb.baseCenter - v.bounds.center;
                pb.explodeDir = d.sqrMagnitude < 1e-8f ? Vector3.up : d.normalized;
            }
        }

        public bool IsRemoved(IPDItem leaf) => removeT.TryGetValue(leaf, out var t) && t > 0.97f;

        /// <summary>Removed parts stay visible, laid out next to the assembly (disassembly / assembly procedures).</summary>
        public bool showRemoved;
        /// <summary>Seconds a part takes to move out / back in (procedures). Set from IETM App.</summary>
        public float removalDuration = 2.5f;
        /// <summary>While true, parts wait in place (the camera is still moving to them).</summary>
        public bool holdRemovals;

        public bool IsVisible(IPDItem leaf) => !hidden.Contains(leaf) && (showRemoved || !IsRemoved(leaf));

        public bool IsGhosted(IPDItem leaf)
        {
            if (isolated != null && !isolated.Contains(leaf)) return true;
            if (xray && !selected.Contains(leaf)) return true;
            return false;
        }

        public PartVisual VisualOf(IPDItem leaf)
        {
            if (!IsVisible(leaf)) return PartVisual.Hidden;
            if (selected.Contains(leaf)) return PartVisual.Selected;
            if (hovered == leaf && !IsGhosted(leaf)) return PartVisual.Hover;
            if (IsGhosted(leaf)) return PartVisual.Ghost;
            if (hovered == leaf) return PartVisual.Hover;
            return PartVisual.Normal;
        }

        // ------------------------------------------------------------------ per-frame

        public void Tick(float dt)
        {
            // removal animation (held while the camera moves to the part)
            bool moving = false;
            var keys = new List<IPDItem>(removeTarget.Keys);
            if (holdRemovals) keys.Clear();
            foreach (var k in keys)
            {
                float cur = removeT.TryGetValue(k, out var c) ? c : 0f;
                float tgt = removeTarget[k];
                if (Mathf.Abs(cur - tgt) > 0.0001f)
                {
                    bool wasRemoved = cur > 0.97f;
                    cur = Mathf.MoveTowards(cur, tgt, dt / Mathf.Max(0.05f, removalDuration));
                    removeT[k] = cur;
                    moving = true;
                    if (wasRemoved != (cur > 0.97f)) dirty = true;
                }
            }
            if (moving) ApplyOffsets();

            // pulse the selection colour
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            var c2 = Color.Lerp(selectColor, Color.white, p * 0.25f);
            if (selMat.HasProperty("_Color")) selMat.SetColor("_Color", c2);
            if (selMat.HasProperty("_BaseColor")) selMat.SetColor("_BaseColor", c2);

            if (dirty) RefreshVisuals();
        }

        ExplodeOffset FindExplode(string node) => FindOffset(db.explode, node);

        /// <summary>Default corner of the L path: the removal's largest axis component (pull out along the pin / shaft first).</summary>
        public static Vector3 AutoVia(Vector3 d)
        {
            var a = new Vector3(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(d.x, 0, 0);
            if (a.y >= a.z) return new Vector3(0, d.y, 0);
            return new Vector3(0, 0, d.z);
        }

        /// <summary>Point on the L-shaped removal path at 0..1 (constant speed along both legs).</summary>
        public static Vector3 AlongPath(Vector3 via, Vector3 end, float e)
        {
            float l1 = via.magnitude, l2 = (end - via).magnitude, L = l1 + l2;
            if (L < 1e-9f) return Vector3.zero;
            float s = e * L;
            return s <= l1 ? via * (l1 > 1e-9f ? s / l1 : 0f) : via + (end - via) * ((s - l1) / Mathf.Max(1e-9f, l2));
        }

        static ExplodeOffset FindOffset(List<ExplodeOffset> list, string node)
        {
            if (list == null) return null;
            foreach (var e in list)
                if (e != null && e.node == node && e.offset != null && e.offset.Length >= 3) return e;
            return null;
        }

        public void SetExplode(float e)
        {
            explode = Mathf.Clamp01(e);
            ApplyOffsets();
        }

        public void ApplyOffsets()
        {
            if (Active == null || poseLocked) return;
            float r = Active.radius;
            foreach (var pb in Active.parts.Values)
            {
                if (!pb.IsBound) continue;
                float rt = removeT.TryGetValue(pb.item, out var t) ? t : 0f;
                float ease = rt * rt * (3f - 2f * rt);
                bool design = designedExplode && Active.hasDesign;
                for (int i = 0; i < pb.nodes.Count; i++)
                {
                    var n = pb.nodes[i];
                    if (n == null) continue;
                    var parent = n.parent;
                    Vector3 offset;
                    if (designedExplode && pb.hasRemoval && ease > 0f)
                    {
                        // laid-out removal position (collision-free disassembly layout) + any explode
                        var remL = AlongPath(pb.removalViaLocal[i], pb.removalLocal[i], ease);
                        Vector3 rem = parent ? parent.TransformVector(remL) : remL;
                        Vector3 ex = design ? (parent ? parent.TransformVector(pb.designLocal[i]) : pb.designLocal[i]) * explode
                                            : pb.explodeDir * (explode * r * 0.9f);
                        offset = rem + ex;
                    }
                    else if (design)
                    {
                        // designed layout: every part slides along its own assembly axis, nothing collides
                        Vector3 d = parent ? parent.TransformVector(pb.designLocal[i]) : pb.designLocal[i];
                        Vector3 dir = d.sqrMagnitude > 1e-12f ? d.normalized : pb.explodeDir;
                        offset = d * explode + dir * (ease * r * 1.4f) + Vector3.up * (ease * r * 0.4f);
                    }
                    else
                        offset = pb.explodeDir * (explode * r * 0.9f) + pb.explodeDir * (ease * r * 1.4f) + Vector3.up * (ease * r * 0.4f);
                    Vector3 basePos = parent ? parent.TransformPoint(pb.baseLocal[i]) : pb.baseLocal[i];
                    n.position = basePos + offset;
                }
            }
        }

        public void RefreshVisuals()
        {
            dirty = false;
            if (Active == null) return;
            foreach (var pb in Active.parts.Values)
            {
                var vis = VisualOf(pb.item);
                // a part being removed stays visible while it moves
                if (vis == PartVisual.Hidden && !hidden.Contains(pb.item) && removeT.TryGetValue(pb.item, out var rt) && rt < 0.97f) vis = PartVisual.Normal;
                for (int i = 0; i < pb.renderers.Count; i++)
                {
                    var r = pb.renderers[i];
                    if (r == null) continue;
                    r.enabled = vis != PartVisual.Hidden;
                    var orig = pb.originals[i];
                    Material m = null;
                    switch (vis)
                    {
                        case PartVisual.Selected: m = selMat; break;
                        case PartVisual.Hover: m = hoverMat; break;
                        case PartVisual.Ghost: m = ghostMat; break;
                    }
                    if (m == null) { r.sharedMaterials = orig; }
                    else
                    {
                        var arr = new Material[Mathf.Max(1, orig.Length)];
                        for (int k = 0; k < arr.Length; k++) arr[k] = m;
                        r.sharedMaterials = arr;
                    }
                    r.shadowCastingMode = vis == PartVisual.Ghost ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }
        }

        // ------------------------------------------------------------------ picking

        readonly RaycastHit[] hits = new RaycastHit[64];

        /// <summary>How many of the points are hidden behind other visible (non-ghosted) parts, seen from 'eye'.</summary>
        public int CountOccluded(Vector3 eye, IList<Vector3> points, HashSet<IPDItem> targets)
        {
            if (Active == null) return 0;
            Physics.SyncTransforms();
            int occ = 0;
            foreach (var p in points)
            {
                var d = p - eye; float len = d.magnitude;
                if (len < 1e-5f) continue;
                int n = Physics.RaycastNonAlloc(eye, d / len, hits, len * 0.97f, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    if (!Active.byCollider.TryGetValue(hits[i].collider, out var pb)) continue;
                    if (targets.Contains(pb.item) || !IsVisible(pb.item) || IsGhosted(pb.item)) continue;
                    occ++; break;
                }
            }
            return occ;
        }

        /// <summary>True when a camera at 'eye' would sit inside or touch a visible part.</summary>
        public bool InsideParts(Vector3 eye, float radius)
        {
            if (Active == null) return false;
            Physics.SyncTransforms();
            foreach (var c in Physics.OverlapSphere(eye, radius, ~0, QueryTriggerInteraction.Ignore))
                if (Active.byCollider.TryGetValue(c, out var pb) && IsVisible(pb.item)) return true;
            return false;
        }

        public IPDItem Pick(Ray ray)
        {
            if (Active == null) return null;
            Physics.SyncTransforms();
            int n = Physics.RaycastNonAlloc(ray, hits, 1000f, ~0, QueryTriggerInteraction.Ignore);
            IPDItem best = null, bestGhost = null;
            float bd = float.MaxValue, bgd = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (!Active.byCollider.TryGetValue(h.collider, out var pb)) continue;
                if (!IsVisible(pb.item)) continue;
                if (IsGhosted(pb.item))
                {
                    if (h.distance < bgd) { bgd = h.distance; bestGhost = pb.item; }
                }
                else if (h.distance < bd) { bd = h.distance; best = pb.item; }
            }
            return best ?? bestGhost;
        }

        /// <summary>
        /// Bounds that cover the parts both assembled and at their laid-out (removed) position, and the average
        /// removal direction in world space. Used to aim the camera at a part while it is removed / installed.
        /// </summary>
        public bool TryGetRemovalPath(IEnumerable<IPDItem> leaves, out Bounds path, out Vector3 direction)
        {
            path = new Bounds(); direction = Vector3.zero;
            bool has = false;
            if (Active == null) return false;
            foreach (var l in leaves)
            {
                if (!Active.parts.TryGetValue(l, out var pb) || !pb.IsBound) continue;
                var bb = pb.CurrentBounds();
                Vector3 rem = Vector3.zero, via = Vector3.zero, now = Vector3.zero;
                float rt = removeT.TryGetValue(l, out var t) ? t : 0f;
                float ease = rt * rt * (3f - 2f * rt);
                if (pb.hasRemoval && pb.nodes.Count > 0 && pb.nodes[0] != null)
                {
                    var parent = pb.nodes[0].parent;
                    System.Func<Vector3, Vector3> W = v => parent ? parent.TransformVector(v) : v;
                    rem = W(pb.removalLocal[0]); via = W(pb.removalViaLocal[0]);
                    now = W(AlongPath(pb.removalViaLocal[0], pb.removalLocal[0], ease));
                }
                var rest = new Bounds(bb.center - now, bb.size);
                if (!has) { path = rest; has = true; } else path.Encapsulate(rest);
                path.Encapsulate(new Bounds(rest.center + via, bb.size));
                path.Encapsulate(new Bounds(rest.center + rem, bb.size));
                direction += rem;
            }
            return has;
        }

        public bool TryGetBounds(IEnumerable<IPDItem> leaves, out Bounds b)
        {
            b = new Bounds();
            bool has = false;
            if (Active == null) return false;
            foreach (var l in leaves)
            {
                if (!Active.parts.TryGetValue(l, out var pb) || !pb.IsBound) continue;
                if (!IsVisible(l)) continue;
                var bb = pb.CurrentBounds();
                if (!has) { b = bb; has = true; } else b.Encapsulate(bb);
            }
            return has;
        }

        public Bounds VisibleBounds()
        {
            if (TryGetBounds(db.Leaves(), out var b)) return b;
            return Active != null ? Active.bounds : new Bounds(Vector3.zero, Vector3.one);
        }
    }
}
