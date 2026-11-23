// HELIOS - the aircraft digital twin app, built from the Figma prototype "HELIOS 3D Application - Air Craft".
// The UI is the "HELIOS UI" prefab (Tools > HELIOS > Build UI): normal GameObjects you edit in the Scene view.
// This component sets up the 3D side: your aircraft, the cameras, the light and mouse navigation.
// Press Play in any scene: when the scene has no HeliosApp, one is added with the UI prefab.
// All values the UI shows are SYNTHETIC.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/HELIOS App")]
    public class HeliosApp : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const int SettingsVersion = 3;
        [SerializeField, HideInInspector] int settingsVersion;

        public static HeliosApp Instance { get; private set; }

        [Header("Aircraft")]
        [Tooltip("Your aircraft: an FBX or prefab from the Project window, or an object in the scene. Empty = a simple placeholder jet.")]
        public GameObject aircraftModel;
        [Tooltip("Which way the model's nose points (in the model's own axes).")]
        public ModelForward noseDirection = ModelForward.PlusZ;
        [Tooltip("Optional: points on your model for the labels. Empty = placed automatically from the model's size.")]
        public Transform humidityPoint, strainPoint, vibrationPoint;

        [Tooltip("The camera that draws the backdrop behind the model (placed in the scene by Tools > HELIOS > Place Aircraft In Scene). Empty = made at Play.")]
        public Camera backgroundCamera;

        [Header("UI")]
        [Tooltip("The UI in the scene. Empty = the one found in the scene, else the \"HELIOS UI\" prefab from Resources.")]
        public HeliosNavigator ui;

        [Header("Hologram colours (Health, Asset Integrity and Maintenance pages)")]
        public Color holoFill = new Color(0.05f, 0.22f, 0.42f, 0.42f);
        [Tooltip("Airframe, structure (wing root), engine.")]
        public Color[] integrityColors = { new Color32(34, 197, 94, 255), new Color32(245, 158, 11, 255), new Color32(239, 68, 68, 255) };
        public Color[] maintenanceColors = { new Color32(34, 211, 238, 255), new Color32(34, 197, 94, 255), new Color32(251, 146, 60, 255) };
        public Color healthColor = new Color32(34, 211, 238, 255);
        [Tooltip("Health page zones: structure (wing root), engine, avionics bay, hydraulics.")]
        public Color[] healthZoneColors = { new Color32(34, 197, 94, 255), new Color32(59, 130, 246, 255), new Color32(251, 146, 60, 255), new Color32(34, 197, 94, 255) };

        [Header("Hologram (the blueprint look)")]
        public HoloSettings hologram = new HoloSettings();

        [Header("Wireframe (lines over the model's own materials; switch it on per page in HeliosPage)")]
        public WireSettings wireframe = new WireSettings();
        [Tooltip("Health page: the status of each part type (scores 98 / 95 / 92 / 97: 95 and up healthy, 90-95 watch, below 90 critical).")]
        public List<CategoryStatus> healthCriteria = DefaultCriteria(HeliosWireRule.HealthStatus);
        [Tooltip("Asset Integrity page: the status of each part type.")]
        public List<CategoryStatus> integrityCriteria = DefaultCriteria(HeliosWireRule.IntegrityStatus);
        [Tooltip("Maintenance page: Critical = overdue, Watch = upcoming, None = no lines.")]
        public List<CategoryStatus> maintenanceCriteria = DefaultCriteria(HeliosWireRule.MaintenanceStatus);
        [Tooltip("Set the type of a part when its name does not tell (e.g. drag 'Mesh_042' in and pick Engine).")]
        public List<PartOverride> partCategories = new List<PartOverride>();
        [Tooltip("Click a part of the 3D model to select it (pages with the wireframe on).")]
        public bool clickToSelectParts = true;
        [Tooltip("How much of the view the selected part fills when the camera zooms to it (bigger = farther away).")]
        [Range(1f, 4f)] public float selectedPartMargin = 1.8f;

        [Header("Camera")]
        [Range(15f, 60f)] public float fieldOfView = 28f;
        [Tooltip("Start angle around the model (degrees) and above it.")]
        public float startYaw = 120f, startPitch = 18f;
        [Range(0.5f, 2f)] public float startZoom = 1f;

        // ---------- kept from v1 (no longer used; the UI is now the prefab) ----------
        [SerializeField, HideInInspector] public Sprite logo, ietmPhoto, ietmInset;
        [SerializeField, HideInInspector] public Font font;
        [SerializeField, HideInInspector] int startPage;
        [SerializeField, HideInInspector] public bool logButtonClicks = true;

        public int DragMode { get; private set; }          // left-drag in the 3D view: 0 rotate, 1 zoom, 2 pan
        public HeliosModel Model { get { return model; } }
        public Camera ViewCamera { get { return cam; } }
        public HeliosPage Page { get; private set; }

        HeliosModel model;
        Camera cam, backCam;
        float yaw, pitch, distance, homeDistance;
        Vector3 pivot;
        bool dragging; int dragButton; Vector2 pressAt;
        bool started, frameNext;
        bool flying; Vector3 pivotGoal; float distanceGoal;           // Sensor Focus: the camera glides to a point

        /// <summary>Raised when the view is reset (toolbar Reset).</summary>
        public event Action ViewReset;
        Rect lastView;

        // =====================================================================
        // start
        // =====================================================================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateInEveryScene()
        {
            if (FindAny<HeliosApp>() != null) return;
            new GameObject("HELIOS").AddComponent<HeliosApp>();
        }

        static T FindAny<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }

        void Awake() { Instance = this; RepairData(); }

        void Start()
        {
            EnsureEventSystem();
            SetUpCameras();
            EnsureLight();
            model = new HeliosModel();
            model.Build(aircraftModel, noseDirection, transform);
            model.SetColors(holoFill, healthColor, integrityColors, maintenanceColors);
            model.SetHologram(hologram, healthZoneColors);
            FindUi();
            if (ui != null && ui.GetComponent<HeliosSensorFocus>() == null) ui.gameObject.AddComponent<HeliosSensorFocus>();   // labels follow the sensor buttons
            ResetView();
            started = true;
            if (ui != null)
            {
                ui.onPageOpened.AddListener(OnPage);
                if (ui.Current != null) OnPage(ui.Current);
            }
        }

        void FindUi()
        {
            if (ui == null) ui = FindAny<HeliosNavigator>();
            if (ui == null)
            {
                var prefab = Resources.Load<GameObject>("HELIOS UI");
                if (prefab != null) ui = Instantiate(prefab).GetComponentInChildren<HeliosNavigator>(true);
            }
            if (ui == null)
            {
                Debug.Log("[HELIOS] No UI prefab yet: building the UI for this session. Use Tools > HELIOS > Build UI to get the editable prefab.");
                ui = HeliosUiFactory.Build(new HeliosUiFactory.RuntimeBinder()).GetComponentInChildren<HeliosNavigator>(true);
            }
            // the backdrop (glow behind the model) is drawn by the background camera
            var bd = ui != null ? ui.transform.root.GetComponentInChildren<HeliosBackdrop>(true) : FindAny<HeliosBackdrop>();
            if (bd == null) bd = FindAny<HeliosBackdrop>();
            if (bd != null) bd.Attach(backCam);
        }

        void SetUpCameras()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("HELIOS Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                if (FindAny<AudioListener>() == null) go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.Depth;            // the background camera has drawn the backdrop
            cam.backgroundColor = HeliosTheme.Current.background;
            cam.cullingMask &= ~(1 << 5);                       // not the UI layer
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 1000f;
            cam.fieldOfView = fieldOfView;
            if (backgroundCamera != null) backCam = backgroundCamera;
            else
            {
                var bg = new GameObject("HELIOS Background Camera");
                bg.transform.SetParent(transform, false);
                backCam = bg.AddComponent<Camera>();
            }
            backCam.clearFlags = CameraClearFlags.SolidColor;
            backCam.backgroundColor = HeliosTheme.Current.background;
            backCam.cullingMask = 1 << 5;
            backCam.depth = cam.depth - 10f;
        }

        static void EnsureLight()
        {
#if UNITY_2023_1_OR_NEWER
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            var lights = FindObjectsOfType<Light>();
#endif
            foreach (var l in lights) if (l.type == LightType.Directional && l.enabled) return;
            var go = new GameObject("HELIOS Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f; light.color = new Color(0.95f, 0.97f, 1f);
            go.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        static void EnsureEventSystem()
        {
            if (FindAny<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null) { go.AddComponent(t); return; }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        // =====================================================================
        // pages and tools
        // =====================================================================

        int holoOverride = -1;        // -1: the page decides; 0: original look; 1: hologram
        int wireOverride = -1;        // -1: the page decides; 0: off; 1: on
        readonly List<Renderer> focusParts = new List<Renderer>();

        void OnPage(HeliosPage p)
        {
            Page = p;
            if (p == null) return;
            holoOverride = -1; wireOverride = -1; focusParts.Clear(); groupHighlight = false; EndTransparent();
            ApplyLook();
            if (cam != null) cam.enabled = p.showModel;
        }

        void ApplyLook()
        {
            if (model == null) return;
            var look = Page != null && Page.showModel ? Page.modelLook : ModelLook.Original;
            if (holoOverride == 1 && look == ModelLook.Original) look = ModelLook.HoloHealth;
            if (holoOverride == 0) look = ModelLook.Original;
            model.SetLook(look);
            ApplyWireframe();
        }

        // =====================================================================
        // wireframe
        // =====================================================================

        public static List<CategoryStatus> DefaultCriteria(HeliosWireRule rule)
        {
            const WireStatus H = WireStatus.Healthy, W = WireStatus.Watch, C = WireStatus.Critical, N = WireStatus.None;
            WireStatus[] st =
                rule == HeliosWireRule.HealthStatus ? new[] { H, H, H, W, H, H } :         // airframe, structure, engine, avionics, hydraulics, stores
                rule == HeliosWireRule.IntegrityStatus ? new[] { H, W, C, H, H, H } :
                new[] { N, W, C, N, N, N };                                                   // maintenance: engine overdue, structure upcoming
            var list = new List<CategoryStatus>();
            for (int i = 0; i < st.Length; i++) list.Add(new CategoryStatus((PartCategory)i, st[i]));
            return list;
        }

        /// <summary>True when the wireframe is drawn right now.</summary>
        public bool WireframeOn
        {
            get { return wireOverride == 1 || (wireOverride == -1 && Page != null && Page.showModel && Page.wireframe); }
        }

        /// <summary>Draws (or removes) the wireframe for the open page: each part gets the colour of its criteria.</summary>
        public void ApplyWireframe()
        {
            if (model == null) return;
            var focusSet = new HashSet<Renderer>(focusParts);
            if (!WireframeOn)
            {
                // parts selected in the Aircraft Hierarchy are outlined even with the wireframe off
                if (groupHighlight && focusSet.Count > 0) model.SetWireframe(r => focusSet.Contains(r) ? wireframe.highlight : (Color?)null, wireframe);
                else model.SetWireframe(null, wireframe);
                return;
            }
            var rule = Page != null ? Page.wireRule : HeliosWireRule.AllParts;
            List<CategoryStatus> table = rule == HeliosWireRule.HealthStatus ? healthCriteria : rule == HeliosWireRule.IntegrityStatus ? integrityCriteria
                : rule == HeliosWireRule.MaintenanceStatus ? maintenanceCriteria : null;
            var ws = wireframe;
            bool selectedOnly = rule == HeliosWireRule.ClickedSensor || (Page != null && Page.onSelectedPartOnly && wireOverride != 1);
            model.SetWireframe(r =>
            {
                if (selectedOnly && !focusSet.Contains(r)) return null;
                if (rule == HeliosWireRule.AllParts || rule == HeliosWireRule.ClickedSensor) return ws.highlight;
                var cat = model.CategoryOf(r, partCategories);
                var st = WireStatus.None;
                if (table != null) foreach (var e in table) if (e != null && e.category == cat) { st = e.status; break; }
                if (st == WireStatus.None) return selectedOnly ? ws.highlight : (Color?)null;     // a clicked part always shows its lines
                return ws.ColorOf(st);
            }, wireframe);
        }

        /// <summary>Shows (true) or hides (false) the wireframe, until the page changes.</summary>
        public void ShowWireframe(bool on) { wireOverride = on ? 1 : 0; ApplyWireframe(); }

        /// <summary>Switches the wireframe on or off.</summary>
        public void ToggleWireframe() { ShowWireframe(!WireframeOn); }

        /// <summary>Marks the part(s) at a model point (a sensor) for the Clicked Sensor wireframe.</summary>
        public void FocusPart(Vector3 point)
        {
            focusParts.Clear();
            if (model != null) focusParts.AddRange(model.PartsAt(point));
            ApplyWireframe();
        }

        /// <summary>Raised when a part is selected (the part) or the selection is cleared (null).</summary>
        public event Action<Renderer> PartSelected;

        /// <summary>The part selected by a click, or null.</summary>
        public Renderer SelectedPart { get { return focusParts.Count > 0 ? focusParts[0] : null; } }

        /// <summary>Selects a part: its wireframe shows (criteria colour) and the camera zooms to it.
        /// The same part again, or null, goes back to the whole aircraft.</summary>
        public void SelectPart(Renderer part)
        {
            if (part == null || (focusParts.Count == 1 && focusParts[0] == part))
            {
                bool had = focusParts.Count > 0;
                focusParts.Clear(); ApplyWireframe();
                if (had) { FlyHome(); if (PartSelected != null) PartSelected(null); }
                return;
            }
            focusParts.Clear(); focusParts.Add(part); groupHighlight = false;
            ApplyWireframe();
            var b = part.bounds;
            float whole = model != null ? Mathf.Max(0.001f, model.Bounds.extents.magnitude) : 1f;
            FlyTo(b.center, b.extents.magnitude / whole * selectedPartMargin);
            if (PartSelected != null) PartSelected(part);
        }

        /// <summary>Selects the part under a screen point (pixels), or clears the selection when there is none.</summary>
        public void SelectPartAt(Vector2 screenPoint)
        {
            if (model == null || cam == null) return;
            SelectPart(model.PickPart(cam.ScreenPointToRay(screenPoint)));
        }

        bool groupHighlight;

        /// <summary>Selects a group of parts (a row of the Aircraft Hierarchy): they are outlined and the camera frames
        /// them. Null or empty clears the selection and goes back to the whole aircraft.</summary>
        public void SelectParts(IList<Renderer> parts, bool fly = true)
        {
            focusParts.Clear();
            if (parts != null) foreach (var r in parts) if (r != null && r.gameObject.activeInHierarchy) focusParts.Add(r);
            groupHighlight = focusParts.Count > 0;
            ApplyWireframe();
            if (!fly) return;
            if (focusParts.Count == 0) { FlyHome(); return; }
            var b = focusParts[0].bounds;
            foreach (var r in focusParts) b.Encapsulate(r.bounds);
            float whole = model != null ? Mathf.Max(0.001f, model.Bounds.extents.magnitude) : 1f;
            FlyTo(b.center, Mathf.Clamp(b.extents.magnitude / whole * selectedPartMargin, 0.05f, 1.2f));
        }

        /// <summary>Shows or hides parts (Aircraft Hierarchy eye button). Hidden parts cannot be clicked and their
        /// wireframe / hologram (children of the part) hide with them.</summary>
        public void SetPartsVisible(IEnumerable<Renderer> parts, bool visible)
        {
            if (parts == null) return;
            foreach (var r in parts) if (r != null && r.gameObject.activeSelf != visible) r.gameObject.SetActive(visible);
            if (!visible && focusParts.RemoveAll(r => r == null || !r.gameObject.activeInHierarchy) > 0) ApplyWireframe();
        }

        /// <summary>Clears the clicked-sensor part.</summary>
        public void ClearPartFocus() { if (focusParts.Count == 0) return; focusParts.Clear(); ApplyWireframe(); }

        /// <summary>The parts the clicked sensor marks.</summary>
        public IList<Renderer> FocusedParts { get { return focusParts; } }

        /// <summary>Shows the model as the hologram (true) or with its own materials (false), until the page changes.</summary>
        public void ShowHologram(bool on) { if (!on) EndTransparent(); holoOverride = on ? 1 : 0; ApplyLook(); }

        bool transparentLook, savedSolidBody;
        /// <summary>See-through hologram (View Tools > Transparent), whatever Hologram > Solid Body says. Any other look
        /// (or another page) puts Solid Body back as it was.</summary>
        public void ShowTransparent()
        {
            if (hologram == null) return;
            if (!transparentLook) { savedSolidBody = hologram.solidBody; transparentLook = true; }
            hologram.solidBody = false;
            ShowHologram(true);
        }

        void EndTransparent()
        {
            if (!transparentLook) return;
            transparentLook = false;
            if (hologram != null) hologram.solidBody = savedSolidBody;
        }

        /// <summary>Switches between the hologram and the model's own materials.</summary>
        public void ToggleHologram() { ShowHologram(!(model != null && model.HologramOn)); }

        /// <summary>The camera toolbar: 0 rotate, 1 zoom, 2 pan (left-drag mode), 3 reset, 4 full screen.</summary>
        public void Tool(int i)
        {
            if (i >= 0 && i <= 2) DragMode = i;
            else if (i == 3) ResetView();
            else if (i == 4) Screen.fullScreen = !Screen.fullScreen;
        }

        public void ResetView()
        {
            float turn = noseDirection == ModelForward.MinusZ ? 180f : noseDirection == ModelForward.PlusX ? 90f : noseDirection == ModelForward.MinusX ? -90f : 0f;
            yaw = startYaw + turn; pitch = startPitch;
            pivot = model != null ? model.Bounds.center : Vector3.zero;
            frameNext = true;
            flying = false;
            if (ViewReset != null) ViewReset();
        }

        float FrameDistance()
        {
            float radius = model != null ? model.Bounds.extents.magnitude * 0.82f : HeliosModel.Size * 0.5f;
            float aspect = cam != null && cam.pixelHeight > 0 ? (float)cam.pixelWidth / cam.pixelHeight : 1.6f;
            float vfov = fieldOfView * Mathf.Deg2Rad;
            float hfov = 2f * Mathf.Atan(Mathf.Tan(vfov / 2f) * aspect);
            return radius / Mathf.Sin(Mathf.Min(vfov, hfov) / 2f) / Mathf.Max(0.1f, startZoom);
        }

        /// <summary>Glides the camera to a point on the model; zoom 1 = the full view, 0.4 = much closer.</summary>
        public void FlyTo(Vector3 point, float zoom)
        {
            pivotGoal = point;
            distanceGoal = (homeDistance > 0f ? homeDistance : FrameDistance()) * Mathf.Clamp(zoom, 0.03f, 1.5f);
            flying = true;
        }

        /// <summary>Glides the camera back to the whole aircraft (keeps the angle you are looking from).</summary>
        public void FlyHome() { FlyTo(model != null ? model.Bounds.center : Vector3.zero, 1f); }

        /// <summary>The model's label point (0 humidity, 1 strain, 2 vibration, 3 engine RPM, 4 oil, 5 fuel, 6 hydraulic), or the point you set.</summary>
        public Vector3 LabelPoint(int i)
        {
            var t = i == 0 ? humidityPoint : i == 1 ? strainPoint : vibrationPoint;
            if (t != null) return t.position;
            return model != null ? model.LabelPoint(i) : Vector3.zero;
        }

        /// <summary>The 3D area in screen pixels: the "3D View Area" rectangle of the UI, else the whole screen.</summary>
        public Rect ViewRect()
        {
            var a = HeliosViewArea.Current;
            if (a == null) return new Rect(0f, 0f, Screen.width, Screen.height);
            var r = a.ScreenRect();
            if (r.width < 10f || r.height < 10f) return new Rect(0f, 0f, Screen.width, Screen.height);
            return r;
        }

        // =====================================================================
        // every frame
        // =====================================================================

        void Update()
        {
            if (!started) return;
            var view = ViewRect();
            if (cam != null)
            {
                cam.pixelRect = view;
                cam.fieldOfView = fieldOfView;
                if (Mathf.Abs(view.width - lastView.width) > 1f || Mathf.Abs(view.height - lastView.height) > 1f) { lastView = view; frameNext = true; }
            }
            if (cam != null && cam.enabled) Navigate(view);
        }

        void Navigate(Rect view)
        {
            if (!HeliosInput.HasMouse) return;
            var mp = HeliosInput.Position;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            for (int b = 0; b < 3; b++)
                if (HeliosInput.Down(b) && view.Contains(mp) && !overUi) { dragging = true; dragButton = b; pressAt = mp; }
            if (dragging && !HeliosInput.Held(dragButton))
            {
                dragging = false;
                // a left click (not a drag) picks a part
                if (dragButton == 0 && clickToSelectParts && (mp - pressAt).sqrMagnitude < 36f && Page != null && (Page.wireframe || wireOverride == 1)) SelectPartAt(mp);
            }
            if (dragging)
            {
                flying = false;                                  // your own drag wins over a glide
                var d = HeliosInput.Delta;
                int mode = dragButton == 0 ? DragMode : dragButton == 1 ? 0 : 2;     // right-drag rotates, middle-drag pans
                if (mode == 0) { yaw += d.x * 0.3f; pitch = Mathf.Clamp(pitch - d.y * 0.3f, -80f, 80f); }
                else if (mode == 1) Zoom(-d.y * 0.01f);
                else { float k = distance * 0.0018f; pivot -= (cam.transform.right * d.x + cam.transform.up * d.y) * k; }
            }
            float sc = HeliosInput.Scroll;
            if (Mathf.Abs(sc) > 0.001f && view.Contains(mp) && !overUi) Zoom(sc * 0.12f);
        }

        void Zoom(float amount) { distance = Mathf.Clamp(distance * (1f - amount), homeDistance * 0.03f, homeDistance * 4f); }

        void LateUpdate()
        {
            if (!started || cam == null) return;
            if (frameNext || homeDistance <= 0f)
            {
                float old = homeDistance;
                homeDistance = FrameDistance();
                distance = old > 0f && !float.IsNaN(distance) ? distance * homeDistance / old : homeDistance;
                frameNext = false;
            }
            if (flying)
            {
                float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f);     // about 0.8 s to arrive
                pivot = Vector3.Lerp(pivot, pivotGoal, k);
                distance = Mathf.Lerp(distance, distanceGoal, k);
                if ((pivot - pivotGoal).sqrMagnitude < 1e-6f && Mathf.Abs(distance - distanceGoal) < 1e-3f) flying = false;
            }
            if (model != null) { model.UpdateHolo(); if (WireframeOn) model.UpdateWireframe(); }
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.position = pivot + rot * new Vector3(0f, 0f, -distance);
            cam.transform.rotation = Quaternion.LookRotation(pivot - cam.transform.position, Vector3.up);
        }

        // =====================================================================
        // saved settings
        // =====================================================================

        /// <summary>Puts saved settings back in range, so an older or hand-edited component always works.</summary>
        public void RepairData()
        {
            if (!Enum.IsDefined(typeof(ModelForward), noseDirection)) noseDirection = ModelForward.PlusZ;
            if (float.IsNaN(fieldOfView) || fieldOfView < 15f || fieldOfView > 60f) fieldOfView = 28f;
            if (float.IsNaN(startYaw)) startYaw = 120f;
            if (float.IsNaN(startPitch)) startPitch = 18f;
            startPitch = Mathf.Clamp(startPitch, -80f, 80f);
            if (float.IsNaN(startZoom) || startZoom < 0.5f || startZoom > 2f) startZoom = 1f;
            if (integrityColors == null || integrityColors.Length != 3) integrityColors = new Color[] { new Color32(34, 197, 94, 255), new Color32(245, 158, 11, 255), new Color32(239, 68, 68, 255) };
            if (maintenanceColors == null || maintenanceColors.Length != 3) maintenanceColors = new Color[] { new Color32(34, 211, 238, 255), new Color32(34, 197, 94, 255), new Color32(251, 146, 60, 255) };
            if (startPage < 0) startPage = 0;
            if (hologram == null) hologram = new HoloSettings();
            hologram.Repair();
            if (healthZoneColors == null || healthZoneColors.Length != 4) healthZoneColors = new Color[] { new Color32(34, 197, 94, 255), new Color32(59, 130, 246, 255), new Color32(251, 146, 60, 255), new Color32(34, 197, 94, 255) };                                                     // v1 setting, kept
            if (wireframe == null) wireframe = new WireSettings();
            wireframe.Repair();
            if (healthCriteria == null) healthCriteria = DefaultCriteria(HeliosWireRule.HealthStatus);
            if (integrityCriteria == null) integrityCriteria = DefaultCriteria(HeliosWireRule.IntegrityStatus);
            if (maintenanceCriteria == null) maintenanceCriteria = DefaultCriteria(HeliosWireRule.MaintenanceStatus);
            if (partCategories == null) partCategories = new List<PartOverride>();
            if (float.IsNaN(selectedPartMargin) || selectedPartMargin < 1f || selectedPartMargin > 4f) selectedPartMargin = 1.8f;
            if (settingsVersion < 2) { startYaw = 120f; startPitch = 18f; startZoom = 1f; }        // fields new in v2
            settingsVersion = SettingsVersion;
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { RepairData(); }
        void OnValidate() { RepairData(); }
        void Reset() { settingsVersion = SettingsVersion; }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
