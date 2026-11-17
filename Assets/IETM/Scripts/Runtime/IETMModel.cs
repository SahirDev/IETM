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
        public float explode;      // 0..1
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

        public bool IsRemoved(IPDItem leaf) => removeT.TryGetValue(leaf, out var t) && t > 0.97f;

        public bool IsVisible(IPDItem leaf) => !hidden.Contains(leaf) && !IsRemoved(leaf);

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
            // removal animation
            bool moving = false;
            var keys = new List<IPDItem>(removeTarget.Keys);
            foreach (var k in keys)
            {
                float cur = removeT.TryGetValue(k, out var c) ? c : 0f;
                float tgt = removeTarget[k];
                if (Mathf.Abs(cur - tgt) > 0.0001f)
                {
                    bool wasRemoved = cur > 0.97f;
                    cur = Mathf.MoveTowards(cur, tgt, dt * 1.1f);
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

        public void SetExplode(float e)
        {
            explode = Mathf.Clamp01(e);
            ApplyOffsets();
        }

        public void ApplyOffsets()
        {
            if (Active == null) return;
            float r = Active.radius;
            foreach (var pb in Active.parts.Values)
            {
                if (!pb.IsBound) continue;
                float rt = removeT.TryGetValue(pb.item, out var t) ? t : 0f;
                float ease = rt * rt * (3f - 2f * rt);
                Vector3 offset = pb.explodeDir * (explode * r * 0.9f) + pb.explodeDir * (ease * r * 1.4f) + Vector3.up * (ease * r * 0.4f);
                for (int i = 0; i < pb.nodes.Count; i++)
                {
                    var n = pb.nodes[i];
                    if (n == null) continue;
                    var parent = n.parent;
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
