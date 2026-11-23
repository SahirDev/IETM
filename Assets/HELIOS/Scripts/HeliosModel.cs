// HELIOS - the aircraft in the 3D view: your model (or a simple placeholder jet), scaled to a standard size,
// its "hologram" look (glowing wireframe over a dark see-through body) and the points the labels point at.
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    public enum ModelForward { PlusZ = 0, MinusZ = 1, PlusX = 2, MinusX = 3 }

    /// <summary>How the model is drawn: its own materials, or the hologram coloured for a page.</summary>
    public enum ModelLook { Original = 0, HoloHealth = 1, HoloIntegrity = 2, HoloMaintenance = 3 }   // the hologram, coloured for a page

    /// <summary>What a part of the aircraft is (picked from its name, or set in HeliosApp > Part Categories).</summary>
    public enum PartCategory { Airframe = 0, Structure = 1, Engine = 2, Avionics = 3, Hydraulics = 4, Stores = 5 }

    /// <summary>A criteria colour for a part's wireframe lines; None = no lines on that part.</summary>
    public enum WireStatus { None = 0, Healthy = 1, Watch = 2, Critical = 3, Highlight = 4 }

    /// <summary>Sets the category of a part: drag the part (scene object) in, or type part of its name.</summary>
    [System.Serializable]
    public class PartOverride
    {
        [Tooltip("The part (an object in the scene). Its child objects get the same category.")]
        public GameObject part;
        [Tooltip("Or: every part whose name (or a parent's name) contains this text.")]
        public string nameContains = "";
        public PartCategory category = PartCategory.Airframe;
    }

    /// <summary>One line of a page's criteria: parts of this category get this colour.</summary>
    [System.Serializable]
    public class CategoryStatus
    {
        public PartCategory category;
        public WireStatus status;
        public CategoryStatus() { }
        public CategoryStatus(PartCategory c, WireStatus s) { category = c; status = s; }
    }

    /// <summary>The wireframe drawn over the model's own materials (HeliosApp Inspector > Wireframe).</summary>
    [System.Serializable]
    public class WireSettings
    {
        [Tooltip("Spacing of the grid lines, as a share of the aircraft's length (smaller = more lines).")]
        [Range(0.002f, 0.05f)] public float gridSpacing = 0.011f;
        [Tooltip("Line thickness in pixels.")]
        [Range(0.05f, 3f)] public float gridLineWidth = 0.4f;
        [Range(0f, 1f)] public float gridOpacity = 0.9f;
        [Tooltip("A thin glow along each part's silhouette.")]
        [Range(0f, 1f)] public float outlineGlow = 0.35f;
        [Tooltip("Also draw the part's real edges (where its surface bends).")]
        public bool edges = true;
        [Range(1f, 90f)] public float edgeAngle = 30f;
        [Range(0f, 3f)] public float edgeBrightness = 0.9f;
        [Range(0f, 1f)] public float opacity = 1f;
        [Header("Criteria colours")]
        public Color healthy = new Color32(34, 197, 94, 255);
        public Color watch = new Color32(245, 158, 11, 255);
        public Color critical = new Color32(239, 68, 68, 255);
        [Tooltip("All Parts and Clicked Sensor pages.")]
        public Color highlight = new Color32(34, 211, 238, 255);

        public Color ColorOf(WireStatus st)
        {
            return st == WireStatus.Healthy ? healthy : st == WireStatus.Watch ? watch : st == WireStatus.Critical ? critical : highlight;
        }

        public void Repair()
        {
            gridSpacing = Mathf.Clamp(Fix(gridSpacing, 0.011f), 0.002f, 0.05f); gridLineWidth = Mathf.Clamp(Fix(gridLineWidth, 0.4f), 0.05f, 3f);
            gridOpacity = Mathf.Clamp01(Fix(gridOpacity, 0.9f)); outlineGlow = Mathf.Clamp01(Fix(outlineGlow, 0.35f));
            edgeAngle = Mathf.Clamp(Fix(edgeAngle, 30f), 1f, 90f); edgeBrightness = Mathf.Clamp(Fix(edgeBrightness, 0.9f), 0f, 3f); opacity = Mathf.Clamp01(Fix(opacity, 1f));
        }
        static float Fix(float v, float d) { return float.IsNaN(v) || float.IsInfinity(v) ? d : v; }
    }

    /// <summary>Which edges the hologram draws: none, only where the surface bends (clean outlines), or every triangle edge.</summary>
    public enum HoloEdges { None = 0, Creases = 1, AllEdges = 2 }

    /// <summary>The blueprint hologram's look (HeliosApp Inspector > Hologram).</summary>
    [System.Serializable]
    public class HoloSettings
    {
        public Color lineColor = new Color32(34, 211, 238, 255);
        [Tooltip("On: the body is solid (nothing shows through it, the far side is hidden). Off: see-through glow.")]
        public bool solidBody = true;
        [Tooltip("The body colour when Solid Body is on.")]
        public Color solidColor = new Color(0.02f, 0.08f, 0.18f, 1f);
        [Tooltip("The see-through body (Solid Body off); alpha = how strong.")]
        public Color bodyColor = new Color(0.03f, 0.18f, 0.36f, 0.35f);
        [Range(0f, 4f)] public float brightness = 1.2f;
        [Tooltip("Glow along the silhouette.")]
        [Range(0f, 3f)] public float outlineGlow = 1.25f;
        [Range(0.5f, 8f)] public float outlineSharpness = 2.6f;
        [Tooltip("Spacing of the panel grid, as a share of the aircraft's length.")]
        [Range(0.005f, 0.2f)] public float gridSpacing = 0.032f;
        [Range(0.1f, 3f)] public float gridLineWidth = 0.75f;
        [Range(0f, 2f)] public float gridBrightness = 0.6f;
        public HoloEdges edges = HoloEdges.Creases;
        [Tooltip("An edge is drawn where the surface bends more than this (degrees).")]
        [Range(1f, 90f)] public float edgeAngle = 30f;
        [Range(0f, 3f)] public float edgeBrightness = 1.1f;
        [Range(0f, 4f)] public float zoneGlow = 1.6f;
        [Tooltip("Not used any more: both sides of every surface are drawn alike (surface model).")]
        [Range(0f, 1f)] public float insideSurfaces = 0.4f;
        [Tooltip("The built-in zones: engine, wing root, avionics bay, hydraulics (add your own with HELIOS > Hologram Zone).")]
        public bool builtInZones = true;

        public void Repair()
        {
            brightness = Mathf.Clamp(Fix(brightness, 1.2f), 0f, 4f); outlineGlow = Mathf.Clamp(Fix(outlineGlow, 1.25f), 0f, 3f);
            outlineSharpness = Mathf.Clamp(Fix(outlineSharpness, 2.6f), 0.5f, 8f); gridSpacing = Mathf.Clamp(Fix(gridSpacing, 0.032f), 0.005f, 0.2f);
            gridLineWidth = Mathf.Clamp(Fix(gridLineWidth, 0.75f), 0.1f, 3f); gridBrightness = Mathf.Clamp(Fix(gridBrightness, 0.6f), 0f, 2f);
            edgeAngle = Mathf.Clamp(Fix(edgeAngle, 30f), 1f, 90f); edgeBrightness = Mathf.Clamp(Fix(edgeBrightness, 1.1f), 0f, 3f);
            zoneGlow = Mathf.Clamp(Fix(zoneGlow, 1.6f), 0f, 4f); insideSurfaces = Mathf.Clamp01(Fix(insideSurfaces, 0.4f));
            if (!System.Enum.IsDefined(typeof(HoloEdges), edges)) edges = HoloEdges.Creases;
        }
        static float Fix(float v, float d) { return float.IsNaN(v) || float.IsInfinity(v) ? d : v; }
    }

    public class HeliosModel
    {
        public const float Size = 10f;               // the model's longest side after scaling, in world units

        public Transform Root { get; private set; }
        public Bounds Bounds { get; private set; }
        public Vector3 Forward { get; private set; }  // the nose direction
        public Vector3 Side { get; private set; }

        readonly List<Renderer> originals = new List<Renderer>();
        readonly List<Holo> holos = new List<Holo>();
        Material fillMat, lineMat;
        bool holoBuilt, blueprint;
        HoloSettings settings = new HoloSettings();
        Color[] healthZones = { new Color32(34, 197, 94, 255), new Color32(59, 130, 246, 255), new Color32(251, 146, 60, 255), new Color32(34, 197, 94, 255) };
        static readonly Matrix4x4[] zoneW2L = new Matrix4x4[12];
        static readonly Vector4[] zoneColor = new Vector4[12];

        /// <summary>The blueprint hologram's look, and the Health page's zone colours (structure, engine, avionics, hydraulics).</summary>
        public void SetHologram(HoloSettings s, Color[] healthZoneColors)
        {
            if (s != null) settings = s;
            if (healthZoneColors != null && healthZoneColors.Length == 4) healthZones = healthZoneColors;
        }

        // =====================================================================
        // parts and the wireframe overlay (the model keeps its own materials)
        // =====================================================================

        /// <summary>The model's parts (each mesh renderer).</summary>
        public IList<Renderer> Parts { get { return originals; } }

        static readonly string[][] Keywords =
        {
            new[] { "fuselage", "canopy", "cockpit", "nose", "body", "hull" },                                                       // Airframe
            new[] { "wing", "tail", "fin", "stabil", "rudder", "flap", "aileron", "elevator", "spar", "rib", "slat", "strake" },     // Structure
            new[] { "engine", "intake", "inlet", "nozzle", "exhaust", "turbine", "compressor", "afterburner", "fan", "jet" },         // Engine
            new[] { "avionic", "radar", "antenna", "electronic", "computer", "pitot", "sensor", "radome", "probe" },                // Avionics
            new[] { "hydraul", "gear", "wheel", "strut", "actuator", "brake", "landing", "tyre", "tire", "pump" },                   // Hydraulics
            new[] { "missile", "pylon", "tank", "bomb", "store", "pod", "rail", "launcher", "weapon" },                             // Stores
        };

        /// <summary>A part's category: your setting in Part Categories, else its name (or a parent's), else where it sits on the aircraft.</summary>
        public PartCategory CategoryOf(Renderer r, IList<PartOverride> overrides)
        {
            if (overrides != null)
                foreach (var o in overrides)
                {
                    if (o == null) continue;
                    if (o.part != null)
                    {
                        if (r.transform == o.part.transform || r.transform.IsChildOf(o.part.transform)) return o.category;
                        if (!o.part.scene.IsValid())     // a part of the prefab / model asset: the scene holds a copy, so match by name
                            for (var t = r.transform; t != null && t != Root; t = t.parent)
                                if (t.name == o.part.name) return o.category;
                    }
                    if (!string.IsNullOrEmpty(o.nameContains))
                        for (var t = r.transform; t != null && t != Root; t = t.parent)
                            if (t.name != null && t.name.IndexOf(o.nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0) return o.category;
                }
            for (var t = r.transform; t != null && t != Root; t = t.parent)
            {
                string n = (t.name ?? "").ToLowerInvariant();
                // the most specific first: stores, hydraulics, engine, avionics, structure, airframe
                foreach (int c in new[] { 5, 4, 2, 3, 1, 0 })
                    foreach (var k in Keywords[c]) if (n.Contains(k)) return (PartCategory)c;
            }
            int z = ZoneOf(r.bounds.center);
            return z == 2 ? PartCategory.Engine : z == 1 ? PartCategory.Structure : PartCategory.Airframe;
        }

        /// <summary>The part(s) at a point on the model: those whose bounds hold it, else the nearest one.</summary>
        public List<Renderer> PartsAt(Vector3 point)
        {
            var list = new List<Renderer>();
            Renderer best = null; float bestD = float.MaxValue, bestVol = float.MaxValue;
            foreach (var r in originals)
            {
                if (r == null) continue;
                var b = r.bounds;
                float d = (b.ClosestPoint(point) - point).sqrMagnitude, vol = b.size.x * b.size.y * b.size.z;
                if (d < bestD - 1e-6f || (Mathf.Abs(d - bestD) <= 1e-6f && vol < bestVol)) { best = r; bestD = d; bestVol = vol; }
            }
            if (best != null) list.Add(best);       // the smallest part at (or nearest to) the point
            return list;
        }

        /// <summary>The part under a ray (a click in the 3D view), or null. Uses the mesh's triangles when the
        /// mesh is readable, else the part's bounds; needs no colliders on the model.</summary>
        public Renderer PickPart(Ray ray)
        {
            Renderer best = null; float bestD = float.MaxValue;
            foreach (var r in originals)
            {
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                float tb;
                if (!r.bounds.IntersectRay(ray, out tb) || tb >= bestD) continue;
                float d;
                int hit = RayMesh(r, ray, out d);
                if (hit == 0) continue;                 // inside the box but misses the surface
                if (hit < 0) d = tb;                    // no readable mesh: the box will do
                if (d < bestD) { bestD = d; best = r; }
            }
            return best;
        }

        // 1 = hits the mesh (d = world distance), 0 = misses it, -1 = the mesh cannot be read
        static int RayMesh(Renderer r, Ray ray, out float d)
        {
            d = 0f;
            if (r is SkinnedMeshRenderer) return -1;
            var mf = r.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh == null || !mesh.isReadable) return -1;
            var v = mesh.vertices; var tri = mesh.triangles;
            if (v == null || tri == null || tri.Length < 3) return -1;
            var w2l = r.transform.worldToLocalMatrix;
            Vector3 o = w2l.MultiplyPoint3x4(ray.origin), dir = w2l.MultiplyVector(ray.direction);
            float best = float.MaxValue;
            for (int i = 0; i + 2 < tri.Length; i += 3)
            {
                Vector3 a = v[tri[i]], e1 = v[tri[i + 1]] - a, e2 = v[tri[i + 2]] - a;
                var p = Vector3.Cross(dir, e2);
                float det = Vector3.Dot(e1, p);
                if (Mathf.Abs(det) < 1e-12f) continue;
                float inv = 1f / det; var s = o - a;
                float u = Vector3.Dot(s, p) * inv; if (u < 0f || u > 1f) continue;
                var q = Vector3.Cross(s, e1);
                float w = Vector3.Dot(dir, q) * inv; if (w < 0f || u + w > 1f) continue;
                float t = Vector3.Dot(e2, q) * inv;
                if (t > 0f && t < best) best = t;
            }
            if (best == float.MaxValue) return 0;
            var hitWorld = r.transform.localToWorldMatrix.MultiplyPoint3x4(o + dir * best);
            d = (hitWorld - ray.origin).magnitude;
            return 1;
        }

        class Overlay { public GameObject go; public Renderer surf, edges; }
        readonly Dictionary<Renderer, Overlay> overlays = new Dictionary<Renderer, Overlay>();
        Material overlayMat, overlayEdgeMat;
        bool overlayTried, overlayOk;
        MaterialPropertyBlock mpb;
        WireSettings wire = new WireSettings();

        /// <summary>Draws wireframe lines over the parts that get a colour (null = no lines on that part), or none (colourOf null).
        /// The parts keep their own materials.</summary>
        public void SetWireframe(System.Func<Renderer, Color?> colourOf, WireSettings ws)
        {
            if (ws != null) wire = ws;
            if (colourOf == null || HologramOn) { foreach (var o in overlays.Values) if (o.go != null && o.go.activeSelf) o.go.SetActive(false); return; }
            if (!overlayTried)
            {
                overlayTried = true;
                var sh = Resources.Load<Shader>("HELIOS Wireframe Overlay");
                var ln = Resources.Load<Shader>("HELIOS Hologram Lines");
                overlayOk = sh != null && sh.isSupported;
                if (overlayOk) overlayMat = new Material(sh) { name = "HELIOS Wireframe Overlay" };
                if (overlayOk && ln != null && ln.isSupported) overlayEdgeMat = new Material(ln) { name = "HELIOS Wireframe Edges" };
                if (!overlayOk) Debug.LogWarning("[HELIOS] The wireframe shader is not available here: the wireframe is not shown.");
            }
            if (!overlayOk) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var r in originals)
            {
                if (r == null) continue;
                var c = colourOf(r);
                Overlay o;
                overlays.TryGetValue(r, out o);
                if (c == null) { if (o != null && o.go != null && o.go.activeSelf) o.go.SetActive(false); continue; }
                if (o == null) { o = MakeOverlay(r); overlays[r] = o; }
                if (o == null || o.go == null) continue;
                if (!o.go.activeSelf) o.go.SetActive(true);
                mpb.Clear(); mpb.SetColor("_LineColor", c.Value);
                if (o.surf != null) o.surf.SetPropertyBlock(mpb);
                if (o.edges != null) o.edges.SetPropertyBlock(mpb);
            }
            UpdateWireframe();
        }

        Overlay MakeOverlay(Renderer r)
        {
            Mesh mesh = null;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            var smr = r as SkinnedMeshRenderer;
            if (smr != null && smr.sharedMesh != null)
            {
                mesh = new Mesh();
                if (smr.sharedMesh.isReadable) smr.BakeMesh(mesh); else mesh = smr.sharedMesh;
            }
            if (mesh == null) return null;
            var o = new Overlay { go = new GameObject("Wireframe") };
            o.go.transform.SetParent(r.transform, false);
            o.go.layer = r.gameObject.layer;
            o.go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = o.go.AddComponent<MeshRenderer>();
            var mats = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = overlayMat;
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            o.surf = mr;
            if (wire.edges && overlayEdgeMat != null && mesh.isReadable)
            {
                var em = CreaseMesh(mesh, wire.edgeAngle);
                if (em != null)
                {
                    var eg = new GameObject("Edges");
                    eg.transform.SetParent(o.go.transform, false);
                    eg.layer = o.go.layer;
                    eg.AddComponent<MeshFilter>().sharedMesh = em;
                    var er = eg.AddComponent<MeshRenderer>(); er.sharedMaterial = overlayEdgeMat;
                    er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; er.receiveShadows = false;
                    o.edges = er;
                }
            }
            return o;
        }

        /// <summary>Sends the wireframe settings to the shaders (every frame, so Inspector changes show while playing).</summary>
        public void UpdateWireframe()
        {
            if (overlayMat == null) return;
            float len = Mathf.Max(Bounds.size.x, Mathf.Max(Bounds.size.y, Bounds.size.z));
            overlayMat.SetFloat("_GridSpacing", Mathf.Max(0.0005f, wire.gridSpacing * len));
            overlayMat.SetFloat("_GridWidth", wire.gridLineWidth);
            overlayMat.SetFloat("_GridStrength", wire.gridOpacity);
            overlayMat.SetFloat("_RimStrength", wire.outlineGlow);
            overlayMat.SetFloat("_Opacity", wire.opacity);
            if (overlayEdgeMat != null) { overlayEdgeMat.SetFloat("_EdgeStrength", wire.edgeBrightness * wire.opacity); overlayEdgeMat.SetFloat("_Intensity", 1f); }
            Shader.SetGlobalVector("_HoloCenter", Bounds.center);
            if (!HologramOn) Shader.SetGlobalFloat("_HoloZoneCount", 0f);     // the edge lines take each part's own colour
        }

        /// <summary>True when the model is drawn as the hologram right now.</summary>
        public bool HologramOn { get { return look != ModelLook.Original && (int)look >= 0 && holos.Count > 0; } }
        ModelLook look = (ModelLook)(-1);

        class Holo { public GameObject go; public Mesh lines; public byte[] zone; public Color32[] colors; }

        // zone colours per look: 0 airframe, 1 structure (wing root / centre fuselage), 2 engine
        Color fill = new Color(0.05f, 0.22f, 0.42f, 0.42f), health = new Color32(34, 211, 238, 255);
        Color[] integrity = { new Color32(34, 197, 94, 255), new Color32(245, 158, 11, 255), new Color32(239, 68, 68, 255) };
        Color[] maintenance = { new Color32(34, 211, 238, 255), new Color32(34, 197, 94, 255), new Color32(251, 146, 60, 255) };

        /// <summary>The hologram colours (from the HeliosApp Inspector).</summary>
        public void SetColors(Color fillColor, Color healthColor, Color[] integrityColors, Color[] maintenanceColors)
        {
            fill = fillColor; health = healthColor;
            if (integrityColors != null && integrityColors.Length == 3) integrity = integrityColors;
            if (maintenanceColors != null && maintenanceColors.Length == 3) maintenance = maintenanceColors;
            if (fillMat != null) fillMat.color = fill;
            var l = look; look = (ModelLook)(-1); if ((int)l >= 0) SetLook(l);
        }

        Color[] Palette(ModelLook l)
        {
            switch (l)
            {
                case ModelLook.HoloIntegrity: return integrity;
                case ModelLook.HoloMaintenance: return maintenance;
                default: return new[] { health, health, health };
            }
        }

        // =====================================================================
        // building
        // =====================================================================

        /// <summary>Name of the object that holds the aircraft once it is placed in the scene (Tools > HELIOS > Place Aircraft In Scene).</summary>
        public const string PlacedRootName = "HELIOS Model";

        /// <summary>True when the aircraft is already in the scene under "HELIOS Model", centred and scaled in the Editor:
        /// nothing is instantiated, moved or scaled at Play.</summary>
        public static bool IsPlaced(GameObject source)
        {
            return source != null && source.scene.IsValid() && source.transform.parent != null && source.transform.parent.name == PlacedRootName;
        }

        public void Build(GameObject source, ModelForward forward, Transform parent)
        {
            GameObject inst;
            bool placed = IsPlaced(source);
            if (placed)
            {
                Root = source.transform.parent;                                              // placed in the Editor: used as it is
                inst = source; if (!inst.activeSelf) inst.SetActive(true);
                if (!Root.gameObject.activeSelf) Root.gameObject.SetActive(true);
            }
            else
            {
                Root = new GameObject(PlacedRootName).transform;
                Root.SetParent(parent, false);
                if (source == null) inst = Placeholder();
                else if (source.scene.IsValid()) { inst = source; inst.SetActive(true); }     // an object already in the scene
                else inst = Object.Instantiate(source);                                      // a prefab or model asset (loaded at Play)
                inst.transform.SetParent(Root, true);
            }

            Forward = forward == ModelForward.MinusZ ? Vector3.back : forward == ModelForward.PlusX ? Vector3.right : forward == ModelForward.MinusX ? Vector3.left : Vector3.forward;
            Side = Vector3.Cross(Vector3.up, Forward);

            originals.Clear();
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer || r is SkinnedMeshRenderer) originals.Add(r);

            if (placed) { Bounds = WorldBounds(); return; }

            // centre the model on the origin and scale its longest side to Size
            var b = WorldBounds();
            if (b.size.sqrMagnitude < 1e-8f) b = new Bounds(inst.transform.position, Vector3.one);
            inst.transform.position -= b.center;
            float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            Root.localScale = Vector3.one * (longest > 1e-5f ? Size / longest : 1f);
            Bounds = WorldBounds();
        }

        Bounds WorldBounds()
        {
            bool any = false; var b = new Bounds();
            foreach (var r in originals)
            {
                if (r == null) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b : new Bounds(Vector3.zero, Vector3.one * Size);
        }

        /// <summary>A simple jet (nose at +Z) made of primitives, shown until a model is set.</summary>
        static GameObject Placeholder()
        {
            var jet = new GameObject("Placeholder Jet");
            var silver = new Color(0.72f, 0.75f, 0.8f);
            Part(jet, "Fuselage", PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(1.2f, 5f, 1.1f), silver);
            Part(jet, "Nose", PrimitiveType.Capsule, new Vector3(0f, -0.05f, 4.4f), new Vector3(90f, 0f, 0f), new Vector3(0.7f, 1.6f, 0.65f), silver);
            Part(jet, "Cockpit Canopy", PrimitiveType.Sphere, new Vector3(0f, 0.55f, 2.6f), Vector3.zero, new Vector3(0.75f, 0.6f, 2.2f), new Color(0.3f, 0.38f, 0.48f));
            Part(jet, "Engine Intake", PrimitiveType.Cube, new Vector3(0f, -0.65f, 1.2f), Vector3.zero, new Vector3(0.9f, 0.6f, 2.4f), silver * 0.85f);
            Part(jet, "Wing Left", PrimitiveType.Cube, new Vector3(-2.6f, -0.05f, -0.9f), new Vector3(0f, -28f, 0f), new Vector3(4.6f, 0.12f, 2.4f), silver);
            Part(jet, "Wing Right", PrimitiveType.Cube, new Vector3(2.6f, -0.05f, -0.9f), new Vector3(0f, 28f, 0f), new Vector3(4.6f, 0.12f, 2.4f), silver);
            Part(jet, "Stabiliser Left", PrimitiveType.Cube, new Vector3(-1.4f, -0.05f, -4.4f), new Vector3(0f, -25f, 0f), new Vector3(2.2f, 0.08f, 1.2f), silver);
            Part(jet, "Stabiliser Right", PrimitiveType.Cube, new Vector3(1.4f, -0.05f, -4.4f), new Vector3(0f, 25f, 0f), new Vector3(2.2f, 0.08f, 1.2f), silver);
            Part(jet, "Tail Fin", PrimitiveType.Cube, new Vector3(0f, 1.3f, -3.9f), new Vector3(-30f, 0f, 0f), new Vector3(0.1f, 2.2f, 1.5f), silver);
            Part(jet, "Engine Nozzle", PrimitiveType.Cylinder, new Vector3(0f, 0f, -5.3f), new Vector3(90f, 0f, 0f), new Vector3(0.75f, 0.45f, 0.75f), new Color(0.35f, 0.36f, 0.4f));
            return jet;
        }

        static void Part(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Color color)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null) { var m = new Material(r.sharedMaterial); m.color = color; r.sharedMaterial = m; }
        }

        // =====================================================================
        // positions on the model (0..1 along the nose-to-tail axis, height and side)
        // =====================================================================

        /// <summary>A point on the model: f 0 at the nose to 1 at the tail, h 0 bottom to 1 top, s -0.5 left to 0.5 right.</summary>
        public Vector3 Point(float f, float h, float s)
        {
            var b = Bounds; var e = b.extents;
            float len = Mathf.Abs(Vector3.Dot(e, Abs(Forward))) * 2f, wid = Mathf.Abs(Vector3.Dot(e, Abs(Side))) * 2f;
            return b.center + Forward * (0.5f - f) * len + Vector3.up * (h - 0.5f) * e.y * 2f + Side * s * wid;
        }

        static Vector3 Abs(Vector3 v) { return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)); }

        /// <summary>The three label points: 0 cockpit (humidity), 1 wing root (strain), 2 engine intake (vibration).</summary>
        public Vector3 LabelPoint(int i)
        {
            switch (i)
            {
                case 0: return Point(0.24f, 0.78f, 0f);
                case 1: return Point(0.52f, 0.62f, 0.08f);
                case 3: return Point(0.95f, 0.45f, 0f);      // engine RPM: engine, at the nozzle
                case 4: return Point(0.78f, 0.35f, 0.02f);   // oil pressure: engine bay
                case 5: return Point(0.62f, 0.42f, 0.04f);   // fuel flow: fuel lines, centre fuselage
                case 6: return Point(0.55f, 0.18f, 0.03f);   // hydraulic pressure: hydraulic bay, under the fuselage
                default: return Point(0.42f, 0.25f, 0.02f);  // 2 vibration: engine intake
            }
        }

        int ZoneOf(Vector3 world)
        {
            var b = Bounds; var e = b.extents; var p = world - b.center;
            float len = Mathf.Max(1e-4f, Mathf.Abs(Vector3.Dot(e, Abs(Forward))) * 2f), wid = Mathf.Max(1e-4f, Mathf.Abs(Vector3.Dot(e, Abs(Side))) * 2f);
            float f = 0.5f - Vector3.Dot(p, Forward) / len, h = Vector3.Dot(p, Vector3.up) / Mathf.Max(1e-4f, e.y * 2f) + 0.5f, s = Mathf.Abs(Vector3.Dot(p, Side) / wid);
            if ((f > 0.3f && f < 0.62f && h < 0.45f && s < 0.16f) || (f > 0.9f && s < 0.12f && h < 0.62f)) return 2;     // intake and nozzle
            if (f > 0.42f && f < 0.86f && s < 0.3f && h > 0.35f) return 1;                                                // wing root, centre fuselage
            return 0;
        }

        // =====================================================================
        // the look
        // =====================================================================

        public void SetLook(ModelLook l)
        {
            if (l == look) return;
            look = l;
            bool holo = l != ModelLook.Original;
            if (holo && !holoBuilt) BuildHolo();
            if (holos.Count == 0) holo = false;           // no hologram could be made (no shader or no meshes): keep the model as it is
            foreach (var r in originals) if (r != null) r.enabled = !holo;
            var pal = Palette(l);
            foreach (var h in holos) if (h.go != null) h.go.SetActive(holo);
            if (holo && blueprint) { UpdateHolo(); return; }
            foreach (var h in holos)
            {
                if (h.go == null) continue;
                if (!holo || h.lines == null) continue;
                for (int i = 0; i < h.colors.Length; i++)
                {
                    var c = pal[h.zone[i]];
                    h.colors[i] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), 235);
                }
                h.lines.colors32 = h.colors;
            }
        }

        static Shader UnlitShader()
        {
            var s = Shader.Find("Sprites/Default");          // unlit, see-through, vertex colours; draws in every render pipeline
            if (s == null) s = Shader.Find("Unlit/Color");
            return s;
        }

        // =====================================================================
        // the blueprint hologram (HELIOS/Hologram Surface + Lines shaders)
        // =====================================================================

        struct BuiltInZone { public float f, h, s, len, height, width; public int role; public BuiltInZone(float f, float h, float s, float len, float height, float width, int role) { this.f = f; this.h = h; this.s = s; this.len = len; this.height = height; this.width = width; this.role = role; } }
        // role: 1 structure (wing root), 2 engine, 3 avionics, 4 hydraulics
        static readonly BuiltInZone[] builtIn =
        {
            new BuiltInZone(0.46f, 0.20f, 0f, 0.26f, 0.26f, 0.18f, 2),   // engine intake
            new BuiltInZone(0.95f, 0.45f, 0f, 0.12f, 0.34f, 0.16f, 2),   // engine nozzle
            new BuiltInZone(0.62f, 0.50f, 0f, 0.30f, 0.25f, 0.55f, 1),   // wing root, centre fuselage
            new BuiltInZone(0.38f, 0.70f, 0f, 0.12f, 0.22f, 0.14f, 3),   // avionics bay behind the cockpit
            new BuiltInZone(0.78f, 0.72f, 0f, 0.22f, 0.25f, 0.10f, 4),   // hydraulic lines along the spine
        };

        Color ZoneRoleColor(int role, out bool show)
        {
            show = true;
            switch (look)
            {
                case ModelLook.HoloIntegrity: if (role >= 3) { show = false; return integrity[0]; } return integrity[role];
                case ModelLook.HoloMaintenance: if (role >= 3) { show = false; return maintenance[0]; } return maintenance[role];
                default: return healthZones[role - 1];
            }
        }

        /// <summary>Sends the hologram's colours, sizes and zones to the shaders (every frame while it shows, so zones can move).</summary>
        public void UpdateHolo()
        {
            if (!blueprint || !HologramOn) return;
            var st = settings;
            float len = Mathf.Max(Bounds.size.x, Mathf.Max(Bounds.size.y, Bounds.size.z));
            Color baseLine = look == ModelLook.HoloIntegrity ? integrity[0] : look == ModelLook.HoloMaintenance ? maintenance[0] : st.lineColor;
            if (fillMat != null)
            {
                fillMat.SetColor("_LineColor", baseLine); fillMat.SetColor("_FillColor", st.solidBody ? st.solidColor : st.bodyColor);
                fillMat.SetFloat("_Solid", st.solidBody ? 1f : 0f);
                fillMat.SetFloat("_SrcBlend", 1f);                                   // One
                fillMat.SetFloat("_DstBlend", st.solidBody ? 0f : 1f);               // Zero (solid) or One (additive glow)
                fillMat.SetFloat("_ZWrite", st.solidBody ? 1f : 0f);
                fillMat.SetFloat("_Cull", 0f);                                       // Off: the model is made of surfaces (no closed solids), so both sides are drawn
                fillMat.renderQueue = st.solidBody ? 2000 : 3000;                    // Geometry or Transparent
                fillMat.SetFloat("_RimPower", st.outlineSharpness); fillMat.SetFloat("_RimStrength", st.outlineGlow);
                fillMat.SetFloat("_GridSpacing", Mathf.Max(0.001f, st.gridSpacing * len)); fillMat.SetFloat("_GridWidth", st.gridLineWidth);
                fillMat.SetFloat("_GridStrength", st.gridBrightness); fillMat.SetFloat("_ZoneGlow", st.zoneGlow);
                fillMat.SetFloat("_Intensity", st.brightness); fillMat.SetFloat("_BackFace", st.insideSurfaces);
            }
            if (lineMat != null) { lineMat.SetColor("_LineColor", baseLine); lineMat.SetFloat("_EdgeStrength", st.edgeBrightness); lineMat.SetFloat("_Intensity", st.brightness); }
            int n = 0;
            var rot = Quaternion.LookRotation(Forward, Vector3.up);
            float wid = Mathf.Abs(Vector3.Dot(Bounds.size, Abs(Side))), lng = Mathf.Abs(Vector3.Dot(Bounds.size, Abs(Forward))), hgt = Bounds.size.y;
            if (st.builtInZones)
                foreach (var z in builtIn)
                {
                    bool show; var c = ZoneRoleColor(z.role, out show);
                    if (!show || n >= 12) continue;
                    var size = new Vector3(Mathf.Max(1e-4f, z.width * wid), Mathf.Max(1e-4f, z.height * hgt), Mathf.Max(1e-4f, z.len * lng));
                    zoneW2L[n] = Matrix4x4.TRS(Point(z.f, z.h, z.s), rot, size).inverse;
                    zoneColor[n] = new Vector4(c.r, c.g, c.b, 1f);
                    n++;
                }
            foreach (var z in HeliosHoloZone.All)
            {
                if (z == null || !z.isActiveAndEnabled || !z.ShowsOn(look) || n >= 12) continue;
                zoneW2L[n] = z.transform.worldToLocalMatrix;
                zoneColor[n] = new Vector4(z.color.r, z.color.g, z.color.b, z.glow);
                n++;
            }
            for (int i = n; i < 12; i++) { zoneW2L[i] = Matrix4x4.identity; zoneColor[i] = Vector4.zero; }
            Shader.SetGlobalVector("_HoloCenter", Bounds.center);
            Shader.SetGlobalFloat("_HoloZoneCount", n);
            Shader.SetGlobalMatrixArray("_HoloZoneW2L", zoneW2L);
            Shader.SetGlobalVectorArray("_HoloZoneColor", zoneColor);
        }

        void BuildHolo()
        {
            holoBuilt = true;
            var surf = Resources.Load<Shader>("HELIOS Hologram Surface");
            var lines = Resources.Load<Shader>("HELIOS Hologram Lines");
            blueprint = surf != null && lines != null && surf.isSupported && lines.isSupported;
            if (blueprint)
            {
                fillMat = new Material(surf) { name = "HELIOS Hologram Surface" };
                lineMat = new Material(lines) { name = "HELIOS Hologram Lines" };
                foreach (var r in originals) AddHolo(r, fillMat, lineMat, true);
                return;
            }
            var sh = UnlitShader();                         // older look, when the hologram shaders cannot run here
            if (sh == null) return;
            fillMat = new Material(sh) { name = "HELIOS Hologram Fill", color = fill };
            lineMat = new Material(sh) { name = "HELIOS Hologram Lines", color = Color.white };
            foreach (var r in originals)
            {
                if (r == null) continue;
                Mesh mesh = null;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
                var smr = r as SkinnedMeshRenderer;
                if (smr != null && smr.sharedMesh != null)
                {
                    mesh = new Mesh();
                    if (smr.sharedMesh.isReadable) smr.BakeMesh(mesh); else mesh = smr.sharedMesh;
                }
                if (mesh == null) continue;
                var go = new GameObject("Hologram");
                go.transform.SetParent(r.transform, false);
                go.layer = r.gameObject.layer;
                var fmf = go.AddComponent<MeshFilter>(); fmf.sharedMesh = mesh;
                var fmr = go.AddComponent<MeshRenderer>();
                var mats = new Material[Mathf.Max(1, mesh.subMeshCount)];
                for (int i = 0; i < mats.Length; i++) mats[i] = fillMat;
                fmr.sharedMaterials = mats;
                fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; fmr.receiveShadows = false;
                var h = new Holo { go = go };
                if (mesh.isReadable) h.lines = LineMesh(mesh, r.transform, out h.zone);
                if (h.lines != null)
                {
                    var lg = new GameObject("Lines");
                    lg.transform.SetParent(go.transform, false);
                    lg.layer = go.layer;
                    lg.AddComponent<MeshFilter>().sharedMesh = h.lines;
                    var lr = lg.AddComponent<MeshRenderer>(); lr.sharedMaterial = lineMat;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
                    h.colors = new Color32[h.zone.Length];
                }
                go.SetActive(false);
                holos.Add(h);
            }
        }

        /// <summary>One renderer's hologram: the body (surface shader) and its edges (lines shader).</summary>
        void AddHolo(Renderer r, Material surfMat, Material edgeMat, bool bp)
        {
            if (r == null) return;
            Mesh mesh = null;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            var smr = r as SkinnedMeshRenderer;
            if (smr != null && smr.sharedMesh != null)
            {
                mesh = new Mesh();
                if (smr.sharedMesh.isReadable) smr.BakeMesh(mesh); else mesh = smr.sharedMesh;
            }
            if (mesh == null) return;
            var go = new GameObject("Hologram");
            go.transform.SetParent(r.transform, false);
            go.layer = r.gameObject.layer;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var fmr = go.AddComponent<MeshRenderer>();
            var mats = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = surfMat;
            fmr.sharedMaterials = mats;
            fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; fmr.receiveShadows = false;
            var h = new Holo { go = go };
            if (mesh.isReadable && settings.edges != HoloEdges.None)
            {
                byte[] zone;
                h.lines = settings.edges == HoloEdges.Creases ? CreaseMesh(mesh, settings.edgeAngle) : LineMesh(mesh, r.transform, out zone);
            }
            if (h.lines != null)
            {
                var lg = new GameObject("Edges");
                lg.transform.SetParent(go.transform, false);
                lg.layer = go.layer;
                lg.AddComponent<MeshFilter>().sharedMesh = h.lines;
                var lr = lg.AddComponent<MeshRenderer>(); lr.sharedMaterial = edgeMat;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
            }
            go.SetActive(false);
            holos.Add(h);
        }

        /// <summary>Only the edges where the surface bends (or where it ends): the model's real outlines, not every triangle.
        /// Vertices at the same place are joined first, so split normals and UV seams don't add lines.</summary>
        public static Mesh CreaseMesh(Mesh mesh, float angle)
        {
            var verts = mesh.vertices; var tris = mesh.triangles;
            if (verts.Length == 0 || tris.Length < 3) return null;
            float q = Mathf.Max(mesh.bounds.size.magnitude * 1e-5f, 1e-7f);
            var weld = new int[verts.Length];
            var map = new Dictionary<long, int>();
            for (int i = 0; i < verts.Length; i++)
            {
                long x = (long)Mathf.Round(verts[i].x / q) & 0x1FFFFF, y = (long)Mathf.Round(verts[i].y / q) & 0x1FFFFF, z = (long)Mathf.Round(verts[i].z / q) & 0x1FFFFF;
                long key = (x << 42) | (y << 21) | z;
                int id;
                if (!map.TryGetValue(key, out id)) { id = i; map[key] = i; }
                weld[i] = id;
            }
            int triCount = tris.Length / 3;
            var normals = new Vector3[triCount];
            var edges = new Dictionary<long, int[]>();            // a, b (vertex indices), first face, second face, faces
            for (int t = 0; t < triCount; t++)
            {
                int i0 = tris[t * 3], i1 = tris[t * 3 + 1], i2 = tris[t * 3 + 2];
                var nrm = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
                if (nrm.sqrMagnitude < 1e-20f) continue;          // a flat-collapsed triangle: no edges
                normals[t] = nrm.normalized;
                for (int e = 0; e < 3; e++)
                {
                    int a = tris[t * 3 + e], b = tris[t * 3 + (e + 1) % 3];
                    int wa = weld[a], wb = weld[b];
                    if (wa == wb) continue;
                    long key = wa < wb ? ((long)wa << 32) | (uint)wb : ((long)wb << 32) | (uint)wa;
                    int[] info;
                    if (!edges.TryGetValue(key, out info)) { info = new[] { a, b, t, -1, 0 }; edges[key] = info; }
                    else if (info[3] < 0) info[3] = t;
                    info[4]++;
                }
            }
            float cosLimit = Mathf.Cos(angle * Mathf.Deg2Rad);
            var idx = new List<int>();
            foreach (var info in edges.Values)
            {
                bool keep = info[4] != 2 || Vector3.Dot(normals[info[2]], normals[info[3]]) < cosLimit;
                if (!keep) continue;
                idx.Add(info[0]); idx.Add(info[1]);
                if (idx.Count >= 400000) break;
            }
            if (idx.Count == 0) return null;
            var m = new Mesh { name = mesh.name + " edges" };
            if (verts.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = verts;
            m.SetIndices(idx.ToArray(), MeshTopology.Lines, 0);
            m.bounds = mesh.bounds;
            return m;
        }

        /// <summary>The mesh's edges as lines (shared edges once), and the zone of every vertex.</summary>
        Mesh LineMesh(Mesh mesh, Transform t, out byte[] zone)
        {
            zone = null;
            var verts = mesh.vertices; var tris = mesh.triangles;
            if (verts.Length == 0 || tris.Length < 3) return null;
            const int MaxEdges = 150000;
            int triCount = tris.Length / 3, step = Mathf.Max(1, triCount * 3 / 2 / MaxEdges);
            var seen = new HashSet<long>(); var idx = new List<int>();
            for (int k = 0; k < triCount; k += step)
                for (int e = 0; e < 3; e++)
                {
                    int a = tris[k * 3 + e], c = tris[k * 3 + (e + 1) % 3];
                    long key = a < c ? ((long)a << 32) | (uint)c : ((long)c << 32) | (uint)a;
                    if (seen.Add(key)) { idx.Add(a); idx.Add(c); }
                }
            var m = new Mesh { name = mesh.name + " lines" };
            if (verts.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = verts;
            m.SetIndices(idx.ToArray(), MeshTopology.Lines, 0);
            m.bounds = mesh.bounds;
            zone = new byte[verts.Length];
            var l2w = t.localToWorldMatrix;
            for (int i = 0; i < verts.Length; i++) zone[i] = (byte)ZoneOf(l2w.MultiplyPoint3x4(verts[i]));
            return m;
        }
    }
}
