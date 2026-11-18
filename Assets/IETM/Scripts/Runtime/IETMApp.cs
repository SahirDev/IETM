// IETM application controller (core logic).
// The UI is pre-built in the Editor (IETM menu) and referenced through 'view' (IETMView).
// If no UI was pre-built, it is created at runtime as a fallback.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace IETM
{
    [DefaultExecutionOrder(100)]
    public partial class IETMApp : MonoBehaviour
    {
        [Header("UI (pre-built by IETM > Build Landing Gear IETM Scene)")]
        [Tooltip("The pre-built IETM UI. Leave empty to build the UI at runtime.")]
        public IETMView view;

        [Header("3D Models (scene instances or prefab assets)")]
        [Tooltip("Main model: landing gear in the EXTENDED position.")]
        public GameObject extendedModel;
        [Tooltip("Optional: same gear in the RETRACTED/COLLAPSED position.")]
        public GameObject retractedModel;
        [Tooltip("True when the Editor builder already oriented/aligned the models and tagged the parts (IETMPart).")]
        public bool modelPreconfigured;
        [Tooltip("Node used to align the retracted model with the extended model (its parent = CAD assembly frame).")]
        public string alignNodeName = "F-16 Front Landing Gear";
        [Tooltip("Auto-orientation: direction align node -> this node points down. Empty = keep imported orientation.")]
        public string downReferenceNode = "Wheel";
        [Tooltip("Extra rotation applied after auto-orientation (degrees).")]
        public Vector3 extraRotation = Vector3.zero;

        [Header("Data")]
        [Tooltip("IETM JSON. If empty, Resources/IETM/landing_gear_ietm is used.")]
        public TextAsset dataFile;

        [Header("Rendering")]
        public Shader highlightShader;
        public Shader ghostShader;
        public Camera viewerCamera;
        public Color backgroundColor = new Color(0.067f, 0.09f, 0.15f);

        [Header("Start-up")]
        [Tooltip("Set by the HELIOS shell while one of its pages covers the IETM (no viewer input, no shortcuts).")]
        public bool inputSuspended;
        [Tooltip("Module opened at start (id from the JSON). Default: the title page.")]
        public string startModule = "DM-TITLE";
        [Header("Procedures")]
        [Tooltip("Seconds a part takes to move out (disassembly) or back in (assembly). Can be changed in Play mode.")]
        [Range(0.3f, 8f)] public float partMoveSeconds = 2.5f;
        [Tooltip("Camera orbits to a clear view of the part being removed / the parts shown in Operation.")]
        public bool cameraFollowsParts = true;
        [Tooltip("Printable IPC (PDF) under Assets/StreamingAssets, opened by the 'PDF' button in the IPD figure card.")]
        public string ipdPdfFile = "IETM/32-20-00_Fig1_IPC.pdf";
        [Tooltip("Show the item-number callouts when the IETM starts (toggle with the Callouts button or L).")]
        public bool calloutsOnAtStart = false;

        // ---- runtime state
        IETMDatabase db;
        IETMModel model;
        ViewerCamera vcam;

        DataModule currentModule;
        IPDItem selectedItem;
        readonly List<CartLine> cart = new List<CartLine>();
        bool labelsOn = true;
        int procStep;

        struct HistoryEntry { public DataModule module; public IPDItem item; }
        readonly List<HistoryEntry> history = new List<HistoryEntry>();
        int historyIndex = -1;
        bool navigatingHistory;

        // input
        IPDItem uiHover;
        Vector2 downPos;
        bool clickCandidate;
        float lastClickTime;
        IPDItem lastClickItem;

        public IETMDatabase Database => db;

        // ------------------------------------------------------------------ lifecycle

        void Start()
        {
            if (dataFile == null) dataFile = Resources.Load<TextAsset>("IETM/landing_gear_ietm");
            db = IETMDatabase.Load(dataFile);
            if (view != null) view.theme.Apply();
            labelsOn = calloutsOnAtStart;

            EnsureEventSystem();
            SetupCameraAndLights();

            var ext = Spawn(extendedModel);
            // A model with its own extend / retract animation (the GLB) needs no second model.
            SetupGearAnimation(ext);
            GameObject ret = null;
            if (gearAnimated) { if (retractedModel != null && retractedModel.scene.IsValid()) retractedModel.SetActive(false); }
            else ret = Spawn(retractedModel);

            // Orientation: an animated model keeps the rotation set in the scene (Landing_Gear 1.glb: X = 90).
            // The two-model setup is stood upright automatically unless the Editor already did it.
            if (ext != null && !gearAnimated && (!modelPreconfigured || IETMModelSetup.UprightError(ext, alignNodeName, downReferenceNode) > 3f))
                IETMModelSetup.OrientUpright(ext, alignNodeName, downReferenceNode, extraRotation);

            model = new IETMModel(db, highlightShader, ghostShader) { selectColor = Theme.Accent, hoverColor = Theme.Warn };
            ModelVariant v0 = null;
            if (ext != null) v0 = model.AddVariant("Extended", ext);
            if (ret != null) model.AddVariant("Retracted", ret, v0, alignNodeName, !modelPreconfigured);
            if (model.Active == null) Debug.LogError("[IETM] No model assigned. Assign the extended landing gear on IETM App.");

            if (view == null) view = IETMUIBuilder.Build(db);
            InitUI();

            if (model.Active != null)
            {
                vcam.SetHome(model.Active.bounds);
                vcam.Home(true);
            }

            var start = db.modules.FirstOrDefault(m => m.id == startModule)
                        ?? db.modules.FirstOrDefault(m => m.type == "ipd") ?? db.modules.FirstOrDefault();
            if (start != null) OpenModule(start);
            Toast("IETM loaded  ·  " + db.items.Count + " IPD items  ·  " + model.variants.Count + " gear state(s)", Theme.Accent);
        }

        static GameObject Spawn(GameObject src)
        {
            if (src == null) return null;
            GameObject go = src;
            if (!src.scene.IsValid()) // prefab asset -> instantiate
            {
                go = Instantiate(src);
                go.name = src.name;
            }
            go.SetActive(true);
            return go;
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void SetupCameraAndLights()
        {
            if (viewerCamera == null) viewerCamera = Camera.main;
            if (viewerCamera == null)
            {
                var cgo = new GameObject("IETM Camera", typeof(Camera));
                cgo.tag = "MainCamera";
                viewerCamera = cgo.GetComponent<Camera>();
            }
            viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            viewerCamera.backgroundColor = backgroundColor;
            viewerCamera.fieldOfView = 35f;
            if (!viewerCamera.TryGetComponent(out vcam)) vcam = viewerCamera.gameObject.AddComponent<ViewerCamera>();

            bool hasDir = FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l => l.type == LightType.Directional && l.enabled);
            if (!hasDir)
            {
                var key = new GameObject("IETM Key Light").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 1.2f; key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }
            if (viewerCamera.GetComponentInChildren<Light>() == null)
            {
                var fill = new GameObject("IETM Camera Fill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = 0.45f; fill.shadows = LightShadows.None;
                fill.transform.SetParent(viewerCamera.transform, false);
                fill.transform.localRotation = Quaternion.Euler(10f, 15f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.35f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.18f);
        }

        // ------------------------------------------------------------------ update loop

        void Update()
        {
            if (model != null) model.removalDuration = partMoveSeconds;   // live from the Inspector
            if (model == null || view == null) return;

            SyncViewportRect();
            AnimatePanels();

            // HELIOS shell page (aircraft / system / 3D view) covers the IETM: no viewer input
            if (inputSuspended)
            {
                vcam.PointerInViewport = false;
                vcam.InputBlocked = true;
                model.SetHover(null);
                UpdateGearAnimation();
                model.Tick(Time.unscaledDeltaTime);
                UpdateToasts();
                return;
            }

            bool typing = IsTyping();
            Vector2 mouse = InputCompat.MousePosition;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool inViewport = RectTransformUtility.RectangleContainsScreenPoint(view.viewport, mouse, null) && !overUI && !modalOpen;
            vcam.PointerInViewport = inViewport;
            vcam.InputBlocked = typing;

            IPDItem hoverItem = null;
            if (inViewport && !vcam.IsDragging) hoverItem = model.Pick(viewerCamera.ScreenPointToRay(mouse));
            model.SetHover(inViewport ? hoverItem : uiHover);
            UpdateHoverTip(hoverItem, mouse);

            if (InputCompat.LeftDown && inViewport) { clickCandidate = true; downPos = mouse; }
            if (clickCandidate && (mouse - downPos).sqrMagnitude > 36f) clickCandidate = false;
            if (InputCompat.LeftUp && clickCandidate)
            {
                clickCandidate = false;
                var hit = model.Pick(viewerCamera.ScreenPointToRay(mouse));
                bool dbl = hit != null && hit == lastClickItem && Time.unscaledTime - lastClickTime < 0.35f;
                lastClickTime = Time.unscaledTime; lastClickItem = hit;
                if (hit != null)
                {
                    if (InputCompat.Ctrl && hit.parent != null && hit.parent.parent != null) Select(hit.parent, false, dbl);
                    else Select(hit, false, dbl);
                }
                else ClearSelection();
            }

            if (!typing) HandleShortcuts();

            UpdateGearAnimation();
            model.Tick(Time.unscaledDeltaTime);
            UpdateStatus();
            UpdateToasts();
        }

        void LateUpdate()
        {
            if (model == null || view == null) return;
            UpdateCallouts();
        }

        bool IsTyping()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;
            var f = es.currentSelectedGameObject.GetComponent<InputField>();
            return f != null && f.isFocused;
        }

        void HandleShortcuts()
        {
            if (InputCompat.KeyDown(IKey.Escape))
            {
                if (modalOpen) { CloseModals(); return; }
                ClearSelection();
            }
            if (InputCompat.KeyDown(IKey.F)) FocusSelection();
            if (InputCompat.KeyDown(IKey.Home)) HomeView();
            if (InputCompat.KeyDown(IKey.H)) HideSelected();
            if (InputCompat.KeyDown(IKey.I)) ToggleIsolate();
            if (InputCompat.KeyDown(IKey.X)) ToggleXRay();
            if (InputCompat.KeyDown(IKey.L)) ToggleLabels();
            if (InputCompat.KeyDown(IKey.E)) ToggleExplode();
            if (InputCompat.KeyDown(IKey.R)) ToggleAutoRotate();
            if (InputCompat.KeyDown(IKey.Space)) ToggleGear();
            if (InputCompat.KeyDown(IKey.Slash)) ToggleHelp();
            if (currentModule != null && currentModule.type == "procedure")
            {
                if (InputCompat.KeyDown(IKey.Right)) StepNext();
                if (InputCompat.KeyDown(IKey.Left)) StepPrev();
            }
        }

        // ------------------------------------------------------------------ commands (buttons)

        public void Execute(IETMCommand c, string arg = null)
        {
            switch (c)
            {
                case IETMCommand.HistoryBack: HistoryGo(-1); break;
                case IETMCommand.HistoryForward: HistoryGo(1); break;
                case IETMCommand.OpenTitlePage: { var t = db.modules.FirstOrDefault(); if (t != null) OpenModule(t); } break;
                case IETMCommand.OpenModule:
                    {
                        var m = db.modules.FirstOrDefault(x => x.id == arg);
                        if (m != null) OpenModule(m); else Toast("Module not found: " + arg, Theme.Warn);
                    }
                    break;
                case IETMCommand.SelectItem:
                    {
                        var it = db.Find(arg);
                        if (it != null) Select(it, false, true); else Toast("Item not found: " + arg, Theme.Warn);
                    }
                    break;
                case IETMCommand.ToggleLeftPanel: leftOpen = !leftOpen; UpdatePanelHandles(); break;
                case IETMCommand.ToggleRightPanel: rightOpen = !rightOpen; UpdatePanelHandles(); break;
                case IETMCommand.ToggleOrderList: ToggleCart(); break;
                case IETMCommand.ToggleHelp: ToggleHelp(); break;
                case IETMCommand.CloseDialogs: CloseModals(); break;
                case IETMCommand.TabPublication: ShowLeftTab(0); break;
                case IETMCommand.TabStructure: ShowLeftTab(1); break;
                case IETMCommand.GearExtended: SetGear(0); break;
                case IETMCommand.GearRetracted: SetGear(1); break;
                case IETMCommand.ToggleGear: ToggleGear(); break;
                case IETMCommand.HomeView: HomeView(); break;
                case IETMCommand.FitSelection: FocusSelection(); break;
                case IETMCommand.Isolate: ToggleIsolate(); break;
                case IETMCommand.Hide: HideSelected(); break;
                case IETMCommand.ShowAll: ShowAll(); break;
                case IETMCommand.XRay: ToggleXRay(); break;
                case IETMCommand.Callouts: ToggleLabels(); break;
                case IETMCommand.AutoRotate: ToggleAutoRotate(); break;
                case IETMCommand.ViewIso: vcam.SetPreset("iso"); break;
                case IETMCommand.ViewFront: vcam.SetPreset("front"); break;
                case IETMCommand.ViewSide: vcam.SetPreset("side"); break;
                case IETMCommand.ViewTop: vcam.SetPreset("top"); break;
                case IETMCommand.ViewBottom: vcam.SetPreset("bottom"); break;
                case IETMCommand.ViewAft: vcam.SetPreset("aft"); break;
                case IETMCommand.ViewPort: vcam.SetPreset("port"); break;
                case IETMCommand.ZoomIn: vcam.Zoom(0.8f); break;
                case IETMCommand.ZoomOut: vcam.Zoom(1.25f); break;
                case IETMCommand.Snapshot: TakeSnapshot(); break;
                case IETMCommand.ClearSelection: ClearSelection(); break;
                case IETMCommand.ExplodeToggle: ToggleExplode(); break;
                case IETMCommand.AddSelectionToOrder: AddToCart(selectedItem); break;
                case IETMCommand.OrderClear: cart.Clear(); RefreshCart(); break;
                case IETMCommand.OrderCopy: GUIUtility.systemCopyBuffer = CartAsText(); Toast("Order list copied (tab-separated)", Theme.Ok); break;
                case IETMCommand.IpdSelectAll: if (db.items.Count > 0) Select(db.items[0], false, true); break;
                case IETMCommand.IpdReset: ShowAll(); ClearSelection(); HomeView(); break;
                case IETMCommand.IpdClearFilter: if (view.tableFilter) view.tableFilter.text = ""; ApplyTableFilter(); break;
                case IETMCommand.IpdDownloadPdf: DownloadIpdPdf(); break;
                case IETMCommand.IpdCopyList: GUIUtility.systemCopyBuffer = TableAsText(); Toast("Parts list copied (tab-separated)", Theme.Ok); break;
                case IETMCommand.StepPrevious: StepPrev(); break;
                case IETMCommand.StepNext: StepNext(); break;
                case IETMCommand.StepRestart: StepGo(0); break;
                case IETMCommand.RestoreParts: model.RestoreAll(); Toast("All removed parts restored", Theme.Ok); break;
            }
        }

        // ------------------------------------------------------------------ selection

        public void Select(IPDItem item, bool fromTable = false, bool focus = false)
        {
            if (item == null) { ClearSelection(); return; }
            selectedItem = item;
            model.SetSelection(model.LeavesOf(item));
            RefreshSelectionUI(!fromTable);
            if (focus) FocusSelection();
            PushHistory();
        }

        public void ClearSelection()
        {
            selectedItem = null;
            model.SetSelection(null);
            RefreshSelectionUI(false);
        }

        void FocusSelection()
        {
            if (selectedItem != null && model.TryGetBounds(model.LeavesOf(selectedItem), out var b)) vcam.Frame(b, false, 1.6f);
            else vcam.Frame(model.VisibleBounds());
        }

        void FocusItems(IEnumerable<IPDItem> leaves, float pad = 1.4f)
        {
            if (model.TryGetBounds(leaves, out var b)) vcam.Frame(b, false, pad);
        }

        /// <summary>
        /// Orbits the camera to look at 'area' from the side that sees the target parts best: candidate
        /// directions around the motion axis (or around the vertical when there is no motion) are scored by how much
        /// of the target is hidden by other parts and by how far the camera has to turn; the camera then backs
        /// off until it is outside every part, so it never ends up inside the model.
        /// </summary>
        void AimAt(List<IPDItem> targets, Bounds area, Vector3 motion, float padding)
        {
            vcam.autoRotate = false; if (view.spinButton) view.spinButton.On = false;
            var tset = new HashSet<IPDItem>(targets);
            var pts = new List<Vector3> { area.center };
            if (model.TryGetBounds(targets, out var cur)) pts.Add(cur.center);
            if (motion.sqrMagnitude > 1e-12f) { pts.Add(area.center - motion * 0.5f); pts.Add(area.center + motion * 0.5f); }

            var all = model.VisibleBounds();
            float baseDist = vcam.DistanceFor(area, padding);
            var fNow = vcam.TargetForward;

            // candidate view directions
            var cands = new List<Vector3>();
            if (motion.sqrMagnitude > 1e-12f)
            {
                var dn = motion.normalized;
                var e1 = fNow - dn * Vector3.Dot(fNow, dn);
                if (e1.sqrMagnitude < 1e-6f) e1 = Vector3.Cross(dn, Vector3.up);
                e1.Normalize();
                var e2 = Vector3.Cross(dn, e1).normalized;
                for (int a = 0; a < 360; a += 30)
                {
                    float r = a * Mathf.Deg2Rad;
                    cands.Add((Mathf.Cos(r) * e1 + Mathf.Sin(r) * e2).normalized);
                }
            }
            else
            {
                for (int a = 0; a < 360; a += 30)
                    foreach (float p in new[] { 22f, 38f })
                        cands.Add(Quaternion.Euler(p, vcam.TargetYaw + a, 0f) * Vector3.forward);
            }

            Vector3 best = fNow; float bestScore = float.MaxValue; float bestDist = baseDist;
            foreach (var c0 in cands)
            {
                var c = c0;
                // keep a pleasant elevation: look slightly down, never from far below
                float pitch = Mathf.Asin(Mathf.Clamp(-c.y, -1f, 1f)) * Mathf.Rad2Deg;
                if (pitch < -10f || pitch > 70f) continue;
                float dist = baseDist;
                // back off until the camera is outside the parts (but not further than outside the whole model)
                float maxDist = Vector3.Distance(area.center, all.center) + all.extents.magnitude * 1.1f + baseDist * 0.2f;
                while (dist < maxDist && model.InsideParts(area.center - c * dist, all.extents.magnitude * 0.03f)) dist *= 1.15f;
                var eye = area.center - c * dist;
                int occ = model.CountOccluded(eye, pts, tset);
                float turn = Vector3.Angle(c, fNow) / 180f;
                float score = occ * 10f + turn * 3f + (pitch < 8f ? 1.5f : 0f) + (dist / baseDist - 1f);
                if (score < bestScore) { bestScore = score; best = c; bestDist = dist; }
            }
            float safe = Vector3.Distance(area.center, all.center) + all.extents.magnitude * 1.05f;
            vcam.OrbitTo(area.center, best, bestDist, safe);
        }

        void HomeView()
        {
            vcam.autoRotate = false; if (view.spinButton) view.spinButton.On = false;
            vcam.SetPreset("iso");
            vcam.Frame(model.VisibleBounds());
        }

        // ------------------------------------------------------------------ viewer commands

        void HideSelected()
        {
            if (selectedItem == null) { Toast("Select a part to hide", Theme.Muted); return; }
            foreach (var l in model.LeavesOf(selectedItem)) model.SetHidden(l, true);
            Toast("Hidden: " + selectedItem.description, Theme.Muted);
            ClearSelection();
            RefreshStructureTree();
        }

        void ShowAll()
        {
            model.ShowAll();
            SetOn(view.isolateButton, false);
            RefreshStructureTree();
        }

        void ToggleIsolate()
        {
            if (model.isolated != null) { model.Isolate(null); SetOn(view.isolateButton, false); return; }
            if (selectedItem == null) { Toast("Select a part or assembly to isolate", Theme.Muted); return; }
            model.Isolate(model.LeavesOf(selectedItem));
            SetOn(view.isolateButton, true);
            FocusSelection();
        }

        void ToggleXRay() { model.SetXRay(!model.xray); SetOn(view.xrayButton, model.xray); }
        void ToggleLabels() { labelsOn = !labelsOn; SetOn(view.calloutsButton, labelsOn); }
        void ToggleAutoRotate() { vcam.autoRotate = !vcam.autoRotate; SetOn(view.spinButton, vcam.autoRotate); }
        void ToggleExplode()
        {
            float on = model.HasDesignedExplode ? 1f : 0.6f;
            if (view.explodeSlider) view.explodeSlider.value = view.explodeSlider.value > 0.01f ? 0f : on;
            else SetExplode(model.explode > 0.01f ? 0f : on);
        }
        void ToggleGear() => SetGear(CurrentGear == 0 ? 1 : 0);

        static void SetOn(IETMButton b, bool on) { if (b != null) b.On = on; }

        void SetGear(int index)
        {
            if (gearAnimated) { SetGearAnimated(index); return; }
            if (index >= model.variants.Count || model.variants[index].root == null)
            {
                if (index != 0) Toast("Retracted model not assigned", Theme.Warn);
                return;
            }
            if (index == model.ActiveIndex) { UpdateGearButtons(); return; }
            model.SetVariant(index);
            UpdateGearButtons();
            Toast("Gear shown " + (index == 0 ? "EXTENDED (down and locked)" : "RETRACTED (up)"), Theme.Accent2);
        }

        void SetExplode(float v)
        {
            if (gearMoving)
            {
                if (view.explodeSlider) view.explodeSlider.SetValueWithoutNotify(0f);
                return;
            }
            model.designedExplode = CurrentGear == 0;   // the layout is designed for the extended gear
            model.SetExplode(v);
            if (view.explodeValue != null) view.explodeValue.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        void TakeSnapshot()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Toast("Snapshots are not available in WebGL builds", Theme.Muted);
#else
            string path = System.IO.Path.Combine(Application.persistentDataPath, "IETM_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Toast("Snapshot saved: " + path, Theme.Ok);
#endif
        }

        // ------------------------------------------------------------------ modules

        public void OpenModule(DataModule m)
        {
            if (m == null) return;
            if (m.type == "document") { DownloadFile(string.IsNullOrEmpty(m.file) ? ipdPdfFile : m.file); return; }
            if (currentModule != null && currentModule.type == "procedure" && currentModule != m)
            {
                model.RestoreAll();
                model.Isolate(null);
                model.showRemoved = false;
                SetOn(view.isolateButton, false);
                if (gearLoop) { gearLoop = false; SetGear(0); }
            }
            currentModule = m;
            BuildModuleContent();
            RefreshPublicationTree();

            if (m.type == "procedure")
            {
                procStep = 0;
                ApplyStep();
            }
            else if (m.focus != null && m.focus.Length > 0)
            {
                var it = db.Find(m.focus[0]);
                if (it != null) { selectedItem = it; model.SetSelection(model.LeavesOf(it)); RefreshSelectionUI(true); }
            }
            else RefreshSelectionUI(false);
            PushHistory();
        }

        // ------------------------------------------------------------------ procedure engine

        void StepNext() { if (currentModule?.steps == null) return; if (procStep < currentModule.steps.Length - 1) { procStep++; ApplyStep(); } }
        void StepPrev() { if (currentModule?.steps == null) return; if (procStep > 0) { procStep--; ApplyStep(); } }
        void StepGo(int i) { if (currentModule?.steps == null) return; procStep = Mathf.Clamp(i, 0, currentModule.steps.Length - 1); ApplyStep(); }

        void ApplyStep()
        {
            var steps = currentModule.steps;
            if (steps == null || steps.Length == 0) return;
            var step = steps[procStep];

            if (!string.IsNullOrEmpty(step.gear)) SetGear(step.gear == "retracted" ? 1 : 0);

            bool layout = false;
            foreach (var s in steps) if (s.action == "detach" || s.action == "install") layout = true;
            model.showRemoved = layout;
            model.designedExplode = CurrentGear == 0;

            var removed = new HashSet<IPDItem>();
            for (int i = 0; i <= procStep; i++)
            {
                var s = steps[i];
                if (s.action == "restore") removed.Clear();
                else if ((s.action == "remove" || s.action == "detach") && s.focus != null)
                    foreach (var f in s.focus) { var it = db.Find(f); if (it != null) foreach (var l in model.LeavesOf(it)) removed.Add(l); }
                else if (s.action == "install" && s.focus != null)
                    foreach (var f in s.focus) { var it = db.Find(f); if (it != null) foreach (var l in model.LeavesOf(it)) removed.Remove(l); }
            }
            foreach (var l in db.Leaves()) model.SetRemoved(l, removed.Contains(l));

            // operation: loop the gear animation while the step is shown
            gearLoop = step.animate == "cycle";
            if (step.animate == "retract") SetGear(1);
            else if (step.animate == "extend") SetGear(0);

            var focusLeaves = new List<IPDItem>();
            IPDItem first = null;
            if (step.focus != null)
                foreach (var f in step.focus) { var it = db.Find(f); if (it == null) continue; if (first == null) first = it; focusLeaves.AddRange(model.LeavesOf(it)); }

            selectedItem = first;
            model.SetSelection(step.action == "operate" ? new List<IPDItem>() : focusLeaves);

            if (step.action == "isolate" || step.action == "operate") { model.Isolate(step.action == "operate" && first != null && first.parent == null ? null : focusLeaves); SetOn(view.isolateButton, step.action == "isolate"); }
            else if (step.action == "remove" && first != null && first.parent != null) { model.Isolate(model.LeavesOf(first.parent)); SetOn(view.isolateButton, true); }
            else { model.Isolate(null); SetOn(view.isolateButton, false); }

            if (!string.IsNullOrEmpty(step.view)) vcam.SetPreset(step.view);
            model.removalDuration = partMoveSeconds;
            if (cameraFollowsParts && layout && (step.action == "detach" || step.action == "install") && focusLeaves.Count > 0 && focusLeaves.Count <= 4
                && model.TryGetRemovalPath(focusLeaves, out var path, out var motion))
            {
                // follow the part being removed / installed: orbit to a clear view across its path
                AimAt(focusLeaves, path, motion, 1.35f);
            }
            else if (cameraFollowsParts && step.action == "operate" && first != null && first.parent != null && focusLeaves.Count > 0
                && model.TryGetBounds(focusLeaves, out var fb))
            {
                // operation: focus the parts that are working (they move, so leave some room)
                AimAt(focusLeaves, fb, Vector3.zero, 1.9f);
            }
            else if (step.action == "operate" || layout)
            {
                // keep the whole assembly (and the laid-out parts) in view
                if (procStep == 0 || layout) FocusItems(db.Leaves().ToList(), layout ? 1.9f : 1.15f);
            }
            else
            {
                var frameSet = step.action == "remove" && first != null && first.parent != null ? model.LeavesOf(first.parent) : focusLeaves;
                if (frameSet.Count > 0) FocusItems(frameSet, frameSet.Count > 6 ? 1.15f : 1.5f);
            }

            RefreshSelectionUI(true);
            RefreshProcedureUI();
        }

        // ------------------------------------------------------------------ order list

        void AddToCart(IPDItem item)
        {
            if (item == null) { Toast("Select a part first", Theme.Muted); return; }
            var line = cart.FirstOrDefault(c => c.item == item);
            if (line != null) line.qty++;
            else
            {
                int.TryParse(item.upa, out int q); if (q <= 0) q = 1;
                cart.Add(new CartLine { item = item, qty = q });
            }
            Toast("Added to order list:  " + item.partNumber, Theme.Ok);
            RefreshCart();
        }

        string CartAsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Item\tPart Number\tCAGE\tDescription\tQty");
            foreach (var c in cart) sb.AppendLine($"{c.item.itemNo}\t{c.item.partNumber}\t{c.item.cage}\t{c.item.description}\t{c.qty}");
            return sb.ToString();
        }

        string TableAsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Item\tISN\tCAGE\tPart Number\tDescription\tUPA");
            foreach (var it in db.items) sb.AppendLine($"{it.itemNo}\t{it.isn}\t{it.cage}\t{it.partNumber}\t{it.IndentedDescription}\t{it.upa}");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ history

        void PushHistory()
        {
            if (navigatingHistory) return;
            var e = new HistoryEntry { module = currentModule, item = selectedItem };
            if (historyIndex >= 0 && historyIndex < history.Count)
            {
                var cur = history[historyIndex];
                if (cur.module == e.module && cur.item == e.item) return;
            }
            if (historyIndex < history.Count - 1) history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
            history.Add(e);
            if (history.Count > 100) history.RemoveAt(0);
            historyIndex = history.Count - 1;
        }

        void HistoryGo(int delta)
        {
            int ni = historyIndex + delta;
            if (ni < 0 || ni >= history.Count) return;
            historyIndex = ni;
            navigatingHistory = true;
            var e = history[ni];
            if (e.module != currentModule) OpenModule(e.module);
            if (e.item != null) Select(e.item); else ClearSelection();
            navigatingHistory = false;
        }
    }
}
