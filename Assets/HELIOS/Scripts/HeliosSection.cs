// HELIOS - Cross Section tool (last button of the 3D View's View Tools).
// On: the aircraft is cut by a plane across one of its axes (X = wing to wing, Y = top to bottom, Z = nose to tail).
// The slider moves the plane through the aircraft and the cut follows it; Flip keeps the other half. The faces the cut
// The model is drawn from both sides (surface model), the outline of the cut is drawn in the section colour (shader
// HELIOS/Section), and a faint plane shows where the cut is.
// Off (or another page): the model gets its own materials back. The controls are objects in the HELIOS UI prefab.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Cross Section Tool")]
    public class HeliosSection : MonoBehaviour
    {
        [Tooltip("Shader of the cut model (HELIOS/Section).")]
        public Shader sectionShader;
        [Tooltip("Colour of the outline where the plane cuts the aircraft.")]
        public Color sectionColor = new Color(0.13f, 0.83f, 0.93f, 1f);
        [Tooltip("Colour of the plane that shows where the cut is (alpha = how visible).")]
        public Color planeColor = new Color(0.13f, 0.83f, 0.93f, 0.06f);
        [Tooltip("Width of the cut outline, as a share of the aircraft's length.")]
        [Range(0.0005f, 0.02f)] public float outlineWidth = 0.004f;

        [Header("Controls (in the prefab)")]
        public GameObject controls;
        public Slider slider;
        [Tooltip("Highlight of the X, Y and Z buttons.")]
        public GameObject[] axisSelected = new GameObject[3];
        [Tooltip("Highlight of the Cross Section button while the tool is on.")]
        public GameObject toolSelected;

        public bool IsOn { get; private set; }
        int axis = 2;                    // Z: nose to tail
        float position = 0.5f;
        float side = 1f;

        readonly List<Renderer> parts = new List<Renderer>();
        readonly List<Material[]> saved = new List<Material[]>();
        readonly Dictionary<Material, Material> cut = new Dictionary<Material, Material>();
        Transform plane;
        Material planeMat;
        static readonly int PlaneId = Shader.PropertyToID("_HeliosSectionPlane");
        static readonly int EdgeId = Shader.PropertyToID("_HeliosSectionEdge");

        void Awake()
        {
            if (slider != null) { slider.SetValueWithoutNotify(position); slider.onValueChanged.AddListener(v => position = v); }
            if (controls != null) controls.SetActive(false);
            if (toolSelected != null) toolSelected.SetActive(false);
            ShowAxis();
        }

        void OnDisable() { SetOn(false); }       // page closed

        // ------------------------------------------------------------------ buttons (OnClick lists)

        public void Toggle() { SetOn(!IsOn); }
        public void SetAxis(int a) { axis = Mathf.Clamp(a, 0, 2); ShowAxis(); }
        public void Flip() { side = -side; }

        public void SetOn(bool on)
        {
            if (on == IsOn) return;
            var app = HeliosApp.Instance;
            if (on && (app == null || app.Model == null || app.Model.Root == null)) return;
            IsOn = on;
            if (controls != null) controls.SetActive(on);
            if (toolSelected != null) toolSelected.SetActive(on);
            if (on)
            {
                app.ShowHologram(false);         // the cut is made on the model's own materials
                app.ShowWireframe(false);
                SwapIn(app.Model);
                if (plane == null) MakePlane();
                plane.gameObject.SetActive(true);
            }
            else
            {
                SwapOut();
                if (plane != null) plane.gameObject.SetActive(false);
                Shader.SetGlobalVector(PlaneId, new Vector4(0f, 0f, 0f, -1f));
            }
        }

        void ShowAxis()
        {
            for (int i = 0; i < axisSelected.Length; i++) if (axisSelected[i] != null) axisSelected[i].SetActive(i == axis);
        }

        // ------------------------------------------------------------------ the cut

        void LateUpdate()
        {
            if (!IsOn) return;
            var app = HeliosApp.Instance;
            if (app == null || app.Model == null || app.Model.Root == null || !app.Model.Root.gameObject.activeInHierarchy) { SetOn(false); return; }
            var m = app.Model;
            var b = m.Bounds;
            Vector3 fwd = m.Forward.sqrMagnitude > 0.5f ? m.Forward.normalized : Vector3.forward;
            Vector3 sideDir = m.Side.sqrMagnitude > 0.5f ? m.Side.normalized : Vector3.right;
            Vector3 up = Vector3.Cross(fwd, sideDir).normalized; if (Vector3.Dot(up, Vector3.up) < 0f) up = -up;
            Vector3 n = axis == 0 ? sideDir : axis == 1 ? up : fwd;
            // half the extent of the bounds along n, and the plane's size across the other two axes
            Vector3 e = b.extents;
            float half = Mathf.Abs(n.x) * e.x + Mathf.Abs(n.y) * e.y + Mathf.Abs(n.z) * e.z;
            Vector3 p = b.center + n * Mathf.Lerp(-half * 1.02f, half * 1.02f, position);
            Vector3 cutN = n * side;
            Shader.SetGlobalVector(PlaneId, new Vector4(cutN.x, cutN.y, cutN.z, -Vector3.Dot(cutN, p)));
            Shader.SetGlobalFloat(EdgeId, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) * outlineWidth);

            Vector3 a1 = axis == 0 ? fwd : sideDir, a2 = axis == 1 ? fwd : up;
            float s1 = Mathf.Abs(a1.x) * e.x + Mathf.Abs(a1.y) * e.y + Mathf.Abs(a1.z) * e.z;
            float s2 = Mathf.Abs(a2.x) * e.x + Mathf.Abs(a2.y) * e.y + Mathf.Abs(a2.z) * e.z;
            plane.position = p;
            plane.rotation = Quaternion.LookRotation(n, a2);
            plane.localScale = new Vector3(s1 * 2.1f, s2 * 2.1f, 1f);
        }

        void SwapIn(HeliosModel model)
        {
            parts.Clear(); saved.Clear();
            if (sectionShader == null) sectionShader = Shader.Find("HELIOS/Section (Built-in)");
            if (sectionShader == null) { Debug.LogWarning("[HELIOS] Cross Section: shader HELIOS/Section (Built-in) not found."); return; }
            foreach (var r in model.Parts)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                parts.Add(r); saved.Add(mats);
                var c = new Material[mats.Length];
                for (int i = 0; i < mats.Length; i++) c[i] = CutOf(mats[i]);
                r.sharedMaterials = c;
            }
        }

        void SwapOut()
        {
            for (int i = 0; i < parts.Count; i++) if (parts[i] != null) parts[i].sharedMaterials = saved[i];
            parts.Clear(); saved.Clear();
        }

        Material CutOf(Material src)
        {
            if (src == null) return null;
            if (cut.TryGetValue(src, out var m) && m != null) return m;
            m = new Material(sectionShader) { name = src.name + " (Section)" };
            var tex = FirstTexture(src, "baseColorTexture", "_BaseMap", "_MainTex");
            if (tex != null)
            {
                m.SetTexture("_MainTex", src.GetTexture(tex));
                m.SetTextureScale("_MainTex", src.GetTextureScale(tex));
                m.SetTextureOffset("_MainTex", src.GetTextureOffset(tex));
            }
            var nrm = FirstTexture(src, "normalTexture", "_BumpMap");
            if (nrm != null) m.SetTexture("_BumpMap", src.GetTexture(nrm));
            string col = First(src, "baseColorFactor", "_BaseColor", "_Color");
            if (col != null) m.SetColor("_Color", src.GetColor(col));
            string met = First(src, "metallicFactor", "_Metallic");
            if (met != null) m.SetFloat("_Metallic", src.GetFloat(met));
            if (src.HasProperty("roughnessFactor")) m.SetFloat("_Glossiness", 1f - src.GetFloat("roughnessFactor"));
            else { string sm = First(src, "_Smoothness", "_Glossiness"); if (sm != null) m.SetFloat("_Glossiness", src.GetFloat(sm)); }
            m.SetColor("_CapColor", sectionColor);
            cut[src] = m;
            return m;
        }

        static string First(Material m, params string[] names) { foreach (var n in names) if (m.HasProperty(n)) return n; return null; }
        static string FirstTexture(Material m, params string[] names) { foreach (var n in names) if (m.HasProperty(n) && m.GetTexture(n) != null) return n; return null; }

        /// <summary>The faint plane where the cut is (a quad with an unlit see-through material, made the first time the tool is used).</summary>
        void MakePlane()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "HELIOS Section Plane";
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            var sh = Shader.Find("Sprites/Default");
            planeMat = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { color = planeColor };
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = planeMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            plane = go.transform;
        }

        void OnDestroy()
        {
            SwapOut();
            if (plane != null) Destroy(plane.gameObject);
            if (planeMat != null) Destroy(planeMat);
            foreach (var m in cut.Values) if (m != null) Destroy(m);
        }
    }
}
